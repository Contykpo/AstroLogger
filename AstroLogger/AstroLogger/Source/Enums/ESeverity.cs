namespace AstroLogger
{
    /// <summary>
    /// Enum that represents the severity of a log.
    /// The severity of message defines whether the message is displayed or not.
    /// </summary>
    [Flags]
    public enum ESeverity : byte
    {
        /// <summary>
        /// Information only relevant for application debugging while still on development.
        /// </summary>
        Debug = 1 << 0,

        /// <summary>
        /// Information on the application events.
        /// </summary>
        Info = 1 << 1,

        /// <summary>
        /// Possible errors or dangerous states that do not pose an issue on the application execution.
        /// </summary>
        Warning = 1 << 2,

        /// <summary>
        /// Errors that have an effect on the user experience or the execution of the application. However, they can be solved on runtime.
        /// </summary>
        Error = 1 << 3,

        /// <summary>
        /// Critical errors that cannot be solved on runtime, ending with an application crash.
        /// </summary>
        CriticalError = 1 << 4,

        /// <summary>
        /// All severity categories.
        /// </summary>
        ALL = Debug | Info | Warning | Error | CriticalError,

        /// <summary>
        /// No severity category.
        /// </summary>
        NONE = 0
    }
}
