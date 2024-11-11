using System.Diagnostics;
using System.IO.Pipes;
using System.Threading;

namespace AstroLogger
{
    /// <summary>
    /// Works as an intermediary between a <see cref="Logger"/> and a Console Logger.
    /// </summary>
    public sealed class ConsoleLoggerProxy
    {
        #region Fields and Properties

        // --- Fields ---

        
        /// <summary>
        /// <see cref="mMessageQueue"/>'s access lock.
        /// </summary>
        private object mLock = new object();

        /// <summary>
        /// Queue of messages waiting to be sent to the console.
        /// </summary>
        private Queue<(string message, ESeverity severity)> mMessageQueue;

        /// <summary>
        /// Thread responsible for sending messages to the console.
        /// </summary>
        private Thread mThread;

        /// <summary>
        /// Stores the value of <see cref="ShouldExit"/>.
        /// </summary>
        private long mShouldExit = 0;

        /// <summary>
        /// Stores the value of <see cref="ConsoleIsOpen"/>.
        /// </summary>
        private long mConsoleIsOpen = 0;

        /// <summary>
        /// Logger represented by this proxy.
        /// </summary>
        public readonly Logger representedLogger;

        
        // --- Properties ---


        /// <summary>
        /// Indicates whether the <see cref="mThread"/> should stop it's execution.
        /// </summary>
        public bool ShouldExit
        {
            get => Interlocked.Read(ref mShouldExit) == 1;
            set => Interlocked.CompareExchange(ref mShouldExit, value ? 1 : 0, value ? 0 : 1);
        }

        /// <summary>
        /// Indicates if the console is currently open.
        /// </summary>
        public bool ConsoleIsOpen
        {
            get => Interlocked.Read(ref mConsoleIsOpen) == 1;
            private set => Interlocked.CompareExchange(ref mConsoleIsOpen, value ? 1 : 0, value ? 0 : 1);
        }

