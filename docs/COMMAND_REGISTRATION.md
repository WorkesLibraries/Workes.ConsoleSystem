# Command Registration

Command registration defines command schemas for parsing and later execution.

In the current package, commands can be created, validated, registered, inspected, and parsed. Command execution is not implemented yet.

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

`Execute(...)` stores the command handler delegate, but the handler is not invoked by parsing. Handler invocation is planned for command execution.

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
    .Constraint("delay-ignore-players", "Delay cannot be combined with ignore players.")
    .Execute<RestartCommandState>((ctx, state) => new CommandResult())
    .Build();

console.RegisterCommand(restart);
```

Arguments, flags, and options are parsed and bound by `ConsoleManager.ParseCommand(...)`. Constraints remain metadata until constraint evaluation is implemented.

## Command Echo And Success Output

Command definitions can store command-specific echo and success-output metadata for later execution.

By default, command execution is expected to echo submitted command input according to manager execution options. A command can override that behavior:

```csharp
CommandDefinition command = new CommandBuilder("noclip")
    .EchoInput("CommandInput")
    .SuccessOutputInlineMarkup("<style=Success>Noclip enabled.</style>")
    .Execute(ctx => new CommandResult())
    .Build();
```

Use `DoNotEchoInput()` when a command should not echo submitted input.

Plain success output helpers treat strings literally. Markup success output helpers include `Markup` in the method name. Dynamic output can still be returned from the command handler through `CommandResult`.

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

## Related Guides

- [ConsoleManager](CONSOLE_MANAGER.md)
- [Configuration](CONFIGURATION.md)
- [Command History](COMMAND_HISTORY.md)
- [Command Parsing](COMMAND_PARSING.md)
- [Command Results And Output](COMMAND_OUTPUT.md)
- [Formatting](FORMATTING.md)
