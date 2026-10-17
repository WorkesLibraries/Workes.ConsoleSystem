# Command validation

Parsing creates a `BoundCommand`; validation checks whether that bound state satisfies the command's rules. Call `ConsoleManager.ValidateCommand(boundCommand)` to inspect validation separately. It returns `CommandValidationResult` and does not run the handler or mutate history.

Validation runs automatically as part of direct command execution.

## Built-in option rules

Use `CommandBuilder.Range(minimum, maximum)` to set an inclusive range for an option, or `AllowedValues(...)` to restrict it to a set of values. These methods apply to the most recently declared option. Omitted options receive their configured default, or the type's default value when no default is declared, before validation.

## Custom state constraints

Add a named predicate with a message using `Constraint<TState>`:

```csharp
new CommandBuilder("player.teleport")
    .Argument<TeleportState>(state => state.Zone, "zone")
    .Constraint<TeleportState>(
        "known-zone",
        "The destination zone is not available.",
        state => state.Zone != "void")
    .Execute<TeleportState>((context, state) => CommandResult.Success())
    .Build();
```

Constraints run in declaration order after option range and allowed-value checks. A false predicate returns its configured message as a structured command failure. If a constraint throws, the exception is converted into failure data and the handler is not invoked.

For a parse failure, inspect `CommandParseResult.Failure`. For a validation failure, inspect `CommandValidationResult.Failure`. Both failures include a stable code and a readable message; some failures also include a source identifier or input span. See [Outputs and failures](COMMAND_OUTPUTS_AND_FAILURES.md).
