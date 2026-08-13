# Command Registration

Command registration defines command schemas before command parsing and execution are implemented.

In the current package, commands can be created, validated, and registered. Registered definitions can be inspected through `console.Commands.Definitions`. Command input parsing and command execution are planned later stages.

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

`Execute(...)` stores the command handler delegate, but the handler is not invoked until command execution is implemented later.

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
    .Flag<RestartCommandState>(x => x.IgnorePlayers, "--ignore-players", "-i")
    .Option<RestartCommandState>(x => x.DelaySeconds, "--delay", "-d")
        .Default(10)
        .Range(0, 60)
        .AllowedValues(0, 10, 30, 60)
    .Constraint("delay-ignore-players", "Delay cannot be combined with ignore players.")
    .Execute<RestartCommandState>((ctx, state) => new CommandResult())
    .Build();

console.RegisterCommand(restart);
```

Arguments, flags, options, and constraints are schema metadata in this stage. They are not parsed or evaluated yet.

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
