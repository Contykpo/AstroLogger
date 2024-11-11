using AstroLogger;

namespace AstroLogger.Utilities
{
    public static class Utilities
    {
        /// <summary>
        /// Logs a <paramref name="message"/> when the <paramref name="assertion"/> value is es false.
        /// </summary>
        /// <param name="assertion">Boolean expression to be evaluated</param>
        /// <param name="logger">Logger that will log the <paramref name="message"/></param>
        /// <param name="message">Message that will be logged</param>
        /// <param name="severity">Severity of the <paramref name="message"/></param>
        /// <param name="destination">Destination of the <paramref name="message"/></param>
        /// <returns>The value of <paramref name="assertion"/></returns>
        public static bool Assertion(this bool assertion, Logger logger, string message, ESeverity severity, EDestination destination = EDestination.ALL)
        {
            if (!assertion)
            {
                // If the given logger is not null, we use it to log the message.
                if (logger != null)
                {
                    logger.Log(message, severity, destination);
                }
                // If the logger is null, we try logging the message with the global logger.
                else
                {
                    message.AttemptLog(severity, destination);
                }
            }

            return assertion;
        }

        /// <summary>
        /// Logs a <paramref name="message"/> when the <paramref name="assertion"/> value is es false.
        /// </summary>
        /// <param name="assertion">Boolean expression to be evaluated</param>
        /// <param name="message">Message that will be logged</param>
        /// <param name="severity">Severity of the <paramref name="message"/></param>
        /// <param name="destination">Destination of the <paramref name="message"/></param>
        /// <returns>The value of <paramref name="assertion"/></returns>
        public static bool Assertion(this bool assertion, string message, ESeverity severity, EDestination destination = EDestination.ALL)
        {
            return Assertion(assertion, Globals.GlobalLogger, message, severity, destination);
        }
    }
}
