# Command History

`CommandHistory` stores submitted command input strings for UI navigation.

It is separate from `ConsoleHistory`. Adding a string through `RecordCommandInput(...)` only updates command input navigation history. Executing a command through `TryExecuteCommand(...)` or `ExecuteCommand(...)` also records command input and, when echo is enabled, writes a rendered `CommandInputEntry` to the shared console entry stream.

## Adding Input

```csharp
using Workes.ConsoleSystem.Core;

var console = new ConsoleManager();

bool added = console.RecordCommandInput("noclip");

Console.WriteLine(added); // true
```

`RecordCommandInput` returns:

- `true` when the input is retained;
- `false` when the input is skipped.

`RecordCommandInput(null)` throws `ArgumentNullException`.

Blank or whitespace-only input is ignored and returns `false`.

## Duplicate Behavior

By default, `CommandHistory` rejects consecutive duplicates.

Duplicate comparison:

- trims leading and trailing whitespace;
- compares case-insensitively;
- only checks against the newest retained input.

The original submitted string is preserved when it is stored.

```csharp
console.RecordCommandInput("noclip");      // true
console.RecordCommandInput(" NOCLIP ");    // false
console.RecordCommandInput("help");        // true
console.RecordCommandInput("noclip");      // true
```

`noclip` and `noclip --invisible` are different inputs, so both are retained.

## Allowing Consecutive Duplicates

Use `CommandHistoryDuplicatePolicy.Allow` when every non-blank submission should be retained.

```csharp
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;

var console = new ConsoleManager(new ConsoleManagerOptions
{
    History = new HistoryOptions
    {
        CommandHistoryDuplicatePolicy = CommandHistoryDuplicatePolicy.Allow
    }
});
```

## Capacity

Command history is bounded. The default capacity is `100`.

When capacity is reached, adding a new retained input drops the oldest retained input.

```csharp
var console = new ConsoleManager(new ConsoleManagerOptions
{
    History = new HistoryOptions
    {
        CommandHistoryCapacity = 2
    }
});

console.RecordCommandInput("first");
console.RecordCommandInput("second");
console.RecordCommandInput("third");

Console.WriteLine(console.CommandHistory.Entries.Count); // 2
```

After the third retained input, the entries are `second` and `third`.

## Clearing History

Use `Clear()` to remove all retained command inputs.

```csharp
console.CommandHistory.Clear();
```

## Related Guides

- [ConsoleManager](CONSOLE_MANAGER.md)
- [Configuration](CONFIGURATION.md)
- [Console History](CONSOLE_HISTORY.md)
- [Command Execution](COMMAND_EXECUTION.md)
