# Core Concepts

`Workes.ConsoleSystem` gives a game or tool one engine-neutral console model. Your application owns the UI. The package owns command registration, command execution, logging, retained history, failures, autocomplete, and semantic text.

## Mental Model

| Component | Responsibility |
|---|---|
| `ConsoleManager` | The root object for one console instance. |
| `ConsoleHistory` | One chronological stream of visible console entries. |
| `CommandHistory` | Submitted command strings for input navigation. |
| `CommandBuilder` | Builds immutable command definitions. |
| `CommandSystem` | Read-only registry of registered command definitions. |
| `CommandResult` | The success/failure and output returned by command handlers. |
| `ConsoleFailure` | Stable structured failure data for expected rejection. |
| `ConsoleText` | Engine-neutral visible text with optional semantic styling. |

## Normal Runtime Flow

Create one manager, register commands during startup, then submit player-authored command text through `TryExecuteCommand(...)`.

```csharp
var console = new ConsoleManager();

console.RegisterCommand(new CommandBuilder("noclip")
    .SuccessOutputInline("Noclip enabled.", defaultStyle: "Success")
    .Execute(ctx => new CommandResult())
    .Build());

if (!console.TryExecuteCommand("noclip", out CommandResult result))
{
    Console.WriteLine(result.Failure!.Message);
}
```

Use `ExecuteCommand(...)` when success is expected and failure should throw `ConsoleOperationException`.

## Console Entries

`ConsoleHistory` stores `IConsoleEntry` values. The concrete entry type tells the UI what kind of entry it is:

- `LogEntry` for log messages.
- `CommandInputEntry` for echoed submitted commands.
- `CommandOutputEntry` for command output.
- `CommandFailureEntry` for framework-created command failures.

Custom entry types can implement `IConsoleEntry` for game-specific console events.

## Commands

Commands use one schema model. A command path is followed by required positional arguments, then optional flags and options:

```text
<path> <required positional arguments...> [flags/options...]
```

Simple commands do not need a state type. Commands with arguments, flags, or options should use small reference-type state records.

## Results And Failures

Command handlers return `CommandResult`. Successful results can contain zero, one, or many outputs. Expected command-level rejection uses `CommandResult.Failed(...)` with `ConsoleFailure`.

The package follows the same style as other Workes packages:

- `Try...` APIs return `false` and expose structured failure data.
- expected-success APIs throw package exceptions carrying the same failure.
- programmer/setup misuse throws standard .NET exceptions.

## UI Boundary

The package does not draw a console UI. A UI reads `console.History.Entries`, renders each concrete entry type, calls `GetAutocomplete(...)` while the user types, and submits text through `TryExecuteCommand(...)`.

## Related Guides

- [Quick Start](QUICK_START.md)
- [Console UI Integration](CONSOLE_UI_INTEGRATION.md)
- [Command Registration](COMMAND_REGISTRATION.md)
- [Command Execution](COMMAND_EXECUTION.md)
- [Failure Handling](FAILURES.md)
