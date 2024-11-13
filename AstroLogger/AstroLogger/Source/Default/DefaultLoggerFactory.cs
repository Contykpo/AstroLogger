namespace AstroLogger
{
    /// <summary>
    /// Default implementatino of the <see cref="LoggerFactory"/> interface.
    /// </summary>
    public sealed class DefaultLoggerFactory : LoggerFactory
    {
        #region Constructors

        /// <summary>
        /// Default constructor in case it is need to manually initialize a new instance.
        /// </summary>
		public DefaultLoggerFactory() { }

        /// <summary>
        /// Constructor with all the necessary parameters to initialize a new instance using the Initialize method.
        /// Constructor con parametros que llama Inicializar
        /// </summary>
        /// <param name="severity">Severities accepted by the logger</param>
        /// <param name="format">The format that loggers will apply to their messages</param>
        /// <param name="logsPath">Absolute path where logs will be stored</param>
        /// <param name="crashesPath">Absolute path where crashes will be stored</param>
        /// <param name="maximumLogsAmount">Maximum amount of logs, once exceeded they will begin to be deleted</param>
        /// <param name="maximumCrashesAmount">Maximum amount of crashes, once exceeded they will begin to be deleted</param>
		public DefaultLoggerFactory(ESeverity severity, string format, string logsPath = "", string crashesPath = "", int maximumLogsAmount = 5, int maximumCrashesAmount = 5)
        {
            Initialize(severity, format, null!, logsPath, crashesPath, maximumLogsAmount, maximumCrashesAmount);
        }

        #endregion

        #region Methods

        public override FileLogger CreateLogger(string name = "", string fileName = "", object extraParameter = null)
        {
            FileLogger newLogger = null;

            // If the extra parameter is of type Type:
            if (extraParameter is Type type)
            {
                // If the parameter Type is DefaultLogger, return an instance of that type.
                if (type == typeof(DefaultLogger))
                {
                    newLogger = new DefaultLogger(this, name, fileName);
                }
                // If the parameter Type is DefaultLoggerAsync, return an instance of that type.
                else if (type == typeof(DefaultLoggerAsync))
                {
                    newLogger = new DefaultLoggerAsync(this, name, fileName);
                }
            }
            else
            {
                newLogger = new DefaultLogger(this, name, fileName);
            }

            mLoggers.Add(newLogger!);

            // Remove the excess memory occupied by the list.
            mLoggers.TrimExcess();

            return newLogger!;
        }

        public override void End()
        {
            // We loop the list backwards because as we call the End function the number of elements in the list will decrease.
            for (int i = mLoggers.Count - 1; i >= 0; --i)
            {
                mLoggers[i].End();
            }
        }

        #endregion
    }
}
