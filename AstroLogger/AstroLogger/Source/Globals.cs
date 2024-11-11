using System.Runtime.CompilerServices;

namespace AstroLogger
{
    /// <summary>
    /// Static global variables needed across the program lifetime.
    /// </summary>
    public static class Globals
    {
        #region Fields and Properties

        /// <summary>
        /// Contains the value of <see cref="Factory"/>
        /// </summary>
        private static LoggerFactory mFactory;

        /// <summary>
        /// Contains the value of <see cref="GlobalLogger"/>
        /// </summary>
        private static Logger mGlobalLogger;

        /// <summary>
        /// Contains the value of <see cref="ProxyLoggerGlobalConsola"/>
        /// </summary>
        private static ConsoleLoggerProxy mConsoleLoggerProxy;

        /// <summary>
        /// Global instance of <see cref="LoggerFactory"/>
        /// </summary>
        public static LoggerFactory Factory => mFactory;

        /// <summary>
        /// Global instance of a <see cref="Logger"/> responsible for the general logs.
        /// </summary>
        public static Logger GlobalLogger => mGlobalLogger;

        /// <summary>
        /// Intermediate between the global logger and the console that displays the logs
        /// </summary>
        public static ConsoleLoggerProxy GlobalConsoleLoggerProxy => mConsoleLoggerProxy;

        #endregion

        #region Methods

        /// <summary>
        /// Initialize global fields.
        /// </summary>
        /// <typeparam name="TFactory">Type of <see cref="LoggerFactory"/> that will be instantiated to create the global loggger.</typeparam>
        /// <param name="severity"><see cref="ESeverity"/>that will be logged</param>
        /// <param name="format">Format given to logger messages</param>
        /// <param name="globalLoggerName">Name of the global <see cref="Logger"/></param>
        /// <param name="globalLogsFileName">Name given to logger files</param>
        /// <param name="extra">Extra parameter passed to the <see cref="LoggerFactory"/>'s constructor</param>
        public static void Initialize<TFactory>(
            ESeverity severity,
            string format,
            string globalLoggerName,
            string globalLogsFileName = "",
            object extra = null)
            where TFactory : LoggerFactory
        {
            mFactory = Activator.CreateInstance<TFactory>();

            mFactory.Initialize(severity, format, extra);

            mGlobalLogger = mFactory.CreateLogger(globalLoggerName, globalLogsFileName);
        }

        /// <summary>
        /// Assigns <see cref="ConsoleGlobalLoggerProxy"/> to a new instance and initializes it.
        /// </summary>
        /// <param name="consoleTitle">Console title</param>
        /// <param name="loggerExecutablePath">Absolute path to the logger executable</param>
        /// <param name="waitUserInputBeforeClosing">Indicates whether the console must wait for the user input before closing</param>
        /// <param name="waitConsoleClosing">Indicates whether the application must wait for the console to close before exit</param>
        /// <param name="waitForAllMessagesToBeSent">Indicates whether the proxy thread must wait for all the messages to had been sent before exit</param>
        public static void InitializeConsoleLoggerProxy(
            string consoleTitle,
            string loggerExecutablePath,
            bool waitUserInputBeforeClosing,
            bool waitConsoleClosing,
            bool waitForAllMessagesToBeSent)
        {
            if (mGlobalLogger == null)
            {
                throw new NullReferenceException($"{nameof(GlobalLogger)} is null. Verify that {nameof(Initialize)} is being called before attempting to create a proxy.");
            }

            mConsoleLoggerProxy = new ConsoleLoggerProxy(GlobalLogger);

            mConsoleLoggerProxy.Start(consoleTitle, loggerExecutablePath, waitUserInputBeforeClosing, waitConsoleClosing, waitForAllMessagesToBeSent);
        }

        /// <summary>
        /// Attempts to send a log through the global logger.
        /// If the global logger is null, then the log message is sent to the console.
        /// This method should only be used when whe are not sure the global logger is valid.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="severity"></param>
        /// <param name="destination"></param>
        public static void AttemptLog(
            this string message,
            ESeverity severity = ESeverity.Debug,
            EDestination destination = EDestination.ALL,
            [CallerFilePath] string fileCaller = "",
            [CallerMemberName] string functionCaller = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (mGlobalLogger != null)
            {
                mGlobalLogger.Log(message, severity, destination, fileCaller, functionCaller, lineNumber);
            }
            else
            {
                Console.WriteLine($"[{severity}]: {message}");
            }
        }

        /// <summary>
        /// Calls the end function of <see cref="LoggerFactory"/>
        /// </summary>
        public static void End()
        {
            mConsoleLoggerProxy.ShouldExit = true;
            mConsoleLoggerProxy.WaitForThread();

            mFactory.End();
        }

        #endregion
    }
}
