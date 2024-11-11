using AstroLogger;

namespace AstroLogger.Delegates
{
    /// <summary>
    /// Delegate that refers to a method destined to message loggin events.
    /// </summary>
    /// <param name="message">Logged message</param>
    /// <param name="severity">Log message severity</param>
    public delegate void DLoggedMessage(string message, ESeverity severity);

    /// <summary>
    /// Delegate that refers to a method destined to manage crash log events.
    /// </summary>
    /// <param name="message">Crash message</param>
    public delegate void DLoggedCrash(string message);
}