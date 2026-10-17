using System;
using System.Collections.Generic;
using Workes.ConsoleSystem.Configuration;

namespace Workes.ConsoleSystem.History;

/// <summary>
/// Stores previously submitted command strings for console UI navigation.
/// </summary>
public sealed class CommandHistory
{
    private const int DefaultCapacity = 100;

    private readonly List<string> _entries = new List<string>();
    private readonly CommandHistoryDuplicatePolicy _duplicatePolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandHistory"/> class with default settings.
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
    /// Gets a detached snapshot of submitted command inputs in oldest-first order.
    /// </summary>
    public IReadOnlyList<string> Entries => Array.AsReadOnly(_entries.ToArray());

    /// <summary>
    /// Occurs synchronously after retained command inputs change.
    /// </summary>
    public event EventHandler<CommandHistoryChangedEventArgs>? Changed;

    /// <summary>
    /// Removes all retained submitted command inputs.
    /// </summary>
    public void Clear()
    {
        if (_entries.Count == 0)
        {
            return;
        }

        EventHandler<CommandHistoryChangedEventArgs>? changed = Changed;
        string[]? clearedInputs = changed is null ? null : _entries.ToArray();
        _entries.Clear();
        if (changed is not null && clearedInputs is not null)
        {
            changed.Invoke(this, new CommandHistoryChangedEventArgs(
                HistoryChangeKind.Cleared,
                Array.Empty<string>(),
                Array.Empty<string>(),
                clearedInputs,
                _entries.Count));
        }
    }

    internal bool Add(string input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        if (string.IsNullOrWhiteSpace(input) || IsRejectedDuplicate(input))
        {
            return false;
        }

        EventHandler<CommandHistoryChangedEventArgs>? changed = Changed;
        string[] droppedInputs = Array.Empty<string>();
        if (_entries.Count == Capacity)
        {
            if (changed is not null)
            {
                droppedInputs = new[] { _entries[0] };
            }

            _entries.RemoveAt(0);
        }

        _entries.Add(input);
        if (changed is not null)
        {
            changed.Invoke(this, new CommandHistoryChangedEventArgs(
                HistoryChangeKind.Added,
                new[] { input },
                droppedInputs,
                Array.Empty<string>(),
                _entries.Count));
        }
        return true;
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
