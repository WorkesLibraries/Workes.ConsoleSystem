# Workes.ConsoleSystem

[![NuGet](https://img.shields.io/nuget/v/Workes.ConsoleSystem.svg)](https://www.nuget.org/packages/Workes.ConsoleSystem)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/LICENSE)

Workes.ConsoleSystem is an engine-neutral console foundation for applications, games, and tools. It provides one manager-owned API for logging, retained console entries, command input history, command autocomplete, optional history change events, opt-in command permissions and availability checks, configurable help commands, and synchronous or asynchronous command registration, parsing, validation, and execution.

Version 0.8.0 adds immediate streamed command output, optional per-execution progress state, and transient progress notifications with terminal outcomes. It also supports asynchronous command handlers, cooperative cancellation, optional host-thread callback dispatch, and synchronized histories for overlapping commands. It continues the rebuild introduced in 0.2.0 around `Workes.ContentSystem`, which provides internal retained-content storage. ConsoleSystem keeps its own public manager, entry, text, command, and history APIs; consumers do not need to create or call ContentSystem objects directly. This pre-1.0 release is not intended as a drop-in replacement for earlier APIs.

## Highlights

- Record Trace through Critical logs directly on `ConsoleManager`.
- Keep bounded, oldest-first console history and separate command navigation history.
- Represent logs, command input, output, failures, and custom entries with ConsoleSystem types.
- Register typed commands and process them through parsing, validation, and synchronous or asynchronous handlers.
- Offer stateless command, option, and configured value autocomplete for host input controls.
- Restrict commands with optional caller permissions and runtime availability checks.
- Register a configurable help command with positional or option-based search and command details.
- Subscribe to optional synchronous history change events for UI updates.
- Pass cancellation tokens to async executions and explicitly dispatch host-thread callbacks when needed.
- Keep presentation decisions in the host UI with semantic `ConsoleText` segments.
- Configure the manager once; it snapshots the options and exposes a detached options snapshot.

## Installation

Install the package from [NuGet](https://www.nuget.org/packages/Workes.ConsoleSystem):

```bash
dotnet add package Workes.ConsoleSystem --version 0.8.0
```

Or add a package reference:

```xml
<PackageReference Include="Workes.ConsoleSystem" Version="0.8.0" />
```

The package targets .NET Standard 2.1. Its internal storage dependency is restored transitively; normal usage goes through ConsoleSystem APIs.

## Quick Example

```csharp
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Entries;

var console = new ConsoleManager();
console.RegisterCommand(new CommandBuilder("greet")
    .Argument<GreetState>(state => state.Name, id: "name", name: "name")
    .Execute<GreetState>((context, state) =>
        CommandResult.Success(CommandOutput.Inline($"Hello, {state.Name}!")))
    .Build());

console.LogInformation("Console ready.");

if (console.TryExecuteCommand("greet Mira", out CommandResult result))
{
    foreach (IConsoleEntry entry in console.History.Entries)
    {
        if (entry is CommandOutputEntry output)
        {
            System.Console.WriteLine(output.PlainText);
        }
    }
}
else
{
    System.Console.Error.WriteLine(result.Failure?.Message);
}

internal sealed record GreetState(string Name);
```

The [Quick Start](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/docs/QUICK_START.md) walks through this manager-centered workflow. See the focused guides for configuration, logging, history, entries, and each command stage.

## Documentation

- [API reference](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/docs/API_REFERENCE.md)
- [Quick Start](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/docs/QUICK_START.md)
- [Manager and configuration](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/docs/MANAGER_AND_CONFIGURATION.md)
- [Logging](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/docs/LOGGING.md)
- [Entries and semantic text](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/docs/ENTRIES_AND_TEXT.md)
- [Console and command histories](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/docs/HISTORY.md)
- [History change hooks](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/docs/HISTORY_CHANGE_HOOKS.md)
- [Command registration](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/docs/COMMAND_REGISTRATION.md)
- [Command parsing](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/docs/COMMAND_PARSING.md)
- [Command validation](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/docs/COMMAND_VALIDATION.md)
- [Command autocomplete](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/docs/COMMAND_AUTOCOMPLETE.md)
- [Command permissions and availability](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/docs/COMMAND_ACCESS.md)
- [Configurable help command](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/docs/HELP_COMMAND.md)
- [Command execution](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/docs/COMMAND_EXECUTION.md)
- [Command outputs and failures](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/docs/COMMAND_OUTPUTS_AND_FAILURES.md)
- [Changelog](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/CHANGELOG.md)

Version 0.8.0 adds streamed handler output and transient progress reporting to async command handlers, cancellation, and explicit host-thread callback dispatch. These features build on the logical command value model, permissions, availability checks, autocomplete, and configurable help. See the [Command execution guide](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/docs/COMMAND_EXECUTION.md) and [changelog](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/CHANGELOG.md) for details. History persistence is not part of the public API.

## License

Workes.ConsoleSystem is available under the [MIT License](LICENSE).
