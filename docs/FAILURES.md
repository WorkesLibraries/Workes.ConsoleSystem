# Failure Handling

`Workes.ConsoleSystem` treats expected console-system rejection as structured data. This lets UI, gameplay code, tests, telemetry, and localization branch on stable values instead of parsing human-readable messages.

Use this guide when you need to:

- handle invalid command input in UI code.
- branch on stable failure categories or codes.
- decide whether an error should be a returned failure or an exception.
- prepare for future command execution wrappers.

## Expected Failure Versus Programmer Misuse

The package separates two kinds of failure:

| Situation | API behavior | Examples |
|---|---|---|
| Expected console-system rejection | APIs return `ConsoleFailure` through structured results; future expected-success wrappers throw project exceptions containing that same failure | Unknown command, missing argument, invalid option value, failed command constraint |
| Programmer or setup misuse | Standard .NET exceptions are thrown directly | Null arguments, invalid option configuration, malformed command definitions during setup |

This distinction is intentional. Expected rejection is part of normal console usage. Programmer misuse usually means calling code or setup code is wrong and should be fixed.

## The Basic Pattern

Use structured results when rejection is normal control flow.

```csharp
CommandParseResult result = console.ParseCommand(input);

if (!result.Success)
{
    if (result.Failure?.Code == ConsoleFailureCodes.CommandUnknown)
    {
        ShowUnknownCommand(result.Failure.Message);
    }
    else
    {
        ShowParseFailure(result.Failure?.Message);
    }
}
```

Use project-owned exceptions only for expected-success wrappers. The exception types exist before those wrappers, so future APIs can reuse the same failure object instead of inventing another error model.

```csharp
try
{
    // Future expected-success command execution wrapper.
}
catch (ConsoleOperationException ex)
{
    ConsoleFailure failure = ex.Failure;
}
```

## Project Exceptions

`ConsoleSystemException` is the base exception for expected-success console-system wrappers that fail because the console domain rejected the operation.

It derives from `InvalidOperationException`, so broad invalid-operation catches can still catch it. Prefer catching the project-owned type when you need structured failure details.

`ConsoleOperationException` is the general operation wrapper exception. More specific exception types can be added later only when they carry real meaning.

Standard `ArgumentException`, `ArgumentNullException`, `ArgumentOutOfRangeException`, and ordinary `InvalidOperationException` are still used for programmer misuse and invalid object state.

## Design Of `ConsoleFailure`

`ConsoleFailure` is immutable and contains:

| Member | Use |
|---|---|
| `Kind` | Broad category, suitable for coarse branching. |
| `Code` | Stable machine-readable identifier, suitable for precise branching, localization keys, telemetry, and tests. |
| `Message` | Human-readable description for logs, tools, or simple UI display. Messages may evolve over time. |
| `Component` | Optional subsystem or component that reported or wrapped the failure. |
| `Source` | Optional stable source identifier, such as a command path, option name, or constraint id. |
| `Cause` | Optional nested failure when a higher-level subsystem wraps a lower-level rejection. |

Branch on `Kind` or `Code`. Do not parse `Message`.

## Failure Categories

`ConsoleFailureKind` describes the broad reason category:

| Kind | Typical meaning |
|---|---|
| `Unknown` | The package could not classify the failure more precisely. |
| `Validation` | General validation rejected the request. |
| `Configuration` | Setup configuration rejected the request. |
| `CommandDefinition` | Command schema or definition validation rejected the request. |
| `CommandRegistration` | Registry-level command registration rejected the request. |
| `CommandParsing` | Command text shape or command member resolution rejected the request. |
| `CommandBinding` | Parsed values could not be converted or bound to command state. |
| `CommandConstraint` | A command constraint rejected a fully bound command. |
| `CommandExecution` | Command execution rejected or failed. |
| `Extension` | Extension-provided code rejected or failed inside an expected path. |

## Built-In Failure Codes

Built-in codes are constants on `ConsoleFailureCodes` and use the reserved package prefix `workes.console.`.

Common groups include:

| Group | Example codes |
|---|---|
| General | `Unknown`, `ValidationRejected`, `ConfigurationRejected` |
| Definitions and registration | `CommandDefinitionInvalid`, `CommandRegistrationRejected` |
| Parsing | `CommandInputEmpty`, `CommandUnknown`, `CommandArgumentMissing`, `CommandArgumentUnexpected`, `CommandMemberUnknown`, `CommandOptionValueMissing`, `CommandMemberDuplicate`, `CommandOptionValueSyntaxInvalid`, `CommandQuoteUnclosed` |
| Binding | `CommandValueInvalid`, `CommandStateBindingFailed` |
| Future command workflow | `CommandConstraintRejected`, `CommandExecutionRejected`, `ExtensionRejected` |

Package-owned codes are reserved. Extension authors should use their own namespaced codes, such as `com.example.console.command.rejected`.

## Related Guides

- [Command Parsing](COMMAND_PARSING.md)
- [ConsoleManager](CONSOLE_MANAGER.md)
- [Command Registration](COMMAND_REGISTRATION.md)
