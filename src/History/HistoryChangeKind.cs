namespace Workes.ConsoleSystem.History;

/// <summary>
/// Describes how a history collection changed.
/// </summary>
public enum HistoryChangeKind
{
    /// <summary>
    /// One or more items were added to the history.
    /// </summary>
    Added,

    /// <summary>
    /// The history was cleared.
    /// </summary>
    Cleared
}
