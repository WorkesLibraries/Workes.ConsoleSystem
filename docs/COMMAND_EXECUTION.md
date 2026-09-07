# Command Execution

Command execution is the normal way to submit command input at runtime.

The execution APIs parse input, validate the bound command, invoke the command handler, write console history entries, and produce a `CommandResult`.

Use `TryExecuteCommand(...)` when command failure is expected and should be shown as user feedback. Use `ExecuteCommand(...)` when the command is expected to succeed and failure should interrupt the caller.

## Basic Usage

```csharp
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Core;

var console = new ConsoleManager();

console.RegisterCommand(new CommandBuilder("noclip")
    .SuccessOutputInline("<style=Success>Noclip enabled.</style>")
    .Execute(ctx => new CommandResult())
    .Build());

if (!console.TryExecuteCommand("noclip", out CommandResult result))
{
    Console.WriteLine(result.Failure!.Message);
}
```

Execution automatically validates before running the handler. You do not need to call `ParseCommand(...)` or `ValidateCommand(...)` for normal command submission.

## Execution Flow

Raw string execution follows this flow:

```text
input
-> parse
-> validate
-> execute handler
-> append history entries
-> return CommandResult
```

If you already have a successful `BoundCommand` from preflight parsing, you can execute it directly:

```csharp
CommandParseResult parse = console.ParseCommand("noclip");

if (parse.IsSuccess)
{
    bool success = console.TryExecuteCommand(parse.Command!, out CommandResult result);
}
```

The bound-command path still validates automatically before invoking the handler.

## Try And Expected-Success APIs

Use `TryExecuteCommand(...)` for interactive/user-authored input:

```csharp
if (!console.TryExecuteCommand("noclip", out CommandResult result))
{
    Console.WriteLine(result.Failure!.Message);
}
```

Use `ExecuteCommand(...)` for expected-success code:

```csharp
try
{
    CommandResult result = console.ExecuteCommand("noclip");
}
catch (ConsoleOperationException ex)
{
    Console.WriteLine(ex.Failure.Message);
}
```

Try methods return `false` for expected console failures and expose the failed `CommandResult` through the out parameter. Non-try execution throws `ConsoleOperationException` for expected console failures. Caller misuse such as null input still throws standard .NET exceptions.

## History Entries

Execution writes visible command activity to `ConsoleHistory`.

When echo is enabled, the submitted input is appended first as `CommandInputEntry`. Successful command output is appended afterwards as `CommandOutputEntry`. Framework-created failures are appended as `CommandFailureEntry`.

```text
player.give @me wood 7
Gave Workes 7 wood
```

Command input is also recorded in `CommandHistory` for UI navigation.

## Echo Input

Command input echo is enabled by default. Configure the manager default:

```csharp
var console = new ConsoleManager(new ConsoleManagerOptions
{
    Execution = new CommandExecutionOptions
    {
        EchoInput = true,
        EchoInputDefaultStyle = "CommandInput"
    }
});
```

Commands can override the manager setting:

```csharp
new CommandBuilder("quiet")
    .DoNotEchoInput()
    .Execute(ctx => new CommandResult())
    .Build();
```

Echoed input preserves the exact submitted input text.

## Failures

Parse failures, validation failures, handler-returned failures, handler exceptions, and null handler results all become failed `CommandResult` values on the try path, or `ConsoleOperationException` values on the expected-success path.

```csharp
if (!console.TryExecuteCommand("missing", out CommandResult result) &&
    result.Failure?.Code == ConsoleFailureCodes.CommandUnknown)
{
    Console.WriteLine(result.Failure.Message);
}
```

Handler exceptions are converted into `ConsoleFailureKind.CommandExecution` failures with nested cause information.

## Related Guides

- [Command Registration](COMMAND_REGISTRATION.md)
- [Command Parsing](COMMAND_PARSING.md)
- [Command Validation](COMMAND_VALIDATION.md)
- [Command Results And Output](COMMAND_OUTPUT.md)
- [Failure Handling](FAILURES.md)
- [ConsoleManager](CONSOLE_MANAGER.md)
