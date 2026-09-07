# Command Registration

Command registration defines command schemas for parsing, validation, execution, and autocomplete.

Commands can be created, registered, inspected, executed, parsed, validated, and completed through autocomplete.

## Simple Commands

Use `CommandBuilder` to create a command definition, then register it through `ConsoleManager`.

```csharp
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Core;

var console = new ConsoleManager();

CommandDefinition noclip = new CommandBuilder("noclip")
    .Description("Toggle noclip.")
    .Execute(ctx => new CommandResult())
    .Build();

console.RegisterCommand(noclip);
```

`Execute(...)` stores the command handler delegate. The handler is invoked by `ConsoleManager.TryExecuteCommand(...)` or `ConsoleManager.ExecuteCommand(...)`, not by parsing or validation.

## Typed Command Schemas

Use a reference-type state model when a command has arguments, flags, or options.

```csharp
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Core;

public sealed record RestartCommandState(
    string Reason,
    bool IgnorePlayers,
    int DelaySeconds);

var console = new ConsoleManager();

CommandDefinition restart = new CommandBuilder("server.restart")
    .Description("Restart the server.")
    .Argument<RestartCommandState>(x => x.Reason, "reason")
    .Flag<RestartCommandState>(x => x.IgnorePlayers, "ignore-players", "i")
    .Option<RestartCommandState>(x => x.DelaySeconds, "delay", "d")
        .Default(10)
        .Range(0, 60)
        .AllowedValues(0, 10, 30, 60)
    .Constraint<RestartCommandState>(
        "delay-ignore-players",
        "Delay cannot be combined with ignore players.",
        state => !state.IgnorePlayers || state.DelaySeconds == 0)
    .Execute<RestartCommandState>((ctx, state) => new CommandResult())
    .Build();

console.RegisterCommand(restart);
```

Arguments, flags, and options are parsed and bound by `ConsoleManager.ParseCommand(...)`. Option ranges, allowed values, and typed constraints are enforced by `ConsoleManager.ValidateCommand(...)`.

Normal player-authored command submissions should use `ConsoleManager.TryExecuteCommand(...)`, which parses and validates automatically before invoking the handler. Use `ConsoleManager.ExecuteCommand(...)` when failure should throw.

## Value Autocomplete

Positional arguments and options can provide autocomplete candidates:

```csharp
CommandDefinition give = new CommandBuilder("player.give")
    .Argument<GiveState>(x => x.Player, "player")
        .ValueCandidates(ctx => new[] { "@me", "Anthony5172" })
    .Argument<GiveState>(x => x.Item, "item")
        .ValueCandidates(ctx => new[] { "wood", "stone", "gold" })
    .Execute<GiveState>((ctx, state) => new CommandResult())
    .Build();
```

Path, flag, and option name candidates come from command schema. Argument and option value candidates are opt-in through `ValueCandidates(...)`.

## Command Echo And Success Output

Command definitions can store command-specific echo and success-output metadata for execution.

By default, command execution echoes submitted command input according to manager execution options. A command can override that behavior:

```csharp
CommandDefinition command = new CommandBuilder("noclip")
    .EchoInput("CommandInput")
    .SuccessOutputInline("<style=Success>Noclip enabled.</style>")
    .Execute(ctx => new CommandResult())
    .Build();
```

Use `DoNotEchoInput()` when a command should not echo submitted input.

Success output helpers use formatting-aware strings. With formatting disabled, strings stay literal. With formatting enabled, known markup tags are parsed by the manager when output is resolved or formatted. Dynamic output can still be returned from the command handler through `CommandResult`.

## Flag And Option Names

Flag and option names are defined without their command-line prefix.

```csharp
.Flag<RestartCommandState>(x => x.IgnorePlayers, "ignore-players", "i")
.Option<RestartCommandState>(x => x.DelaySeconds, "delay", "d")
```

The configured parser prefix is applied when command input is parsed. The default prefix is `--`, so the logical option name `delay` is typed as `--delay` by default.

Use `CommandParsingOptions.FlagAndOptionPrefix` to configure another prefix, such as `-`.

```csharp
var console = new ConsoleManager(new ConsoleManagerOptions
{
    CommandParsing = new CommandParsingOptions
    {
        FlagAndOptionPrefix = "-"
    }
});
```

With that configuration, the logical option name `delay` will be typed as `-delay`.

## Path Rules

Command paths are strings such as `noclip`, `server.restart`, or `player.give`.

Paths must:

- be non-null and non-blank;
- contain no whitespace;
- not start or end with `.`;
- not contain empty dot segments.

## Duplicate Paths

Duplicate path validation happens during manager registration.

By default, path comparison is case-insensitive because `CommandParsingOptions.IsCaseSensitive` defaults to `false`.

```csharp
console.RegisterCommand(new CommandBuilder("noclip")
    .Execute(ctx => new CommandResult())
    .Build());

console.RegisterCommand(new CommandBuilder("NOCLIP")
    .Execute(ctx => new CommandResult())
    .Build()); // throws by default
```

Set `IsCaseSensitive = true` when commands with paths that differ only by case should be allowed.

## Batch Registration

Use `RegisterCommands(...)` to register multiple commands atomically.

```csharp
console.RegisterCommands(new[]
{
    new CommandBuilder("noclip")
        .Execute(ctx => new CommandResult())
        .Build(),
    new CommandBuilder("help")
        .Execute(ctx => new CommandResult())
        .Build()
});
```

If any command in the batch is invalid for the registry, no commands from that batch are registered.

## Inspecting Registered Commands

```csharp
foreach (CommandDefinition command in console.Commands.Definitions)
{
    Console.WriteLine(command.Path);
}
```

`console.Commands` is an inspectable registry. Registration is owned by `ConsoleManager`.

`CommandDefinition` exposes public metadata for registered commands:

| Metadata | Use |
|---|---|
| `Path`, `Description`, `StateType` | Identify and describe the command. |
| `Arguments` | Required positional `CommandArgumentDefinition` values. |
| `Flags` | Boolean `CommandFlagDefinition` values and aliases. |
| `Options` | `CommandOptionDefinition` values, aliases, defaults, ranges, and allowed values. |
| `Constraints` | `CommandConstraintDefinition` names and messages. |
| `EchoInput` | `CommandEchoInputDefinition` override metadata. |
| `SuccessOutputs` | Static `CommandSuccessOutputDefinition` metadata appended during execution. |

`CommandArgumentDefinition`, `CommandFlagDefinition`, and `CommandOptionDefinition` all inherit from `CommandMemberDefinition`. Command member definitions expose `PropertyName`, `ValueType`, `Name`, `Aliases`, `Description`, and whether value autocomplete candidates are available.

## Related Guides

- [ConsoleManager](CONSOLE_MANAGER.md)
- [Configuration](CONFIGURATION.md)
- [Command History](COMMAND_HISTORY.md)
- [Command Execution](COMMAND_EXECUTION.md)
- [Command Parsing](COMMAND_PARSING.md)
- [Command Validation](COMMAND_VALIDATION.md)
- [Command Autocomplete](COMMAND_AUTOCOMPLETE.md)
- [Command Results And Output](COMMAND_OUTPUT.md)
- [Formatting](FORMATTING.md)
