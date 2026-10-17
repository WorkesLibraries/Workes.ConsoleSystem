# Command registration

Define a command with `CommandBuilder`, call `Build()` to create an immutable `CommandDefinition`, then register it through `ConsoleManager`.

```csharp
var command = new CommandBuilder("player.teleport")
    .Description("Moves a player to a zone.")
    .Argument<TeleportState>(state => state.Player, "player")
    .Argument<TeleportState>(state => state.Zone, "zone")
    .Flag<TeleportState>(state => state.Silent, "silent", "s")
    .Option<TeleportState>(state => state.Height, "height", "h")
        .Default(0)
        .Range(0, 500)
    .Constraint<TeleportState>(
        "known-zone",
        "The destination zone is not available.",
        state => state.Zone != "void")
    .Execute<TeleportState>((context, state) =>
        CommandResult.Success(CommandOutput.Inline($"Moved {state.Player} to {state.Zone}.")))
    .Build();

console.RegisterCommand(command);

internal sealed record TeleportState(string Player, string Zone, bool Silent, int Height);
```

## Command schema

- The command path is a non-empty token, optionally divided into dot-separated segments such as `player.teleport`. It cannot contain whitespace or empty dot segments.
- `Argument<TState>` adds a required positional value. Values bind in declaration order.
- `Flag<TState>` adds a boolean member. Its value is `false` when omitted and `true` when present. Aliases are optional.
- `Option<TState>` adds a named value. Aliases are optional. `Default`, `Range`, and `AllowedValues` apply to the option most recently added, so place them immediately after that option definition.
- `Constraint<TState>` adds a state-level predicate that runs during validation.
- `Description`, `EchoInput`/`DoNotEchoInput`, and `SuccessOutputInline`/`SuccessOutputBlock` add command metadata.
- `Execute` accepts a simple handler with only `CommandContext`; `Execute<TState>` accepts the bound typed state as well.

Typed state must be a reference type. Binding uses public state properties and a public constructor whose parameter names match the bound property names, ignoring case unless parsing is configured as case-sensitive. Immutable record types fit this model. The handler returns `CommandResult.Success(...)` or `CommandResult.Failed(...)`.

The manager owns registration. Registering a duplicate command path or a schema with conflicting flag/option names or aliases throws an argument/invalid-operation exception. `RegisterCommands` validates a batch before adding it, so invalid batches are not partially registered. The current API has no unregister or replacement operation.

For a command without bound state, omit typed members and use `Execute(context => ...)`. Registered definitions can be inspected through `console.Commands.Definitions`; callers still register them through the manager.

See [Command parsing](COMMAND_PARSING.md) for input syntax and [Command validation](COMMAND_VALIDATION.md) for range and constraint behavior.
