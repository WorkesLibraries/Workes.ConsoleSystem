# Workes.ConsoleSystem

[![NuGet](https://img.shields.io/nuget/v/Workes.ConsoleSystem.svg)](https://www.nuget.org/packages/Workes.ConsoleSystem)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/LICENSE)

Workes.ConsoleSystem is an engine-neutral console foundation for applications, games, and tools. It provides one manager-owned API for logging, retained console entries, command input history, and synchronous command registration, parsing, validation, and execution.

Version 0.1.0 is a substantial rebuild of the existing `Workes.ConsoleSystem` package around `Workes.ContentSystem`, which now provides the internal retained-content storage. ConsoleSystem keeps its own public manager, entry, text, command, and history APIs; consumers do not need to create or call ContentSystem objects directly. This release is not intended as a drop-in replacement for APIs from the earlier implementation.

## Highlights

- Record Trace through Critical logs directly on `ConsoleManager`.
- Keep bounded, oldest-first console history and separate command navigation history.
- Represent logs, command input, output, failures, and custom entries with ConsoleSystem types.
- Register typed commands and process them through parsing, validation, and synchronous handlers.
- Keep presentation decisions in the host UI with semantic `ConsoleText` segments.
- Configure the manager once; it snapshots the options and exposes a detached options snapshot.

## Installation

Install the package from [NuGet](https://www.nuget.org/packages/Workes.ConsoleSystem):

```bash
dotnet add package Workes.ConsoleSystem --version 0.1.0
```

Or add a package reference:

```xml
<PackageReference Include="Workes.ConsoleSystem" Version="0.1.0" />
```

The package targets .NET Standard 2.1. Its internal storage dependency is restored transitively; normal usage goes through ConsoleSystem APIs.

## Quick Example

```csharp
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Entries;

var console = new ConsoleManager();
console.RegisterCommand(new CommandBuilder("greet")
    .Argument<GreetState>(state => state.Name, "name")
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

- [Quick Start](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/docs/QUICK_START.md)
- [Manager and configuration](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/docs/MANAGER_AND_CONFIGURATION.md)
- [Logging](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/docs/LOGGING.md)
- [Entries and semantic text](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/docs/ENTRIES_AND_TEXT.md)
- [Console and command histories](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/docs/HISTORY.md)
- [Command registration](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/docs/COMMAND_REGISTRATION.md)
- [Command parsing](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/docs/COMMAND_PARSING.md)
- [Command validation](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/docs/COMMAND_VALIDATION.md)
- [Command execution](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/docs/COMMAND_EXECUTION.md)
- [Command outputs and failures](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/docs/COMMAND_OUTPUTS_AND_FAILURES.md)
- [Changelog](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/CHANGELOG.md)

Autocomplete configuration is present in the options surface, but this version does not provide a completion operation. History change events and persistence are also not part of the 0.1.0 public API.

## License

Workes.ConsoleSystem is available under the [MIT License](LICENSE).
