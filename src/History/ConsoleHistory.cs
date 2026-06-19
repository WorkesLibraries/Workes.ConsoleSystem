using System.Collections.Generic;
using Workes.ConsoleSystem.Entries;

namespace Workes.ConsoleSystem.History;

/// <summary>
/// Represents the shared chronological collection of console entries.
/// </summary>
public sealed class ConsoleHistory
{
    private readonly List<IConsoleEntry> _entries = new List<IConsoleEntry>();

    /// <summary>
    /// Gets the entries currently present in the console history.
    /// </summary>
    public IReadOnlyList<IConsoleEntry> Entries => _entries;

    internal void Add(IConsoleEntry entry)
    {
        _entries.Add(entry);
    }
}
