using System;
using System.Collections.Generic;
using Workes.ConsoleSystem.Entries;

namespace Workes.ConsoleSystem.History;

/// <summary>
/// Provides data for console history change events.
/// </summary>
public sealed class ConsoleHistoryChangedEventArgs : EventArgs
{
    internal ConsoleHistoryChangedEventArgs(
        HistoryChangeKind changeKind,
        IReadOnlyList<IConsoleEntry> addedEntries,
        IReadOnlyList<IConsoleEntry> droppedEntries,
        IReadOnlyList<IConsoleEntry> clearedEntries,
        int retainedCount)
    {
        ChangeKind = changeKind;
        AddedEntries = Copy(addedEntries, nameof(addedEntries));
        DroppedEntries = Copy(droppedEntries, nameof(droppedEntries));
        ClearedEntries = Copy(clearedEntries, nameof(clearedEntries));
        RetainedCount = retainedCount;
    }

    /// <summary>
    /// Gets the kind of history change.
    /// </summary>
    public HistoryChangeKind ChangeKind { get; }

    /// <summary>
    /// Gets the entries added by this change.
    /// </summary>
    public IReadOnlyList<IConsoleEntry> AddedEntries { get; }

    /// <summary>
    /// Gets the entries dropped by capacity overflow during this change.
    /// </summary>
    public IReadOnlyList<IConsoleEntry> DroppedEntries { get; }

    /// <summary>
    /// Gets the entries removed by a clear operation.
    /// </summary>
    public IReadOnlyList<IConsoleEntry> ClearedEntries { get; }

    /// <summary>
    /// Gets the retained entry count after this change.
    /// </summary>
    public int RetainedCount { get; }

    private static IReadOnlyList<IConsoleEntry> Copy(IReadOnlyList<IConsoleEntry> entries, string parameterName)
    {
        if (entries is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        var copy = new IConsoleEntry[entries.Count];
        for (int i = 0; i < entries.Count; i++)
        {
            copy[i] = entries[i];
        }

        return Array.AsReadOnly(copy);
    }
}
