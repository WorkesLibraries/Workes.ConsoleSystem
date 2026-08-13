# Console History

`ConsoleHistory` is the shared rendered console entry stream.

Logs, future command input entries, command output, command failures, and custom entry types are intended to appear in this chronological stream. Currently, implemented log calls already write `LogEntry` values into it.

## Reading Entries

```csharp
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Entries;

var console = new ConsoleManager();

console.Log.Information("Console ready.");

foreach (var entry in console.History.Entries)
{
    if (entry is LogEntry log)
    {
        Console.WriteLine($"{log.Timestamp:u} [{log.Level}] {log.Message}");
    }
}
```

`Entries` is a live read-only view of the retained entries. Items are ordered from oldest retained entry to newest retained entry.

## Capacity

Console history is bounded. The default capacity is `200`.

When capacity is reached, adding a new entry drops the oldest retained entry.

```csharp
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;

var console = new ConsoleManager(new ConsoleManagerOptions
{
    History = new HistoryOptions
    {
        ConsoleHistoryCapacity = 2
    }
});

console.Log.Information("First");
console.Log.Information("Second");
console.Log.Information("Third");

Console.WriteLine(console.History.Entries.Count); // 2
```

After the third log, the retained messages are `Second` and `Third`.

## Clearing History

Use `Clear()` to remove all retained console entries.

```csharp
console.History.Clear();
```

## Adding Entries

`ConsoleHistory` does not currently expose public direct entry addition. User-visible package systems write to it, such as `ConsoleLog`.

This keeps the shared rendered stream controlled while the command system is still being built.

## Related Guides

- [ConsoleManager](CONSOLE_MANAGER.md)
- [Configuration](CONFIGURATION.md)
- [Command History](COMMAND_HISTORY.md)
