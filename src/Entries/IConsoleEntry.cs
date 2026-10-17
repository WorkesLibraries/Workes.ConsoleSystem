using System;

namespace Workes.ConsoleSystem.Entries;

/// <summary>
/// Represents an item that can appear in the shared console history.
/// </summary>
public interface IConsoleEntry
{
    /// <summary>
    /// Gets the time at which the entry was created or submitted.
    /// </summary>
    DateTimeOffset Timestamp { get; }
}
