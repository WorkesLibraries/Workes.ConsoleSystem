# Configuration

`ConsoleManagerOptions` configures a `ConsoleManager` during construction.

All options have defaults. You only need to set values that differ from the normal behavior.

```csharp
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;

var console = new ConsoleManager(new ConsoleManagerOptions
{
    CommandParsing = new CommandParsingOptions
    {
        OptionValueStyle = OptionValueStyle.AnySeparated
    },
    History = new HistoryOptions
    {
        ConsoleHistoryCapacity = 200,
        CommandHistoryCapacity = 100,
        CommandHistoryDuplicatePolicy = CommandHistoryDuplicatePolicy.RejectConsecutive
    }
});
```

## Snapshot Behavior

Options are mutable setup objects. This keeps object-initializer setup simple.

`ConsoleManager` copies the supplied values during construction. Changing the original options object later does not change the manager.

```csharp
var options = new ConsoleManagerOptions
{
    History = new HistoryOptions
    {
        ConsoleHistoryCapacity = 50
    }
};

var console = new ConsoleManager(options);

options.History.ConsoleHistoryCapacity = 500;

Console.WriteLine(console.History.Capacity); // 50
```

`console.Options` exposes the resolved option values for inspection.

## Command Parsing Options

Command parsing options exist now so the public configuration shape is ready for command parsing work.

Defaults:

- `OptionValueStyle = OptionValueStyle.SpaceSeparated`
- `IsCaseSensitive = false`
- `AllowQuotedStrings = true`
- `AllowFlagsAndOptionsInAnyOrder = true`

Parsing behavior itself is not implemented yet.

## History Options

History options are active now.

Defaults:

- `ConsoleHistoryCapacity = 200`
- `CommandHistoryCapacity = 100`
- `CommandHistoryDuplicatePolicy = CommandHistoryDuplicatePolicy.RejectConsecutive`

Both capacities must be greater than zero.

## Presentation Options

Presentation options are extension slots for later output formatting work.

Defaults:

- `Theme = null`
- `Formatter = null`

Concrete theme, style, semantic output, and formatter types are planned for later stages.

## Related Guides

- [ConsoleManager](CONSOLE_MANAGER.md)
- [Console History](CONSOLE_HISTORY.md)
- [Command History](COMMAND_HISTORY.md)
