using AstroLogger.Delegates;
using System.Runtime.CompilerServices;

namespace AstroLogger
{
    /// <summary>
    /// Interface that contains the methods and properties of <see cref="Logger"/>
    /// </summary>
    public abstract class Logger
    {
        #region Events

        /// <summary>
        /// Event that fires when a message is logged through this logger.
        /// </summary>
        public event DLoggedMessage OnMessageLogged = delegate { };

        /// <summary>
        /// Event that fires when a crash is logged through this logger.
        /// </summary>
        public event DLoggedCrash OnCrashLogged = delegate { };

        #endregion

        #region Properties

        /// <summary>
        /// <see cref="LoggerFactory"/> that created this <see cref="Logger"/>
        /// </summary>
        public LoggerFactory Factory { get; set; }

        /// <summary>
        /// Name of this <see cref="Logger"/>
        /// </summary>
        public string Nombre { get; set; }

        #endregion

        #region Constructor

        public Logger(LoggerFactory _factory, string _nombre)
        {
            Factory = _factory;
            Nombre = _nombre;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Writes a log.
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="severity">Severity of the message</param>
        /// <param name="callerFile">Leave it blank</param>
        /// <param name="callerFunction">Leave it blank</param>
        /// <param name="lineNumber">Leave it blank</param>
        public abstract void Log(string message, ESeverity severity = ESeverity.Debug, EDestination destination = EDestination.ALL,
            [CallerFilePath] string callerFile = "",
            [CallerMemberName] string callerFunction = "",
            [CallerLineNumber] int lineNumber = 0);

        /// <summary>
        /// Writes a log and crashes the application
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="callerFile">Leave it blank</param>
        /// <param name="callerFunction">Leave it blank</param>
        /// <param name="lineNumber">Leave it blank</param>
        public abstract void LogCrash(string message,
            [CallerFilePath] string callerFile = "",
            [CallerMemberName] string callerFunction = "",
            [CallerLineNumber] int lineNumber = 0);

        /// <summary>
        /// If this <see cref="Logger"/> is a <see cref="File"/>, the logger get removed from the Logger List of it's <see cref="LoggerFactory"/>.
        /// Calling this method is not mandatory, however, we might want to call it to release the allocated memory being occupied by the logger.
        /// </summary>
        public virtual void End()
        {
            Factory.Loggers.Remove(this);
        }

        /// <summary>
        /// Fires the <see cref="OnMessageLogged"/> event.
        /// </summary>
        /// <param name="message">Logged message</param>
        /// <param name="severity">Severity of the message</param>
        protected void FireOnMessageLoggedEvent(string message, ESeverity severity) => OnMessageLogged(message, severity);

        /// <summary>
        /// Fires the <see cref="OnCrashLogueado"/ event>
        /// </summary>
        /// <param name="message">Crash message</param>
        protected void FireOnCrashLoggedEvent(string message) => OnCrashLogged(message);

        #endregion
    }
}
