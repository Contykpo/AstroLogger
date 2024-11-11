namespace AstroLogger
{
    /// <summary>
    /// Enum that represents the destination where a log will be displayed.
    /// </summary>
    [Flags]
    public enum EDestination : byte
    {
        /// <summary>
        /// Logs are displayed on console.
        /// </summary>
        Console = 1 << 0,

        /// <summary>
        /// Logs are displayed inside a file.
        /// </summary>
        File = 1 << 1,

        /// <summary>
        /// Logs are displayed on console and also, inside a file.
        /// </summary>
        ALL = Console | File,

        /// <summary>
        /// Logs are not displayed.
        /// </summary>
        NONE = 0
    }
}
