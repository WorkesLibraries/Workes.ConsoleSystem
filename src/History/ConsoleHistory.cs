using System;
using System.Collections.Generic;
using Workes.ConsoleSystem.Entries;

namespace Workes.ConsoleSystem.History;

/// <summary>
/// Represents the shared chronological collection of console entries.
/// </summary>
public sealed class ConsoleHistory
{
    private const int DefaultCapacity = 200;

    private readonly List<IConsoleEntry> _entries = new List<IConsoleEntry>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleHistory"/> class.
    /// </summary>
    public ConsoleHistory()
        : this(DefaultCapacity)
    {
    }

    internal ConsoleHistory(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity values must be greater than zero.");
        }

        Capacity = capacity;
    }

    /// <summary>
    /// Gets the maximum number of console entries retained.
    /// </summary>
    public int Capacity { get; }

    /// <summary>
    /// Gets the entries currently present in the console history.
    /// </summary>
    public IReadOnlyList<IConsoleEntry> Entries => _entries;

    /// <summary>
    /// Removes all retained console entries.
    /// </summary>
    public void Clear()
    {
        _entries.Clear();
    }

    internal void Add(IConsoleEntry entry)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        if (_entries.Count == Capacity)
        {
            _entries.RemoveAt(0);
        }

        _entries.Add(entry);
    }
}
