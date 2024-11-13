namespace AstroLogger
{
    /// <summary>
    /// Represents the contents of a log and it's origin.
    /// </summary>
    public class Message
    {
        #region Fields and Properties

        /// <summary>
        /// Content of a log.
        /// </summary>
        public string Content;

        /// <summary>
        /// Severity of a log.
        /// </summary>
        public readonly ESeverity Severity;

        /// <summary>
        /// Destination of a log.
        /// </summary>
        public readonly EDestination Destination;

        /// <summary>
        /// Name of the file where the log generated.
        /// </summary>
        public readonly string FullFileName;

        /// <summary>
        /// Name of the function where the log generated.
        /// </summary>
        public readonly string FunctionName;

        /// <summary>
        /// Number of the line where the log generated.
        /// </summary>
        public readonly int LineNumber;

        /// <summary>
        /// Name of the logger where the log generated.
        /// </summary>
        public readonly Logger Origin;

        /// <summary>
        /// Name of the file that generated the log.
        /// </summary>
        public string FileName => Path.GetFileName(FullFileName);

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="content">Log content</param>
        /// <param name="severity">Severity of the log</param>
        /// <param name="fileName">Name of the file where the log generated</param>
        /// <param name="functionName">Name of the function where the log generated</param>
        /// <param name="lineNumber">Number of the line where the log generated</param>
        /// <param name="origin"><see cref="Logger"/> that generated the log/param>
        public Message(string content, ESeverity severity, EDestination destination, string fileName, string functionName, int lineNumber, Logger origin)
        {
            Content = content;
            Severity = severity;
            Destination = destination;
            FullFileName = fileName;
            FunctionName = functionName;
            LineNumber = lineNumber;
            Origin = origin;
        }

        #endregion
    }
}
