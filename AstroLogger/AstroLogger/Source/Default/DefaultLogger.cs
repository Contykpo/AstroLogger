using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AstroLogger
{
    /// <summary>
    /// Default implementation of the <see cref="Logger"/> interface.
    /// </summary>
    public sealed class DefaultLogger : FileLogger
    {
        #region Constructor

        public DefaultLogger(LoggerFactory factory, string name, string fileName) : base(factory, name, fileName) { }

        #endregion

        #region Methods

        public override void Log(
            string messageContent,
            ESeverity severity = ESeverity.Debug,
            EDestination destination = EDestination.ALL,
            [CallerFilePath] string fileCaller = "",
            [CallerMemberName] string function = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if ((severity & Factory.Severity) == 0) return;

            if ((severity & ESeverity.CriticalError) != 0)
            {
                LogCrash(messageContent, fileCaller, function, lineNumber);

                return;
            }

            Message message = new Message(messageContent, severity, destination, fileCaller, function, lineNumber, this);

            string formattedMessage = message.ApplyFormat(Factory.LogsFormat);

            // If the destination includes a console, then we also write the message to the console.
            if ((destination & EDestination.Console) != 0)
            {
                Trace.WriteLine(formattedMessage);
            }

            // If the destination includes the text file, we write the message to the file.
            if (mFileAccessController != null && (destination & EDestination.File) != 0)
            {
                mFileAccessController.Write(formattedMessage);
            }

            FireOnMessageLoggedEvent(formattedMessage, severity);
        }

        public override void LogCrash(
            string messageContent,
            [CallerFilePath] string fileCaller = "",
            [CallerMemberName] string function = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                Message message = new Message(messageContent, ESeverity.CriticalError, EDestination.ALL, fileCaller, function, lineNumber, this);

                string formattedMessage = message.ApplyFormat(Factory.LogsFormat);

                Console.WriteLine(formattedMessage);

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
                FireOnCrashLoggedEvent(messageContent);

                // Throw the exception.
                throw new Exception(messageContent);
            }
        }

        #endregion
    }
}
