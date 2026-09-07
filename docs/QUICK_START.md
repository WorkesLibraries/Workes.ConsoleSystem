# Quick Start

This guide should take a new user from package installation to the first successful use of `Workes.ConsoleSystem`.

## Prerequisites

You need:

- a .NET project compatible with .NET Standard 2.1.
- the .NET SDK or another NuGet-capable development environment.
- a UI, engine integration, or host application that will render the console history.

## Install The Package

Install from NuGet:

```bash
dotnet add package Workes.ConsoleSystem --version 0.1.0
```

Or add a package reference:

```xml
<PackageReference Include="Workes.ConsoleSystem" Version="0.1.0" />
```

The package targets .NET Standard 2.1.

## Minimal Setup

```csharp
using Workes.ConsoleSystem.Core;

var console = new ConsoleManager();
```

Keep the `ConsoleManager` instance for the lifetime of the host console. The package does not enforce a singleton, but most applications should create one logical console manager during startup.

## Configuration

`ConsoleManager` can also be created with options. All options have defaults, so you only need to specify the values you want to change.

```csharp
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;

var console = new ConsoleManager(new ConsoleManagerOptions
{
    CommandParsing = new CommandParsingOptions
    {
        OptionValueStyle = OptionValueStyle.AnySeparated,
        FlagAndOptionPrefix = "--",
        IsCaseSensitive = false,
        BooleanLiterals = new BooleanLiteralOptions
        {
            TrueLiterals = new[] { "true", "yes" },
            FalseLiterals = new[] { "false", "no" }
        }
    },
    History = new HistoryOptions
    {
        ConsoleHistoryCapacity = 200,
        CommandHistoryCapacity = 100,
        CommandHistoryDuplicatePolicy = CommandHistoryDuplicatePolicy.RejectConsecutive
    }
});
```

The manager snapshots supplied options during construction. Changing the options object afterwards does not change the manager.

Option areas are command parsing preferences, history capacities, command input duplicate handling, optional formatting, and command execution defaults.

## History

`ConsoleHistory` and `CommandHistory` both retain a bounded number of entries. When capacity is reached, the oldest retained item is dropped and public entries remain ordered from oldest retained to newest retained.

```csharp
var console = new ConsoleManager(new ConsoleManagerOptions
{
    History = new HistoryOptions
    {
        ConsoleHistoryCapacity = 2,
        CommandHistoryCapacity = 2
    }
});

console.LogInformation("First");
console.LogInformation("Second");
console.LogInformation("Third");

Console.WriteLine(console.History.Entries.Count); // 2
```

Command input history stores submitted command strings for UI navigation. Blank inputs are ignored. Consecutive duplicate inputs are rejected by default using trimmed, case-insensitive comparison, but the original submitted text is preserved when it is stored.

```csharp
console.RecordCommandInput("noclip");      // true
console.RecordCommandInput(" NOCLIP ");    // false
console.RecordCommandInput("help");        // true
console.RecordCommandInput("noclip");      // true
```

## First Working Example

```csharp
using System;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Entries;

var console = new ConsoleManager();

console.LogInformation("Console ready.");
console.LogError("Example error message.");

foreach (var entry in console.History.Entries)
{
    if (entry is LogEntry log)
    {
        Console.WriteLine($"{log.Timestamp:u} [{log.Level}] {log.Message}");
    }
}
```

Logging writes `LogEntry` values into the shared chronological history. Command execution writes command input, output, and failure entries to the same stream.

## Register A Command Schema

```csharp
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Core;

var console = new ConsoleManager();

var command = new CommandBuilder("noclip")
    .Description("Toggle noclip.")
    .Execute(ctx => new CommandResult())
    .Build();

console.RegisterCommand(command);

Console.WriteLine(console.Commands.Definitions.Count); // 1
```

Registered commands can be inspected, parsed, validated, and executed.

## Execute A Command

Use `TryExecuteCommand(...)` for player or user input where invalid commands should become feedback instead of exceptions.

```csharp
if (!console.TryExecuteCommand("noclip", out CommandResult result))
{
    Console.WriteLine(result.Failure!.Message);
}
```

Execution parses and validates automatically before running the command handler. It also records submitted input in `CommandHistory` and appends command input, output, or failure entries to `ConsoleHistory`.

Use `ExecuteCommand(...)` when the command is expected to succeed and failure should throw `ConsoleOperationException`.

## Inspect Command Input

Parsing and validation are side-effect-free inspection APIs. They are useful for tests, editor tooling, and UI preflight feedback. They are not intended to be the normal way to submit a command during gameplay.

