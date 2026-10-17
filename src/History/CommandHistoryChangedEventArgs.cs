using System;
using System.Collections.Generic;

namespace Workes.ConsoleSystem.History;

/// <summary>
/// Provides data for command history change events.
/// </summary>
public sealed class CommandHistoryChangedEventArgs : EventArgs
{
    internal CommandHistoryChangedEventArgs(
        HistoryChangeKind changeKind,
        IReadOnlyList<string> addedInputs,
        IReadOnlyList<string> droppedInputs,
        IReadOnlyList<string> clearedInputs,
        int retainedCount)
    {
        ChangeKind = changeKind;
        AddedInputs = Copy(addedInputs, nameof(addedInputs));
        DroppedInputs = Copy(droppedInputs, nameof(droppedInputs));
        ClearedInputs = Copy(clearedInputs, nameof(clearedInputs));
        RetainedCount = retainedCount;
    }

    /// <summary>
    /// Gets the kind of history change.
    /// </summary>
    public HistoryChangeKind ChangeKind { get; }

    /// <summary>
    /// Gets the command inputs added by this change.
    /// </summary>
    public IReadOnlyList<string> AddedInputs { get; }

    /// <summary>
    /// Gets the command inputs dropped by capacity overflow during this change.
    /// </summary>
    public IReadOnlyList<string> DroppedInputs { get; }

    /// <summary>
    /// Gets the command inputs removed by a clear operation.
    /// </summary>
    public IReadOnlyList<string> ClearedInputs { get; }

    /// <summary>
    /// Gets the retained command input count after this change.
    /// </summary>
    public int RetainedCount { get; }

    private static IReadOnlyList<string> Copy(IReadOnlyList<string> inputs, string parameterName)
    {
        if (inputs is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        var copy = new string[inputs.Count];
        for (int i = 0; i < inputs.Count; i++)
        {
            copy[i] = inputs[i];
        }

        return Array.AsReadOnly(copy);
    }
}
