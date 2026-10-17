# Command execution

For normal submission, call the manager directly. `TryExecuteCommand` returns a Boolean and structured `CommandResult`; `ExecuteCommand` returns the successful result or throws `ConsoleOperationException` when the command is rejected or fails.

```csharp
if (console.TryExecuteCommand("greet Mira", out CommandResult result))
{
    foreach (CommandOutput output in result.Outputs)
    {
        System.Console.WriteLine(output.PlainText);
    }
}
else
{
    System.Console.Error.WriteLine(result.Failure?.Message);
}
```

String input execution records the submitted command string in `CommandHistory`, parses it, validates the bound state, invokes the synchronous handler, and records output or failure in visible history. Parsing failures also become visible failure entries. Parsing and validation can be called separately when a host needs to inspect input before submission.

If a host has already parsed input, the `BoundCommand` overloads of `TryExecuteCommand` and `ExecuteCommand` accept that result directly. They skip reparsing, but still record the submitted input, run validation, invoke the handler, and record the outcome.

`CommandContext` gives the handler the original input, matched definition, bound command, and execution timestamp. A handler returns a `CommandResult`; it does not write to a hidden output stream.

Input echo is enabled by default through `CommandExecutionOptions.EchoInput`. A command can override that choice with `EchoInput()` or `DoNotEchoInput()` on its builder. The input remains in `CommandHistory` even when visible echo is disabled. A successful execution appends declared success outputs first, followed by outputs returned by the handler.

Handler exceptions are converted into structured command failures. `TryExecuteCommand` returns those failures rather than throwing. `ExecuteCommand` wraps a command failure in `ConsoleOperationException`; when a handler threw, the original exception is also available as `InnerException` and is represented in the structured failure cause chain. Standard argument errors such as a null input still throw directly.

Execution is synchronous in this release. See [Command outputs and failures](COMMAND_OUTPUTS_AND_FAILURES.md) for result construction and entry behavior.
