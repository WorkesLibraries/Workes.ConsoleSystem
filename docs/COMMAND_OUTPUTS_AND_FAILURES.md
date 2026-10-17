# Command outputs and failures

Handlers return a `CommandResult`. Use `CommandResult.Success(...)` for successful execution and `CommandResult.Failed(...)` for an expected command rejection. `CommandResult` has no public constructor; the named factories make the outcome explicit.

```csharp
return CommandResult.Success(
    CommandOutput.Inline("Saved."),
    CommandOutput.Block("First detail line\nSecond detail line"));
```

`CommandOutput.Inline(...)` and `Block(...)` accept strings or `ConsoleText`. Use `BuildInline()` or `BuildBlock()` when an output contains semantic segments. The host UI decides how inline and block output should be laid out. A command can also declare fixed success output with `SuccessOutputInline(...)` or `SuccessOutputBlock(...)` while building its definition.

On successful execution, each output is recorded as a `CommandOutputEntry`; declared outputs are recorded before handler-produced outputs. `CommandResult.Outputs` remains available to the caller as well. When a handler returns a failed result, the manager records a `CommandFailureEntry`; it does not append output entries from that failed result.

## Structured failures

`ConsoleFailure` carries a broad `Kind`, stable machine-readable `Code`, readable `Message`, and optional `Component`, `Source`, and nested `Cause`. Built-in parse, validation, and execution paths create these values automatically. Use the constants in `ConsoleFailureCodes` when matching built-in codes. Use `ConsoleFailures` for common categories or `ConsoleFailure.Create`, `Wrap`, and `FromException` when an extension needs to create or translate failure data.

```csharp
ConsoleFailure failure = ConsoleFailures.CommandExecution(
    "The account could not be saved.",
    source: "account.save");

return CommandResult.Failed(failure);
```

`CommandFailureEntry.Failure` preserves the structured data for a UI, telemetry, or application logic; `Message` and `Content` provide user-visible text. Expected failures are data, not exceptions, when using `TryExecuteCommand`. The `ExecuteCommand` convenience method throws `ConsoleOperationException` for a failed result and retains the structured failure on the exception.
