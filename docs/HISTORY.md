# Console and command histories

The manager exposes two independent histories because they represent different things:

- `console.History` is the visible chronological stream of `IConsoleEntry` values.
- `console.CommandHistory` stores submitted command strings for input navigation.

Neither history needs a ContentSystem object in application code.

## Visible console history

`ConsoleManager.History` exposes `Capacity`, `Entries`, and `Clear()`. `Entries` returns a detached, oldest-first snapshot. Mutating or retaining that snapshot does not mutate the manager's history. When the configured capacity is reached, adding a new entry drops the oldest entry. Logs, echoed input, command outputs, failures, and custom entries all share this capacity.

```csharp
foreach (IConsoleEntry entry in console.History.Entries)
{
    System.Console.WriteLine(entry.Timestamp);
}

console.History.Clear();
```

Use `ConsoleManager.RecordEntry` to add custom or constructed ConsoleSystem entries. Normal logging and command execution record their own entries.

## Command input history

`ConsoleManager.CommandHistory` exposes `Capacity`, oldest-first detached `Entries`, and `Clear()`. Manager command execution adds submitted input automatically. `ConsoleManager.RecordCommandInput(input)` is available when a host input workflow records a submitted string separately; it returns `false` for blank input or a suppressed duplicate.

By default, consecutive inputs that match after trimming, using ordinal case-insensitive comparison, are rejected. Set `History.CommandHistoryDuplicatePolicy` to `Allow` to retain them. When full, command history drops its oldest string. This history is not echoed into the visible transcript by itself; visible input entries are controlled by command execution echo settings.

Both histories have independently configurable capacities. See [Manager and configuration](MANAGER_AND_CONFIGURATION.md) and [Command execution](COMMAND_EXECUTION.md).

Both histories also expose optional change notifications. See [History change hooks](HISTORY_CHANGE_HOOKS.md) for payloads, event ordering, and manager-level forwarding.
