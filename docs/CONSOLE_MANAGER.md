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
- `Commands`, the inspectable command registry.

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

## Manager Responsibilities

The manager owns the normal package workflow:

- log messages can be written through manager methods such as `console.LogInformation(...)`;
- log entries are stored in `console.History`;
- submitted command strings can be recorded through `console.RecordCommandInput(...)`;
- histories are bounded and drop the oldest retained item when full.
- immutable command definitions can be registered through `console.RegisterCommand(...)`.
- registered commands can be executed through `console.TryExecuteCommand(...)` or `console.ExecuteCommand(...)`.
- registered command input can be parsed through `console.ParseCommand(...)`.
- parsed commands can be validated through `console.ValidateCommand(...)`.
- registered command schemas can provide autocomplete through `console.GetAutocomplete(...)`.

`ParseCommand(...)` and `ValidateCommand(...)` are useful for inspection and preflight UI feedback. Normal player-authored input should use `TryExecuteCommand(...)`; expected-success code can use `ExecuteCommand(...)`.

## Related Guides

- [Configuration](CONFIGURATION.md)
- [Core Concepts](CONCEPTS.md)
- [Console History](CONSOLE_HISTORY.md)
- [Console UI Integration](CONSOLE_UI_INTEGRATION.md)
- [Command History](COMMAND_HISTORY.md)
- [Command Registration](COMMAND_REGISTRATION.md)
- [Command Execution](COMMAND_EXECUTION.md)
- [Command Parsing](COMMAND_PARSING.md)
- [Command Validation](COMMAND_VALIDATION.md)
- [Command Autocomplete](COMMAND_AUTOCOMPLETE.md)
