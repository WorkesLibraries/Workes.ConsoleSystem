# Quick Start

This walkthrough creates a manager, configures its history, registers a typed command, executes input, and reads the resulting entries. It uses only public Workes.ConsoleSystem APIs.

## Prerequisites

- A .NET project that can reference .NET Standard 2.1 libraries.

## Installation

Install version 0.3.0 from NuGet:

```bash
dotnet add package Workes.ConsoleSystem --version 0.3.0
```

Or add a package reference to your project file:

```xml
<PackageReference Include="Workes.ConsoleSystem" Version="0.3.0" />
```

NuGet restores the internal `Workes.ContentSystem` dependency automatically. Application code uses the ConsoleSystem API and does not need a separate ContentSystem reference. See the [README](../README.md#installation) for package details.

## Create and configure the manager

```csharp
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Entries;

var console = new ConsoleManager(new ConsoleManagerOptions
{
    History = new HistoryOptions
    {
        ConsoleHistoryCapacity = 500,
        CommandHistoryCapacity = 100
    },
    CommandParsing = new CommandParsingOptions
    {
        AllowInterleavedOptions = false
    }
});
```

The manager snapshots the options during construction. Later changes to the setup object do not change this console instance. `console.Options` also returns a detached snapshot. See [Manager and configuration](MANAGER_AND_CONFIGURATION.md).

## Register a command

Typed command state is a reference type whose public constructor parameters match the bound property names. A record is a convenient fit:

```csharp
console.RegisterCommand(new CommandBuilder("greet")
    .Description("Greets a player.")
    .Argument<GreetState>(state => state.Name, "name")
    .Execute<GreetState>((context, state) =>
        CommandResult.Success(CommandOutput.Inline($"Hello, {state.Name}!")))
    .Build());

internal sealed record GreetState(string Name);
```

`CommandBuilder.Build()` creates the definition; `ConsoleManager.RegisterCommand` owns registration. See [Command registration](COMMAND_REGISTRATION.md) for flags, options, aliases, ranges, and constraints.

## Execute and inspect

```csharp
console.LogInformation("Console ready.");

if (console.TryExecuteCommand("greet Mira", out CommandResult result))
{
    foreach (IConsoleEntry entry in console.History.Entries)
    {
        switch (entry)
        {
            case LogEntry log:
                System.Console.WriteLine($"[{log.Level}] {log.Message}");
                break;
            case CommandInputEntry input:
                System.Console.WriteLine($"> {input.Input}");
                break;
            case CommandOutputEntry output:
                System.Console.WriteLine(output.PlainText);
                break;
            case CommandFailureEntry failure:
                System.Console.Error.WriteLine(failure.Message);
                break;
        }
    }
}
else
{
    System.Console.Error.WriteLine(result.Failure?.Message);
}
```

Execution parses and validates the input before calling the handler. On success, the manager records any enabled input echo and then the declared and handler-produced outputs. A rejected command produces a structured failure result and a `CommandFailureEntry`. `TryExecuteCommand` returns `false` for command failures; `ExecuteCommand` is the throwing alternative. See [Command execution](COMMAND_EXECUTION.md) and [outputs and failures](COMMAND_OUTPUTS_AND_FAILURES.md).

## Autocomplete and history updates

For a host input control, ask the manager for candidates at the current cursor position and apply the selected candidate:

```csharp
CommandAutocompleteResult suggestions = console.GetAutocomplete(input, cursorIndex);
if (suggestions.Candidates.Count > 0)
{
    input = suggestions.Apply(suggestions.Candidates[0]);
}
```

Autocomplete is a stateless query. It returns candidate data and a replacement range; the host chooses its menu, selection, and keyboard behavior. Configure option value suggestions on each command schema member where they are wanted. `AllowedValues(...)` validates values, while `SuggestAllowedValues()` separately opts that set into autocomplete.

Subscribe to history changes when the UI needs push notifications instead of polling:

```csharp
console.History.Changed += (_, change) => RefreshConsoleView(change);
console.CommandHistory.Changed += (_, change) => RefreshCommandInput(change);
```

Both events are synchronous and optional. Their ConsoleSystem-owned payloads report added, dropped, or cleared items. See [Command autocomplete](COMMAND_AUTOCOMPLETE.md) and [History change hooks](HISTORY_CHANGE_HOOKS.md) for candidate rules, event payloads, and ordering.

## What to read next

- [Manager and configuration](MANAGER_AND_CONFIGURATION.md)
- [Logging](LOGGING.md)
- [Entries and semantic text](ENTRIES_AND_TEXT.md)
- [Console and command histories](HISTORY.md)
- [History change hooks](HISTORY_CHANGE_HOOKS.md)
- [Command parsing](COMMAND_PARSING.md)
- [Command validation](COMMAND_VALIDATION.md)
- [Command autocomplete](COMMAND_AUTOCOMPLETE.md)
