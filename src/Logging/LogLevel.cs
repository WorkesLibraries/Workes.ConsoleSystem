namespace Workes.ConsoleSystem.Logging;

/// <summary>
/// Describes the severity of a log entry.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Diagnostic information that is usually only useful while tracing detailed behavior.
    /// </summary>
    Trace,

    /// <summary>
    /// Diagnostic information useful during development or debugging.
    /// </summary>
    Debug,

    /// <summary>
    /// General informational output.
    /// </summary>
    Information,

    /// <summary>
    /// A condition that may require attention.
    /// </summary>
    Warning,

    /// <summary>
    /// A failure or error condition.
    /// </summary>
    Error,

    /// <summary>
    /// A severe failure or error condition.
    /// </summary>
    Critical
}