```csharp
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Core;

public sealed record RestartState(
    string Reason,
    bool IgnorePlayers,
    int DelaySeconds);

var console = new ConsoleManager();

console.RegisterCommand(new CommandBuilder("server.restart")
    .Argument<RestartState>(x => x.Reason, "reason")
    .Flag<RestartState>(x => x.IgnorePlayers, "ignore-players", "i")
    .Option<RestartState>(x => x.DelaySeconds, "delay", "d")
        .Default(10)
    .Execute<RestartState>((ctx, state) => new CommandResult())
    .Build());

CommandParseResult result = console.ParseCommand("server.restart maintenance --ignore-players --delay 5");

if (result.IsSuccess)
{
    RestartState state = result.Command!.GetState<RestartState>()!;
    Console.WriteLine(state.DelaySeconds); // 5
}
else if (result.Failure?.Code == ConsoleFailureCodes.CommandUnknown)
{
    Console.WriteLine(result.Failure.Message);
}
```

Parsing validates the command shape and creates typed state. Validation checks option ranges, allowed values, and typed command constraints after parsing succeeds. This manual validation call is only needed when you want preflight feedback without executing.

```csharp
CommandValidationResult validation = console.ValidateCommand(result.Command!);

if (!validation.IsSuccess)
{
    Console.WriteLine(validation.Failure!.Message);
}
```

Validation is side-effect-free. It does not execute the stored handler or write to history.

## Autocomplete Command Input

```csharp
CommandAutocompleteResult autocomplete = console.GetAutocomplete(
    "server.restart maintenance --d",
    "server.restart maintenance --d".Length);

CommandAutocompleteCandidate candidate = autocomplete.Candidates[0];
string completed = autocomplete.Apply(candidate);
```

Autocomplete is side-effect-free. It suggests command paths, flags, options, and command-provided value candidates.

## Build Command Output

Command handlers return `CommandResult`. A result can be an empty success, a success with output, or a failed result carrying a `ConsoleFailure`.

```csharp
using Workes.ConsoleSystem.Commands;

CommandResult result = CommandResult.Success(
    CommandOutput.Inline(
        "Gave <style=Amount>10</style> gold.",
        defaultStyle: "Success"));

Console.WriteLine(result.Outputs[0].PlainText); // Gave 10 gold.
```

Command output derives plain text immediately. A formatting-enabled manager can later resolve markup into engine-specific UI text. Use the output builder when a command needs structured segment data.

## Optional Formatting

Formatting is disabled by default. Plain strings and `ConsoleText.Plain(...)` work without any formatter.

Enable formatting when you want package-managed markup and engine-specific output strings:

```csharp
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Presentation;

var console = new ConsoleManager(new ConsoleManagerOptions
{
    Formatting = ConsoleFormattingOptions.UnityRichText()
});

console.LogInformation("<style=Success><b>Console ready.</b></style>");
```

Themes map style IDs to formatting attributes:

```csharp
var theme = new ConsoleTheme(new Dictionary<string, ConsoleStyle>
{
    ["Success"] = ConsoleStyle.Standard(
        foregroundColor: ConsoleColor.FromHex("#4ade80"))
});

var themedConsole = new ConsoleManager(new ConsoleManagerOptions
{
    Formatting = ConsoleFormattingOptions.UnityRichText(theme)
});

string unityText = themedConsole.Format(
    themedConsole.CreateText("<style=Success><b>Saved</b></style>"));
```

## What To Read Next

- [ConsoleManager](CONSOLE_MANAGER.md) for the root object and ownership model.
- [Core Concepts](CONCEPTS.md) for the package mental model.
- [Configuration](CONFIGURATION.md) for options, defaults, and snapshot behavior.
- [Console History](CONSOLE_HISTORY.md) for rendered console entries and retention.
- [Console UI Integration](CONSOLE_UI_INTEGRATION.md) for rendering the shared history.
- [Command History](COMMAND_HISTORY.md) for submitted command input history.
- [Command Registration](COMMAND_REGISTRATION.md) for immutable command schemas.
- [Command Execution](COMMAND_EXECUTION.md) for direct command submission.
- [Command Parsing](COMMAND_PARSING.md) for parse results and typed value binding.
- [Command Validation](COMMAND_VALIDATION.md) for option rules and typed constraints.
- [Command Autocomplete](COMMAND_AUTOCOMPLETE.md) for stateless completion candidates.
- [Failure Handling](FAILURES.md) for structured failures and project exceptions.
- [Command Results And Output](COMMAND_OUTPUT.md) for semantic command output and formatting.
- [Formatting](FORMATTING.md) for opt-in markup, themes, and formatters.
- [CHANGELOG.md](../CHANGELOG.md) for release history and migration-sensitive changes.

