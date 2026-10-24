# Command registration

Define a command with `CommandBuilder`, call `Build()` to create an immutable `CommandDefinition`, then register it through `ConsoleManager`.

```csharp
var command = new CommandBuilder("player.teleport")
    .Description("Moves a player to a zone.")
    .Notes(new[] { "The destination must be loaded." })
    .Examples(new[] { "player.teleport Alex nether" })
    .Argument<TeleportState>(state => state.Player, id: "player", name: "player")
    .Argument<TeleportState>(state => state.Zone, id: "destination", name: "zone", description: "Destination zone.", examples: new[] { "spawn", "nether" })
        .Optional()
    .Option<TeleportState>(state => state.Zone, id: "destination", name: "destination", description: "Destination zone.", examples: new[] { "spawn", "nether" })
    .Flag<TeleportState>(state => state.Silent, id: "silent", name: "silent", aliases: new[] { "s" })
    .Option<TeleportState>(state => state.Height, id: "height", name: "height", aliases: new[] { "h" })
        .Range(0, 500)
    .Defaults(("destination", "spawn"), ("height", 0))
    .Constraint<TeleportState>(
        "known-zone",
        "The destination zone is not available.",
        state => state.Zone != "void")
    .Execute<TeleportState>((context, state) =>
        CommandResult.Success(CommandOutput.Inline($"Moved {state.Player} to {state.Zone}.")))
    .Build();

console.RegisterCommand(command);

internal sealed record TeleportState(string Player, string? Zone, bool Silent, int Height);
```

The optional positional argument and the option both use ID `"destination"` and target the same state property and type. A caller can provide the zone positionally (`player.teleport Alex nether`) or by option (`player.teleport Alex --destination nether`). Supplying both is a parse failure.

## IDs, names, and state

Every argument, flag, and option requires an ID and a canonical input name. An ID identifies the logical state value; a name is what the user types or supplies positionally. IDs use ordinal, case-sensitive comparison. IDs can differ from names and property names, and only IDs can be used with `Defaults(...)` or to read `BoundCommand.Values`.

`Argument<TState>`, `Flag<TState>`, and `Option<TState>` bind public instance properties on one reference-type state. A public constructor must have parameter names matching the bound properties, ignoring case unless parsing is case-sensitive. Constructor parameter order does not need to match declaration order.

- `Argument<TState>(propertySelector, id, name)` adds a required positional value. Positional values bind in declaration order.
- `.Optional()` marks the immediately preceding positional argument optional. At most one optional positional argument is allowed, and it must be last.
- `Flag<TState>(propertySelector, id, name, aliases...)` adds a presence-based boolean. A missing flag resolves to `false`; a present flag resolves to `true`.
- `Option<TState>(propertySelector, id, name, aliases...)` adds a named value.
- An ID can be shared only by one optional positional argument and one option. Both must target the same property and type. All other duplicate-ID combinations fail during `Build()`.

Flags and options can use aliases. Argument names and all flag/option names and aliases must be unique within a command.

## Defaults and resolved values

`Defaults((id, value), ...)` sets any number of logical value defaults in one call. A value resolves from explicit input first, then its configured default, then `default(T)`. This applies to optional arguments and options; flags are always `false` when absent and cannot have defaults. Defaults for required positional arguments are invalid because those arguments must be supplied. Unknown IDs, repeated defaults, and values that cannot convert to the bound type fail during `Build()`.

`BoundCommand.Values` is a read-only dictionary of every resolved logical value keyed by ID. `Arguments`, `Options`, and `Flags` are read-only source views keyed by canonical input name; they contain only bindings explicitly supplied by the caller. A default or a type default does not appear in those source views.

## Binding metadata

`Range(minimum, maximum)` and `AllowedValues(...)` apply to the most recently declared positional argument or option. They validate only input supplied through that binding. A shared ID may therefore have different rules for its positional and option forms. State constraints added with `Constraint<TState>(...)` run against the final resolved state.

`SuggestAllowedValues()` opts the most recent argument or option into autocomplete suggestions from its allowed-value set. It remains opt-in. `ValueCandidates(...)` attaches a synchronous suggestion provider to the most recent argument or option and takes precedence over allowed-value suggestions. See [Command autocomplete](COMMAND_AUTOCOMPLETE.md).

`Constraint<TState>` adds a state-level predicate. `RequirePermission(...)`, `RequireAnyPermission(...)`, and `AvailableWhen(...)` add optional access rules. `Description`, `EchoInput`/`DoNotEchoInput`, and `SuccessOutputInline`/`SuccessOutputBlock` add command metadata.

## Help metadata and aliases

`Description(...)` sets the short command description. `Notes(...)` accepts optional explanatory lines, and `Examples(...)` accepts complete command invocations. `Argument(...)` and `Option(...)` accept optional `description` and `examples` arguments; their examples are sample values for that binding. `Flag(...)` accepts an optional description. Omitted strings are empty and omitted collections are empty read-only snapshots. The opt-in Detailed help formatter uses metadata only when present.

Use `.Syntax("...")` for authored usage text, or `.GenerateSyntaxWith()` for the canonical parser-aware syntax. Pass a custom `CommandSyntaxFormatter` to generate another style. A command can leave syntax absent; manual and generated syntax cannot both be set.

Aliases now use an optional collection instead of variadic strings. For example, write `.Option<State>(..., aliases: new[] { "d", "delay" })`. This is a pre-1.0 source change from `.Option<State>(..., "d", "delay")`.

`Execute(context => ...)` accepts a simple handler without typed state; `Execute<TState>((context, state) => ...)` receives the resolved state. The handler returns `CommandResult.Success(...)` or `CommandResult.Failed(...)`.

The manager owns registration. Registering a duplicate command path or a schema with conflicting flag/option names or aliases throws an argument or invalid-operation exception. `RegisterCommands` validates a batch before adding it, so invalid batches are not partially registered. There is no unregister or replacement operation.

See [Command parsing](COMMAND_PARSING.md), [Command validation](COMMAND_VALIDATION.md), and [Command autocomplete](COMMAND_AUTOCOMPLETE.md) for input, validation, and suggestion behavior.

For an opt-in command that formats an overview and supports configurable command search/help, see [Configurable help command](HELP_COMMAND.md).
