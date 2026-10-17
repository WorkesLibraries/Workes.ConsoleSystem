namespace Workes.ConsoleSystem.Configuration;

/// <summary>
/// Configures how command paths are completed.
/// </summary>
public enum CommandPathCompletionMode
{
    /// <summary>
    /// Completes whole command paths.
    /// </summary>
    FullPath,

    /// <summary>
    /// Completes one dot-separated command path segment at a time.
    /// </summary>
    DotSegment
}
