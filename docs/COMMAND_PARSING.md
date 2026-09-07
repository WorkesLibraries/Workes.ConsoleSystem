# Command Parsing

Command parsing reads raw command input against registered command schemas and binds values into command state.

Parsing does not write to console history, write command input history, validate command constraints, or execute command handlers.

Most runtime command submissions should go through command execution once it is available. Use `ParseCommand(...)` directly when a UI, test, or tool needs to inspect command input without running it.

## Basic Parsing

Register commands, then call `ParseCommand` when you need side-effect-free inspection.

```csharp
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Core;

var console = new ConsoleManager();

console.RegisterCommand(new CommandBuilder("noclip")
    .Execute(ctx => new CommandResult())
    .Build());

CommandParseResult result = console.ParseCommand("noclip");

if (result.Success)
{
    Console.WriteLine(result.Command!.Definition.Path);
}
```

`ParseCommand` returns a structured result instead of throwing for normal user input mistakes such as unknown commands, missing arguments, duplicate flags, or invalid values.

`ParseCommand(null)` throws `ArgumentNullException` because null input is a caller error.

## Input Shape

Command input follows this shape:

```text
<path> <required positional arguments...> [flags/options...]
```

Positional arguments are required and must appear immediately after the path. Flags and options may appear in any order after the required positional arguments.

Optional state should be modeled as named options or flags, not optional positional arguments.

## Typed State Binding

Complex commands bind parsed values into a typed command state object.

```csharp
public sealed record RestartCommandState(
    string Reason,
    bool IgnorePlayers,
    int DelaySeconds);

var restart = new CommandBuilder("server.restart")
    .Argument<RestartCommandState>(x => x.Reason, "reason")
    .Flag<RestartCommandState>(x => x.IgnorePlayers, "ignore-players", "i")
    .Option<RestartCommandState>(x => x.DelaySeconds, "delay", "d")
        .Default(10)
    .Execute<RestartCommandState>((ctx, state) => new CommandResult())
    .Build();

console.RegisterCommand(restart);

CommandParseResult result = console.ParseCommand("server.restart maintenance --ignore-players --delay 5");
RestartCommandState state = result.Command!.GetState<RestartCommandState>()!;
```

State is created through public constructor binding. Constructor parameter names must match bound property names. Matching is case-insensitive by default and follows `CommandParsingOptions.IsCaseSensitive`.

## Flags And Options

Flag and option schema names are prefix-free:

```csharp
.Flag<RestartCommandState>(x => x.IgnorePlayers, "ignore-players", "i")
.Option<RestartCommandState>(x => x.DelaySeconds, "delay", "d")
```

The parser applies `CommandParsingOptions.FlagAndOptionPrefix`. The default prefix is `--`, so those names are typed as:

```text
--ignore-players
--i
--delay 10
--d 10
```

Use a custom prefix when your command vocabulary can safely support it.

```csharp
var console = new ConsoleManager(new ConsoleManagerOptions
{
    CommandParsing = new CommandParsingOptions
    {
        FlagAndOptionPrefix = "/"
    }
});
```

With that configuration, users type `/delay 10`.

## Option Value Style

`OptionValueStyle` controls option value syntax.

- `SpaceSeparated`: `--delay 10`
- `EqualSeparated`: `--delay=10`
- `AnySeparated`: both forms

The default is `SpaceSeparated`.

## Quoted Strings

Single-quoted and double-quoted strings are supported by default.

```text
say "hello world"
say 'hello world'
```

Escaped matching quotes and backslashes work inside quoted strings.

```text
say "hello \"world\""
```

Unclosed quotes fail parsing with `ConsoleFailureCodes.CommandQuoteUnclosed`.

## Supported Value Types

The parser supports:

- `string`
- `bool`
- numeric primitive types
- enums
- nullable versions of supported value types

Bool option values accept configured boolean literals. Defaults are `true` and `false`, matched case-insensitively by default.

```csharp
var console = new ConsoleManager(new ConsoleManagerOptions
{
    CommandParsing = new CommandParsingOptions
    {
        BooleanLiterals = new BooleanLiteralOptions
        {
            TrueLiterals = new[] { "true", "yes", "on" },
            FalseLiterals = new[] { "false", "no", "off" }
        }
    }
});
```

Flags bind to `true` when present and `false` when absent. Missing options bind to their configured `.Default(...)` metadata when present, otherwise to the target type default.

Option ranges, allowed values, and typed constraints are checked by `ConsoleManager.ValidateCommand(...)` after parsing succeeds.

## Parse Results

Successful results expose a `BoundCommand`.

```csharp
CommandParseResult result = console.ParseCommand("server.restart maintenance --delay 5");

if (result.Success)
{
    Console.WriteLine(result.Command!.Arguments["reason"]);
    Console.WriteLine(result.Command.Options["delay"]);
}
```

Failed results expose a `ConsoleFailure` with stable kind/code values and a human-readable message.

```csharp
CommandParseResult result = console.ParseCommand("server.restart");

if (!result.Success)
{
    Console.WriteLine(result.Failure!.Kind);
    Console.WriteLine(result.Failure.Code);
    Console.WriteLine(result.Failure.Message);
}
```

## Related Guides

- [Command Registration](COMMAND_REGISTRATION.md)
- [Command Validation](COMMAND_VALIDATION.md)
- [Configuration](CONFIGURATION.md)
- [ConsoleManager](CONSOLE_MANAGER.md)
- [Failure Handling](FAILURES.md)