        /// <summary>
        /// 
        /// Determines if <see cref="mMessageQueue"/> is thread-safely empty.
        /// </summary>
        public bool MessageQueueIsEmpty => ThreadSafeTryCatch<bool>(() => mMessageQueue.Count == 0);

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger">Logger represented by this proxy</param>
        public ConsoleLoggerProxy(Logger logger)
        {
            representedLogger = logger;

            mMessageQueue = new Queue<(string message, ESeverity severity)>();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initialize this proxy.
        /// </summary>
        /// <param name="consoleTitle">Title of the console window</param>
        /// <param name="loggerAbsolutePath">Absolute path to the console logger</param>
        /// <param name="waitUserInputBeforeClosing">Indicates whether the console should wait for the user to press any key before exiting.</param>
        /// <param name="waitConsoleClose">Indicates whether the thread should wait for the console to close before finishing its execution.</param>
        /// <param name="waitMessageQueueEmpty">Indicates whether the thread should send all messages in the queue before finishing its execution.</param>
        public void Start(
            string consoleTitle,
            string loggerAbsolutePath,
            bool waitUserInputBeforeClosing,
            bool waitConsoleClose,
            bool waitMessageQueueEmpty)
        {
            // Subscribe to the logger events to receive its logs.
            representedLogger.OnMessageLogged += LogReceivedHandler;
            representedLogger.OnCrashLogged += CrashReceivedHandler;

            mThread = new Thread(() =>
            {
                // Console process.
                Process console = new Process();

                // ConsoleLogger path.
                console.StartInfo.FileName = loggerAbsolutePath;
                // We do not run the console using the shell because this is a .Net Core app.
                console.StartInfo.UseShellExecute = false;

                // Console title.
                console.StartInfo.ArgumentList.Add(consoleTitle);
                // Indicate whether console should wait user input before closing.
                console.StartInfo.ArgumentList.Add(waitUserInputBeforeClosing.ToString());

                using (AnonymousPipeServerStream pipe = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable))
                {
                    // We pass the pipe handle to the console.
                    console.StartInfo.ArgumentList.Add(pipe.GetClientHandleAsString());

                    // Start console process.
                    console.Start();

                    // Subscribe to the exit event so we can finish this thread once the console is closed.
                    console.Exited += (sender, args) =>
                    {
                        ConsoleIsOpen = false;
                        
                        representedLogger.OnMessageLogged -= LogReceivedHandler;
                        representedLogger.OnCrashLogged -= CrashReceivedHandler;
                    };

                    ConsoleIsOpen = true;

                    try
                    {
                        using (StreamWriter pipeWriter = new StreamWriter(pipe))
                        {
                            pipeWriter.AutoFlush = true;

                            // Pass the broadcast begin flag.
                            pipeWriter.WriteLine(Constants.Constants.BroadcastBeginFlag);

                            // Loop while the console is open and the thread doesn't have to end it's execution or empty the message queue.
                            while ((!ShouldExit || (waitMessageQueueEmpty && !MessageQueueIsEmpty)) && ConsoleIsOpen)
                            {
                                // Indicate if we could retrieve the lock.
                                bool retrievedLock = false;

                                string currentMessage = string.Empty;

                                ESeverity currentSeverity = ESeverity.Info;

                                try
                                {
                                    // Try retrieving the lock.
                                    Monitor.TryEnter(mLock, Int32.MaxValue, ref retrievedLock);

                                    // If we could not retrieve the lock, we continue the iteration and try it again.
                                    if (!retrievedLock)
                                    {
                                        Thread.Sleep(5);

                                        continue;
                                    }

                                    // If we could retrieve the lock, but the message queue is empty:
                                    if (mMessageQueue.Count == 0)
                                    {
                                        // Free the lock and wait.
                                        Monitor.Wait(mLock, 200);

                                        // If the message queue is still empty, we continue the iteration.
                                        if (mMessageQueue.Count == 0)
                                            continue;
                                    }

                                    // Get the first log in row.
                                    var log = mMessageQueue.Dequeue();

                                    currentMessage = log.message;
                                    currentSeverity = log.severity;
                                }
                                catch (Exception ex)
                                {

                                }
                                finally
                                {
                                    // Before iterating again, we release the lock if we have retrieved it before.
                                    if (retrievedLock)
                                    {
                                        Monitor.Pulse(mLock);
                                        Monitor.Exit(mLock);
                                    }
                                }

                                // We send the message.
                                pipeWriter.WriteLine(Constants.Constants.MessageFlag);
                                pipeWriter.WriteLine(currentMessage);
                                pipeWriter.WriteLine(Constants.Constants.SeverityFlag);
                                pipeWriter.WriteLine(currentSeverity.ToString());
                                pipeWriter.WriteLine(Constants.Constants.EndMessageFlag);
                            }

                            // We send the ending of the broadcast add.
                            pipeWriter.WriteLine(Constants.Constants.BroadcastEndFlag);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Close the console if any error occurs.
                        console.Close();
                    }
                }

                // Wait for the console to close if we should.
                if (waitConsoleClose)
                {
                    console.WaitForExit();
                }

            });

            // Assign the console title to the thread.
            mThread.Name = $"Proxy ({consoleTitle} - Console)";

            // The thread must be a background thread so that the application can finish without having to wait for it.
            mThread.IsBackground = true;

            // Start the thread.
            mThread.Start();
        }

        /// <summary>
        /// Wait for thread to end its execution.
        /// </summary>
        public void WaitForThread()
        {
            mThread.Join();
        }

        /// <summary>
        /// Deals with the event of receiving a log.
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="severity">Log severity</param>
        private void LogReceivedHandler(string message, ESeverity severity)
        {
            // We try to add the log to the queue asynchronously so as not to clog the main thread.
            Task.Run(() =>
            {
                ThreadSafeTryCatch(() => { mMessageQueue.Enqueue((message, severity)); });
            });
        }

        /// <summary>
        /// Deals with the event of receiving a crash.
        /// </summary>
        /// <param name="message">Logged crash message</param>
        private void CrashReceivedHandler(string message)
        {
            // We try to add the log to the message queue asynchronously so as not to clog the main thread.
            Task.Run(() =>
            {
                ThreadSafeTryCatch(() =>
                {
                    // We are not interested in displaying the rest of the logs after having crashed, so we clear the queue.
                    mMessageQueue.Clear();

                    mMessageQueue.Enqueue((message, ESeverity.CriticalError));
                });

                // The thread should end it's execution because a crash occurred.
                ShouldExit = true;
            });
        }

        /// <summary>
        /// Thread-safely executes a <paramref name="safeDelegate"/> inside a TryCatch.
        /// </summary>
        /// <param name="safeDelegate">Delegate to execute</param>
        private void ThreadSafeTryCatch(Action safeDelegate)
        {
            bool retrievedLock = false;

            try
            {
                Monitor.TryEnter(mLock, 300, ref retrievedLock);

                if (retrievedLock)
                {
                    safeDelegate();
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                if (retrievedLock)
                {
                    Monitor.Pulse(mLock);
                    Monitor.Exit(mLock);
                }
            }
        }

        /// <summary>
        /// Thread-safely executes a <paramref name="safeDelegate"/> with <typeparamref name="TResult"/> return type inside a TryCatch.
        /// </summary>
        /// <param name="safeDelegate"></param>
        /// <typeparam name="TResult">Return type of <see cref="safeDelegate"/></typeparam>
        /// <returns>The result of executing <see cref="safeDelegate"/></returns>
        private TResult ThreadSafeTryCatch<TResult>(Func<TResult> safeDelegate)
        {
            bool retrievedLock = false;

            try
            {
                Monitor.TryEnter(mLock, 300, ref retrievedLock);

                if (retrievedLock)
                {
                    return safeDelegate();
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                if (retrievedLock)
                {
                    Monitor.Pulse(mLock);
                    Monitor.Exit(mLock);
                }
            }

            return default;
        }

        #endregion
    }
}
