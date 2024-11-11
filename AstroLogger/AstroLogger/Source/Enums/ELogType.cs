namespace AstroLogger
{
    /// <summary>
    /// Enum that represents the different types of log files.
    /// </summary>
    [Flags]
    public enum ELogType : byte
    {
        /// <summary>
        /// Represents the logs that did not end with a crash.
        /// </summary>
        General = 1 << 0,

        /// <summary>
        /// Represents the logs that ended with a crash.
        /// </summary>
        Crash = 1 << 1,

        /// <summary>
        /// All the above.
        /// </summary>
        All = General | Crash,

        /// <summary>
        /// No value.
        /// </summary>
        NONE = 0
    }
}
