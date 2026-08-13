using System;
using System.Collections.Generic;
using Workes.ConsoleSystem.Configuration;

namespace Workes.ConsoleSystem.History;

/// <summary>
/// Represents previously submitted interactive command strings for UI navigation.
/// </summary>
public sealed class CommandHistory
{
    private const int DefaultCapacity = 100;

    private readonly List<string> _entries = new List<string>();
    private readonly CommandHistoryDuplicatePolicy _duplicatePolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandHistory"/> class.
    /// </summary>
    public CommandHistory()
        : this(DefaultCapacity, CommandHistoryDuplicatePolicy.RejectConsecutive)
    {
    }

    internal CommandHistory(int capacity, CommandHistoryDuplicatePolicy duplicatePolicy)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity values must be greater than zero.");
        }

        if (!Enum.IsDefined(typeof(CommandHistoryDuplicatePolicy), duplicatePolicy))
        {
            throw new ArgumentOutOfRangeException(nameof(duplicatePolicy), duplicatePolicy, "Unknown command history duplicate policy.");
        }

        Capacity = capacity;
        _duplicatePolicy = duplicatePolicy;
    }

    /// <summary>
    /// Gets the maximum number of submitted command inputs retained.
    /// </summary>
    public int Capacity { get; }

    /// <summary>
    /// Gets the submitted command strings.
    /// </summary>
    public IReadOnlyList<string> Entries => _entries;

    /// <summary>
    /// Adds submitted command input to the history.
    /// </summary>
    /// <param name="input">The submitted command input.</param>
    /// <returns><c>true</c> when the input was retained; otherwise, <c>false</c>.</returns>
    public bool Add(string input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        if (IsRejectedDuplicate(input))
        {
            return false;
        }

        if (_entries.Count == Capacity)
        {
            _entries.RemoveAt(0);
        }

        _entries.Add(input);
        return true;
    }

    /// <summary>
    /// Removes all retained submitted command inputs.
    /// </summary>
    public void Clear()
    {
        _entries.Clear();
    }

    private bool IsRejectedDuplicate(string input)
    {
        if (_duplicatePolicy != CommandHistoryDuplicatePolicy.RejectConsecutive || _entries.Count == 0)
        {
            return false;
        }

        string newestInput = _entries[_entries.Count - 1];
        return string.Equals(newestInput.Trim(), input.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
