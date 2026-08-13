namespace Workes.ConsoleSystem.Configuration;

/// <summary>
/// Defines how submitted command input history handles duplicate entries.
/// </summary>
public enum CommandHistoryDuplicatePolicy
{
    /// <summary>
    /// Retains every non-blank submitted command input.
    /// </summary>
    Allow,

    /// <summary>
    /// Skips a submitted command input when it duplicates the newest retained input.
    /// </summary>
    RejectConsecutive
}
