# Command validation

Parsing creates a `BoundCommand`; validation checks whether its explicitly supplied binding values and final resolved state satisfy the command's rules. Call `ConsoleManager.ValidateCommand(boundCommand)` to inspect validation separately. It returns `CommandValidationResult` and does not run the handler or mutate history. Validation also runs automatically during direct command execution.

## Binding rules

Use `Range(minimum, maximum)` for an inclusive range or `AllowedValues(...)` to restrict a value to a set. These methods apply to the most recently declared positional argument or option. They run only when that particular binding supplied the value. This lets an optional positional argument and an option sharing one ID have different conversion-independent restrictions. A configured default or `default(T)` does not count as supplied input for a binding-specific rule.

```csharp
var command = new CommandBuilder("game.mode")
    .Argument<ModeState>(state => state.Mode, id: "mode", name: "mode")
        .Optional()
        .AllowedValues("creative", "survival")
    .Option<ModeState>(state => state.Mode, id: "mode", name: "mode-option")
        .AllowedValues("creative", "survival", "spectator")
    .Defaults(("mode", "creative"))
    .Execute<ModeState>((context, state) => CommandResult.Success())
    .Build();
```

For this schema, positional `spectator` fails its positional rule while `--mode-option spectator` passes the option rule. The configured default is used only when neither source supplies the shared ID.

## Resolved state constraints

Add a named predicate with a message using `Constraint<TState>`. Constraints inspect the final resolved state after explicit inputs, ID defaults, and type defaults have been applied:

```csharp
new CommandBuilder("player.teleport")
    .Argument<TeleportState>(state => state.Player, id: "player", name: "player")
    .Argument<TeleportState>(state => state.Zone, id: "destination", name: "zone").Optional()
    .Option<TeleportState>(state => state.Zone, id: "destination", name: "destination")
    .Defaults(("destination", "spawn"))
    .Constraint<TeleportState>(
        "known-zone",
        "The destination zone is not available.",
        state => state.Zone != "void")
    .Execute<TeleportState>((context, state) => CommandResult.Success())
    .Build();
```

Constraints run in declaration order after binding-specific range and allowed-value checks. A false predicate returns its configured message as a structured command failure. If a constraint throws, the exception is converted into failure data and the handler is not invoked.

For a parse failure, inspect `CommandParseResult.Failure`. For a validation failure, inspect `CommandValidationResult.Failure`. Both failures include a stable code and readable message; some also include a source identifier or input span. See [Outputs and failures](COMMAND_OUTPUTS_AND_FAILURES.md).
