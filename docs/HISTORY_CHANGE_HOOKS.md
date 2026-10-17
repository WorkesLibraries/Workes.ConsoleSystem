# History change hooks

Both histories expose synchronous `Changed` events for consumers that want to update a UI when retained history changes. Events are optional; reading `Entries` and polling continue to work without subscribing. These APIs are on current `main` and were added after the released 0.2.0 package.

## Visible console history

Subscribe through the history facade:

```csharp
console.History.Changed += (_, change) =>
{
    if (change.ChangeKind == HistoryChangeKind.Added)
    {
        foreach (IConsoleEntry entry in change.AddedEntries)
        {
            AddRow(entry);
        }

        foreach (IConsoleEntry entry in change.DroppedEntries)
        {
            RemoveRow(entry);
        }
    }
    else if (change.ChangeKind == HistoryChangeKind.Cleared)
    {
        ClearRows();
    }
};
```

`ConsoleHistoryChangedEventArgs` provides `ChangeKind`, `AddedEntries`, `DroppedEntries`, `ClearedEntries`, and `RetainedCount`. On capacity overflow, one `Added` event reports both the new entry and any evicted oldest entry. A clear reports the entries that were retained immediately before the clear. Clearing an already empty history raises no event.

The manager also exposes `ConsoleHistoryChanged` as a convenience forwarding event. Both subscription paths use the same event and payload; the event sender is the `ConsoleHistory` instance.

## Command input history

```csharp
console.CommandHistory.Changed += (_, change) =>
{
    if (change.ChangeKind == HistoryChangeKind.Added)
    {
        RefreshInputHistory(change.RetainedCount);
    }
    else if (change.ChangeKind == HistoryChangeKind.Cleared)
    {
        ClearInputHistory();
    }
};
```

`CommandHistoryChangedEventArgs` provides `ChangeKind`, `AddedInputs`, `DroppedInputs`, `ClearedInputs`, and `RetainedCount`. Capacity overflow reports the added input and any evicted oldest input together. Blank inputs, rejected consecutive duplicates, and clearing an already empty history do not raise events. The manager also provides `CommandHistoryChanged`, with the `CommandHistory` instance as sender.

## Timing and event handlers

Events are raised synchronously after the history mutation commits, so `Entries` already reflects the reported change while a handler runs. Event argument collections are read-only snapshots. During command execution, command input history is updated first. If echo is enabled, the visible input entry follows; failure or output entries follow the execution result. Each visible entry raises its own event, and successful execution writes declared success outputs before handler-returned outputs.

Subscriber exceptions follow normal .NET event behavior: they propagate to the code performing the mutation, while the history mutation remains committed. Keep UI event handlers short and handle their own failures where appropriate.

These notifications are ConsoleSystem-owned. No ContentSystem records, IDs, event args, or manager types appear in the public event signatures.
