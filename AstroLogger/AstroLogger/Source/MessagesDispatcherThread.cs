namespace AstroLogger
{
    /// <summary>
    /// Works as <see cref="Queue{T}"/> of <see cref="Message"/>s that asynchronously dispatch messages in FIFO order to a specified function.
    /// </summary>
    public class MessagesDispatcherThread
    {
        #region Fields and Properties

        private Queue<Message> mMessageQueue = new Queue<Message>();

        private object mQueueLock = new object();

        private Action<Message> mLogFunction;
        
        private Thread mThread;

        private long mShouldStop = 0;

        /// <summary>
        /// Indicates whether the <see cref="Thread"/> must continue executing it's loop.
        /// </summary>
        private bool ShouldStop
        {
            get => Interlocked.Read(ref mShouldStop) == 1;
            set
            {
                if (value)
                {
                    Interlocked.CompareExchange(ref mShouldStop, 1, 0);
                }
                else
                {
                    Interlocked.CompareExchange(ref mShouldStop, 0, 1);
                }
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logFunction">Function that will be executed to log messages.
        /// The function is asynchronously executed, so it must be defined using synchronization.
        public MessagesDispatcherThread(Action<Message> logFunction, string loggerName)
        {
            mLogFunction = logFunction;

            // Create a new thread with the loop function.
            mThread = new Thread(Loop);

            mThread.Name = $"{loggerName} - MessagesDispatcherThread";

            // The thread must be a background thread so that the program doesn't crash if the app closes while the thread is still running.
            mThread.IsBackground = true;
            mThread.Start();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Synchronously tries to add a <see cref="Message"/> to the <see cref="Queue{T}"/>.
        /// </summary>
        /// <param name="message"><see cref="Message"/>we try to add to the queue</param>
        /// <param name="timeout">Maximum waiting time to add the <paramref name="message"/> in miliseconds</param>
        /// <returns><see cref="bool"/> indicating whether the <paramref name="message"/> could be added to the <see cref="Queue{T}"/></returns>
        public bool TryAddingMessage(Message message, int timeout)
        {
            bool retrievedLock = false;

            try
            {
                // We try to get the loop lock.
                Monitor.TryEnter(mQueueLock, TimeSpan.FromMilliseconds(timeout), ref retrievedLock);

                // If the loop lock was successfully retrieved, we add the message to the queue.
                if (retrievedLock)
                {
                    mMessageQueue.Enqueue(message);
                }
            }
            finally
            {
                // If we could retrieve the lock:
                if (retrievedLock)
                {
                    // We notify the threads to wait for the lock to be released.
                    Monitor.Pulse(mQueueLock);

                    // We release the lock.
                    Monitor.Exit(mQueueLock);
                }
            }

            return retrievedLock;
        }

        /// <summary>
        /// Asynchronously try to add a <see cref="Message"/> to the <see cref="Queue{T}"/>.
        /// </summary>
        /// <param name="message"><see cref="Message"/>we try to add to the queue</param>
        /// <param name="timeout">Maximum waiting time to add the <paramref name="message"/> in milliseconds</param>
        /// <returns></returns>
        public async Task TryAddingMessageAsync(Message message, int timeout = int.MaxValue)
        {
            await Task.Run(() => TryAddingMessage(message, timeout));
        }

        /// <summary>
        /// Loop executed by the <see cref="Thread"/>
        /// </summary>
        private void Loop()
        {
            bool retrievedLock = false;

            // Loop while we don't have to stop the thread.
            while (!ShouldStop)
            {
                try
                {
                    Monitor.TryEnter(mQueueLock, Int32.MaxValue, ref retrievedLock);

                    // If the lock was retrieved:
                    if (retrievedLock)
                    {
                        // Verify the message queue is not empty:
                        if (mMessageQueue.Count != 0)
                        {
                            // We remove one message by loop cycle from the queue and pass it to the function.
                            mLogFunction(mMessageQueue.Dequeue());
                        }
                    }
                }
                finally
                {
                    if (retrievedLock)
                    {
                        Monitor.Pulse(mQueueLock);
                        Monitor.Exit(mQueueLock);

                        retrievedLock = false;
                    }
                }
            }
        }

        /// <summary>
        /// Stops the <see cref="Thread"/> loop and invalidates this instance.
        /// </summary>
        public async Task Stop()
        {
            ShouldStop = true;

            await Task.Factory.StartNew(() => { mThread.Join(); });

            mLogFunction = null!;
        }

        #endregion
    }
}
