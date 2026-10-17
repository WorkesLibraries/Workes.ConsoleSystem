using System;
using System.Collections.Generic;
using Workes.ConsoleSystem.Entries;
using Workes.ConsoleSystem.Internal;
using Workes.ContentSystem.Core;

namespace Workes.ConsoleSystem.History;

/// <summary>
/// Provides ConsoleSystem-owned access to the retained chronological console entries.
/// </summary>
public sealed class ConsoleHistory
{
    private readonly ContentSequenceManager _contentSequence;

    internal ConsoleHistory(ContentSequenceManager contentSequence, int capacity)
    {
        _contentSequence = contentSequence ?? throw new ArgumentNullException(nameof(contentSequence));
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity values must be greater than zero.");
        }

        Capacity = capacity;
        _contentSequence.Changed += OnContentChanged;
    }

    /// <summary>
    /// Gets the maximum number of console entries retained.
    /// </summary>
    public int Capacity { get; }

    /// <summary>
    /// Occurs synchronously after retained console entries change.
    /// </summary>
    public event EventHandler<ConsoleHistoryChangedEventArgs>? Changed;

    /// <summary>
    /// Gets a detached, oldest-first snapshot of the entries currently in history.
    /// </summary>
    public IReadOnlyList<IConsoleEntry> Entries
    {
        get
        {
            IReadOnlyList<ContentEntryRecord> records = _contentSequence.Records;
            var entries = new IConsoleEntry[records.Count];
            for (int i = 0; i < records.Count; i++)
            {
                entries[i] = ((ContentEntryAdapter)records[i].Entry).ConsoleEntry;
            }

            return Array.AsReadOnly(entries);
        }
    }

    /// <summary>
    /// Removes all retained console entries.
    /// </summary>
    public void Clear()
    {
        _contentSequence.Clear();
    }

    internal void RecordEntry(IConsoleEntry entry)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        _contentSequence.Add(new ContentEntryAdapter(entry));
    }

    private void OnContentChanged(object? sender, ContentChangedEventArgs e)
    {
        EventHandler<ConsoleHistoryChangedEventArgs>? changed = Changed;
        if (changed is null)
        {
            return;
        }

        if (e.Cleared)
        {
            changed.Invoke(this, new ConsoleHistoryChangedEventArgs(
                HistoryChangeKind.Cleared,
                Array.Empty<IConsoleEntry>(),
                Array.Empty<IConsoleEntry>(),
                ToConsoleEntries(e.RemovedRecords),
                _contentSequence.Records.Count));
            return;
        }

        if (e.Kind == ContentChangeKind.Added)
        {
            changed.Invoke(this, new ConsoleHistoryChangedEventArgs(
                HistoryChangeKind.Added,
                ToConsoleEntries(e.AddedRecords),
                ToConsoleEntries(e.RemovedRecords),
                Array.Empty<IConsoleEntry>(),
                _contentSequence.Records.Count));
        }
    }

    private static IReadOnlyList<IConsoleEntry> ToConsoleEntries(IReadOnlyList<ContentEntryRecord> records)
    {
        var entries = new IConsoleEntry[records.Count];
        for (int i = 0; i < records.Count; i++)
        {
            entries[i] = ((ContentEntryAdapter)records[i].Entry).ConsoleEntry;
        }

        return entries;
    }
}
