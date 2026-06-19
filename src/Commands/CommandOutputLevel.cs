namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Describes the severity or purpose of command output.
/// </summary>
public enum CommandOutputLevel
{
    /// <summary>
    /// General command output.
    /// </summary>
    Information,

    /// <summary>
    /// Command output that should draw attention to a possible issue.
    /// </summary>
    Warning,

    /// <summary>
    /// Command output that describes a failure.
    /// </summary>
    Error
}
