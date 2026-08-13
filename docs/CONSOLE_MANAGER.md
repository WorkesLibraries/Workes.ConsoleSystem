# ConsoleManager

`ConsoleManager` is the root object for one console system instance.

Most applications should create one `ConsoleManager` during startup and keep it for the lifetime of the host console. The package does not enforce singleton usage, so tests or unusual hosts can create more than one manager when needed.

## Basic Setup

```csharp
using Workes.ConsoleSystem.Core;

var console = new ConsoleManager();
```

The default constructor creates:

- `History`, the shared rendered console entry stream.
- `CommandHistory`, the submitted command input history used by UI navigation.
- `Log`, the logging facade.
- `Commands`, the future command registration and execution surface.

## Configured Setup

Use `ConsoleManagerOptions` when you want to override defaults.

```csharp
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;

var console = new ConsoleManager(new ConsoleManagerOptions
{
    History = new HistoryOptions
    {
        ConsoleHistoryCapacity = 500,
        CommandHistoryCapacity = 150
    }
});
```

Options are setup values. `ConsoleManager` snapshots the supplied values during construction, so later changes to the original options object do not change the manager.

## Current Behavior

The currently implemented manager behavior is:

- log messages can be written through `console.Log`;
- log entries are stored in `console.History`;
- submitted command strings can be stored in `console.CommandHistory`;
- histories are bounded and drop the oldest retained item when full.

Command registration, parsing, execution, and autocomplete are planned but not implemented yet.

## Related Guides

- [Configuration](CONFIGURATION.md)
- [Console History](CONSOLE_HISTORY.md)
- [Command History](COMMAND_HISTORY.md)
