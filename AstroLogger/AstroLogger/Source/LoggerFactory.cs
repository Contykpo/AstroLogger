namespace AstroLogger
{
    /// <summary>
    /// Abstract class that contains fields, properties, and methods needed by a class to implement the functionality of a logger factory.
    /// </summary>
    public abstract class LoggerFactory
    {
        #region Fields and Properties

        protected string mLogsPath;
        protected string mCrashesPath;
        protected string mLogsFormat;
        
        protected int mMaximumAmountOfLogs;
        protected int mMaximumAmountOfCrashes;
        
        protected ESeverity mSeverity;


        // --- Properties ---


        /// <summary>
        /// <see cref="List{T}"/> of the <see cref="Logger"/> created by this factory.
        /// </summary>
        protected List<Logger> mLoggers = new List<Logger>();

        /// <summary>
        /// Absolute path to the directory where logs are saved.
        /// </summary>
        public string LogsPath => mLogsPath;

        /// <summary>
        /// Absolute path to the directory where crashes are saved.
        /// </summary>
        public string CrashesPath => mCrashesPath;

        /// <summary>
        /// Format used by the <see cref="Logger"/> in its logs.
        /// </summary>
        public string LogsFormat => mLogsFormat;

        /// <summary>
        /// Maximum amount of logs before begining log deletion.
        /// </summary>
        public int MaximumAmountOfLogs => mMaximumAmountOfLogs;

        /// <summary>
        /// Maximum amount of logs before begining crashes deletion.
        /// </summary>
        public int MaximumAmountOfCrashes => mMaximumAmountOfCrashes;

        /// <summary>
        /// Severities managed by the <see cref="Logger"/>s
        /// </summary>
        public ESeverity Severity => mSeverity;

        /// <summary>
        /// <see cref="List{T}"/> of the <see cref="Logger"/> created by this factory.
        /// </summary>
        public List<Logger> Loggers => mLoggers;

        #endregion

        #region Methods

        /// <summary>
        /// Initializes this instance with the values passed by the parameters.
        /// </summary>
        /// <param name="severity">Severities managed by the loggers</param>
        /// <param name="format">Format given to messages by the loggers</param>
        /// <param name="extraParameter">Extra parameter for custom implementations</param>
        /// <param name="logsPath">Absolute path where logs will be stored</param>
        /// <param name="crashesPath">Absolute path where crashes will be stored</param>
        /// <param name="maximumAmountOfLogs">Maximum amount of logs, once logs exceed that amount they will be deleted</param>
        /// <param name="maximumAmountOfCrashes">Maximum amount of crashes, once logs exceed that amount they will be deleted</param>
        public virtual void Initialize(ESeverity severity, string format, object extraParameter = null, string logsPath = "", string crashesPath = "", int maximumAmountOfLogs = 5, int maximumAmountOfCrashes = 5)
        {
            mSeverity = severity;
            mLogsFormat = format;
            mMaximumAmountOfLogs = maximumAmountOfLogs;
            mMaximumAmountOfCrashes = maximumAmountOfCrashes;

            if (string.IsNullOrWhiteSpace(logsPath) || !Path.IsPathFullyQualified(logsPath))
            {
                string temporaryDirectory = Path.Combine(Directory.GetCurrentDirectory(), "tmp");
                string logsDirectory = Path.Combine(temporaryDirectory, "Logs");

                if (!Directory.Exists(temporaryDirectory)) Directory.CreateDirectory(temporaryDirectory);

                if (!Directory.Exists(logsDirectory)) Directory.CreateDirectory(logsDirectory);

                mLogsPath = logsDirectory;
            }
            else
            {
                mLogsPath = logsPath;
            }

            if (string.IsNullOrWhiteSpace(crashesPath) || !Path.IsPathFullyQualified(crashesPath))
            {
                string crashesDirectory = Path.Combine(Directory.GetCurrentDirectory(), "tmp", "Crashes");

                if (!Directory.Exists(crashesDirectory)) Directory.CreateDirectory(crashesDirectory);

                mCrashesPath = crashesDirectory;
            }
            else
            {
                mCrashesPath = crashesPath;
            }
        }

        /// <summary>
        /// Creates a new instance of a <see cref="Logger"/> that uses the format and <see cref="ESeverity"/> specified in the instance of a <see cref="LoggerFactory"/>
        /// </summary>
        /// <param name="name">Logger name.</param>
        /// <param name="fileName">Name of the output file containing the logs. Must be empty if no file is written.</param>
        /// <param name="extraParameter">Extra parameter for custom implementations.</param>
        /// <returns></returns>
        public abstract Logger CreateLogger(string name = "", string fileName = "", object extraParameter = null);

        /// <summary>
        /// Terminates all the loggers if necesary.
        /// </summary>
        public abstract void End();

        /// <summary>
        /// Spreads a message to all <see cref="Logger"/>s created by this instace.
        /// </summary>
        /// <param name="message">Message to be spread</param>
        /// <param name="severity">Severity of the message</param>
        /// <param name="destination">Destination of the message</param>
        public virtual void MensajeDifundido(string message, ESeverity severity, EDestination destination)
        {
            for (int i = 0; i < mLoggers.Count; ++i)
            {
                mLoggers[i].Log(message, severity, destination);
            }
        }

        #endregion
    }
}
