using System.Runtime.CompilerServices;

namespace AstroLogger
{
    public sealed class DefaultLoggerAsync : FileLogger
    {
        #region Fields

        private MessagesDispatcherThread mMessageDispatcherThread;

        #endregion

        #region Constructor

        public DefaultLoggerAsync(LoggerFactory factory, string name, string fileName) : base(factory, name, fileName)
        {
            mMessageDispatcherThread = new MessagesDispatcherThread(DispatchMessage, name);
        }

        #endregion

        #region Methods

        public async override void Log(
            string messageContent,
            ESeverity severity = ESeverity.Debug,
            EDestination destination = EDestination.ALL,
            [CallerFilePath] string fileCaller = "",
            [CallerMemberName] string function = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if ((severity & Factory.Severity) == 0) return;

            Message message = new Message(messageContent, severity, destination, fileCaller, function, lineNumber, this);

            // Add the message to the queue without waiting for the function to end.
            await mMessageDispatcherThread.TryAddingMessageAsync(message);
        }

        /// <summary>
        /// Non-asynchronous function for logging the crashes.
        /// This is because we cannot wait to log a crash, it must be immediate.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="fileCaller"></param>
        /// <param name="function"></param>
        /// <param name="lineNumber"></param>
        public override void LogCrash(
            string messageContent,
            [CallerFilePath] string fileCaller = "",
            [CallerMemberName] string function = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Message message = new Message(messageContent, ESeverity.CriticalError, EDestination.ALL, fileCaller, function, lineNumber, this);

            string formattedMessage = message.ApplyFormat(Factory.LogsFormat);

            Console.WriteLine(formattedMessage);

            try
            {
                if (mFileAccessController != null)
                {
                    ClearLogSurplus(ELogType.Crash);

                    mFileAccessController.Write(formattedMessage);

                    if (!mFileAccessController.MoveFile(Path.GetFullPath(Path.Combine(Factory.CrashesPath, $"Crash {DateTime.Now.ToString("dd-mm (hh-mm-ss tt)")}.crash"))))
                    {
                        throw new Exception("An error ocurred while attempting to log the crash. - Could not move the file.");
                    }
                }
                else
                {
                    throw new Exception("An error ocurred while attempting to log the crash. - FileAccessController is null.");
                }
            }
            catch (Exception ex)
            {
                string.Format($"An error ocurred while attempting to log the crash: {ex.Message}{Environment.NewLine}").AttemptLog(ESeverity.Error);
            }
            finally
            {
                // Throw the exception.
                throw new Exception(messageContent);
            }
        }

        public async override void End()
        {
            base.End();

            await mMessageDispatcherThread.Stop();
        }

        private void DispatchMessage(Message message)
        {
            string formattedMessage = message.ApplyFormat(Factory.LogsFormat);

            if ((message.Destination & EDestination.Console) != 0)
            {
                Console.WriteLine(formattedMessage);
            }

            if (mFileAccessController != null && (message.Destination & EDestination.File) != 0)
            {
                mFileAccessController.Write(formattedMessage);
            }
        }

        #endregion
    }
}
