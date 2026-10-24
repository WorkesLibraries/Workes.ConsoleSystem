# Command execution

For normal submission, call the manager directly. The synchronous `TryExecuteCommand` returns a Boolean and structured `CommandResult`; `ExecuteCommand` returns the successful result or throws `ConsoleOperationException` when the command is rejected or fails. Async alternatives use `Task<CommandResult>` for the try form and throw from the execute form.

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

String input execution records the submitted command string in `CommandHistory`, parses it, validates the bound state, invokes the handler, and records output or failure in visible history. Parsing failures also become visible failure entries. Parsing and validation can be called separately when a host needs to inspect input before submission.

If a host has already parsed input, the `BoundCommand` overloads of `TryExecuteCommand` and `ExecuteCommand` accept that result directly. They skip reparsing, but still record the submitted input, run validation, invoke the handler, and record the outcome.

`CommandContext` gives the handler the original input, matched definition, bound command, execution timestamp, cancellation token, and optional host dispatcher. A handler returns a `CommandResult`; it does not write to a hidden output stream.

Input echo is enabled by default through `CommandExecutionOptions.EchoInput`. A command can override that choice with `EchoInput()` or `DoNotEchoInput()` on its builder. The input remains in `CommandHistory` even when visible echo is disabled. A successful execution appends declared success outputs first, followed by outputs returned by the handler.

Handler exceptions are converted into structured command failures. The try methods return those failures rather than throwing. The execute methods wrap a command failure in `ConsoleOperationException`; when a handler threw, the original exception is also available as `InnerException` and is represented in the structured failure cause chain. Standard argument errors such as a null input still throw directly.

## Asynchronous handlers

Use `ExecuteAsync` for a handler that awaits I/O or other asynchronous work. Both simple and typed handlers can return a result or just a `Task`; a task that completes successfully without a result is treated as `CommandResult.Success()`.

```csharp
console.RegisterCommand(new CommandBuilder("player.save")
    .ExecuteAsync(async context =>
    {
        await saveService.SaveAsync(context.CancellationToken);
        return CommandResult.Success(CommandOutput.Inline("Saved."));
    })
    .Build());

CommandResult result = await console.TryExecuteCommandAsync("player.save");
```

Manager execution is available for both raw strings and parsed `BoundCommand` values:

```csharp
CommandResult result = await console.TryExecuteCommandAsync(input, cancellationToken);
CommandResult result = await console.TryExecuteCommandAsync(boundCommand, cancellationToken);

CommandResult result = await console.ExecuteCommandAsync(input, cancellationToken);
CommandResult result = await console.ExecuteCommandAsync(boundCommand, cancellationToken);
```

Async methods also run synchronous handlers. A synchronous manager method never blocks waiting for an async handler: it returns a `CommandRequiresAsyncExecution` failure without invoking that handler. Use a try method when command rejection is an expected outcome. The execute methods throw `ConsoleOperationException` for command failures.

The manager records command input and configured visible input echo before awaiting a handler. It writes returned outputs or one failure entry only after the handler completes. Concurrent commands may overlap, so visible outputs and failures appear in completion order; input history and echo entries appear at submission time. There is no intermediate or streaming output in this API.

## Cancellation

Async manager methods accept an optional `CancellationToken`, which is available as `CommandContext.CancellationToken`. Synchronous execution gives handlers `CancellationToken.None`. A token that is already cancelled still causes submitted input and configured echo to be recorded before the manager returns a structured `CommandExecutionCancelled` failure without calling the handler.

Cancellation is cooperative. The manager treats an `OperationCanceledException` as cancellation only when this execution's token is requested. An unrelated `OperationCanceledException` is a normal handler exception and uses `CommandExecutionRejected`. If a handler ignores cancellation and returns a result, the manager accepts that result.

## Host dispatcher

Hosts may inject an `ICommandDispatcher` through the three-argument `ConsoleManager` constructor. The handler can use `context.Dispatcher` to ask the host to run a small callback on a host-selected thread:

```csharp
ICommandDispatcher dispatcher = context.Dispatcher
    ?? throw new InvalidOperationException("This console has no dispatcher.");

await dispatcher.InvokeAsync(() => gameWorld.Teleport(player, destination), context.CancellationToken);
```

`InvokeAsync(Action, CancellationToken)` and `InvokeAsync<T>(Func<T>, CancellationToken)` dispatch only the supplied callback. ConsoleSystem does not schedule the whole handler or promise that its continuation resumes on that thread. If no dispatcher was injected, `context.Dispatcher` is `null`; the handler should check it or report a clear host-configuration error. Dispatcher callbacks run without a ConsoleSystem history lock held.

Visible console history and command input history synchronize snapshots, mutations, and synchronous change notifications. A notification runs on the thread that performed the write. UI hosts should marshal their own UI work to the appropriate thread. See [Command outputs and failures](COMMAND_OUTPUTS_AND_FAILURES.md) for result construction and entry behavior.
