# Command Validation

Command validation checks a successfully parsed command before execution.

Parsing answers "does this input match a command schema and bind into typed state?" Validation answers "is the bound command allowed by its command rules?"

Most code should not need to call validation manually once command execution is available. Execution will validate automatically before invoking handlers. Use `ValidateCommand(...)` when a UI or tool wants preflight feedback without running the command.

## Normal Usage

Most game code should not call `ValidateCommand(...)` directly. Once command execution is implemented, submitted command strings should go through execution:

```csharp
CommandResult result = console.ExecuteCommand("server.restart maintenance --ignore-players --delay 5");
```

Execution will parse, validate, and then run the handler. If validation fails, execution will return a failed `CommandResult` containing the validation `ConsoleFailure`.

## Preflight Validation

Manual validation is for preflight scenarios where you want to inspect a command without running it. For example, a UI might parse and validate the current input to show a warning before the player presses enter.

```csharp
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Core;

var console = new ConsoleManager();

console.RegisterCommand(new CommandBuilder("server.restart")
    .Argument<RestartCommandState>(x => x.Reason, "reason")
    .Flag<RestartCommandState>(x => x.IgnorePlayers, "ignore-players")
    .Option<RestartCommandState>(x => x.DelaySeconds, "delay")
        .Default(0)
        .Range(0, 60)
    .Constraint<RestartCommandState>(
        "delay-ignore-players",
        "Delay cannot be combined with ignore players.",
        state => !state.IgnorePlayers || state.DelaySeconds == 0)
    .Execute<RestartCommandState>((ctx, state) => new CommandResult())
    .Build());

CommandParseResult parse = console.ParseCommand("server.restart maintenance --ignore-players --delay 5");

if (parse.Success)
{
    CommandValidationResult validation = console.ValidateCommand(parse.Command!);

    if (!validation.Success)
    {
        Console.WriteLine(validation.Failure!.Message);
    }
}
```

`ValidateCommand(null)` throws `ArgumentNullException` because null input is a caller error.

## Constraints

Constraints are typed predicates over the command state.

```csharp
.Constraint<RestartCommandState>(
    "delay-ignore-players",
    "Delay cannot be combined with ignore players.",
    state => !state.IgnorePlayers || state.DelaySeconds == 0)
```

The predicate returns `true` when the state is valid. If it returns `false`, validation returns a `ConsoleFailure` with:

- `Kind = ConsoleFailureKind.CommandConstraint`
- `Code = ConsoleFailureCodes.CommandConstraintRejected`
- `Message` from the constraint
- `Source` set to the constraint name

Constraints run in registration order and validation returns the first failure.

If a constraint predicate throws, validation converts that exception into a structured constraint failure. The lower-level exception message is available through `Failure.Cause`.

## Option Rules

Option `.Range(...)` and `.AllowedValues(...)` metadata is enforced during validation.

```csharp
.Option<RestartCommandState>(x => x.DelaySeconds, "delay")
    .Range(0, 60)
    .AllowedValues(0, 10, 30, 60)
```

Ranges are inclusive. If both range and allowed values are configured, both rules must pass.

Invalid option metadata is rejected when the command is built. Runtime validation failures return `CommandValidationResult` with `ConsoleFailure` instead of throwing.

## Side Effects

Validation is side-effect-free. It does not:

- execute command handlers;
- write command input history;
- write shared console history;
- create command output entries.

Those behaviors belong to command execution.

## Related Guides

- [Command Registration](COMMAND_REGISTRATION.md)
- [Command Parsing](COMMAND_PARSING.md)
- [Failure Handling](FAILURES.md)
- [ConsoleManager](CONSOLE_MANAGER.md)
