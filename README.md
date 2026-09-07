# Workes.ConsoleSystem

[![NuGet](https://img.shields.io/nuget/v/Workes.ConsoleSystem.svg)](https://www.nuget.org/packages/Workes.ConsoleSystem)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/LICENSE)

Workes.ConsoleSystem is an engine-neutral .NET package for building an in-game or tool-facing console around one shared chronological history.

It provides the console root, configuration, logging facade, entry model, command registration and execution, command parsing and validation, stateless autocomplete, structured failures, and semantic command output.

## Highlights

- Create one `ConsoleManager` to coordinate console state.
- Configure parsing, history, optional formatting, and execution defaults through `ConsoleManagerOptions`.
- Configure autocomplete path behavior for whole-path or dot-segment completion.
- Write log messages into a bounded shared chronological `ConsoleHistory`.
- Keep bounded command input history separate from the rendered console entry stream.
- Register immutable command definitions without binding the package to a specific UI or engine.
- Execute registered commands through one manager-owned runtime flow.
- Parse registered commands into structured success/failure results and typed command state.
- Validate parsed commands against option rules and typed constraints.
- Get stateless autocomplete candidates for paths, flags, options, and command-provided values.
- Branch on package-wide structured failures instead of parsing messages.
- Return semantic command output with plain text derivation and style IDs.
- Opt into package-managed markup, themes, and Unity/Godot formatters when a host wants them.

## Installation

Install the package from [NuGet](https://www.nuget.org/packages/Workes.ConsoleSystem):

```bash
dotnet add package Workes.ConsoleSystem --version 0.1.0
```

Or add a package reference:

```xml
<PackageReference Include="Workes.ConsoleSystem" Version="0.1.0" />
```

The package targets .NET Standard 2.1.

## Quick Example

```csharp
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Entries;

var console = new ConsoleManager();

console.LogInformation("Console ready.");

console.RegisterCommand(new CommandBuilder("noclip")
    .SuccessOutputInline("Noclip enabled.", defaultStyle: "Success")
    .Execute(ctx => new CommandResult())
    .Build());

if (!console.TryExecuteCommand("noclip", out CommandResult result))
{
    Console.WriteLine(result.Failure!.Message);
}

foreach (var entry in console.History.Entries)
{
    if (entry is LogEntry log)
    {
        Console.WriteLine($"[{log.Level}] {log.Message}");
    }
}
```

See the [Quick Start](docs/QUICK_START.md) for the beginner-first walkthrough.

## Documentation

Start here:

1. [Quick Start](docs/QUICK_START.md)
2. [Core Concepts](docs/CONCEPTS.md)

Focused guides:

- [ConsoleManager](docs/CONSOLE_MANAGER.md)
- [Configuration](docs/CONFIGURATION.md)
- [Console History](docs/CONSOLE_HISTORY.md)
- [Console UI Integration](docs/CONSOLE_UI_INTEGRATION.md)
- [Command History](docs/COMMAND_HISTORY.md)
- [Command Registration](docs/COMMAND_REGISTRATION.md)
- [Command Execution](docs/COMMAND_EXECUTION.md)
- [Command Parsing](docs/COMMAND_PARSING.md)
- [Command Validation](docs/COMMAND_VALIDATION.md)
- [Command Autocomplete](docs/COMMAND_AUTOCOMPLETE.md)
- [Failure Handling](docs/FAILURES.md)
- [Command Results And Output](docs/COMMAND_OUTPUT.md)
- [Formatting](docs/FORMATTING.md)

See the [Changelog](CHANGELOG.md) for release history and migration-sensitive changes.

The repository also contains executable examples under `tests/Examples` for logging/history, command execution, formatting, and autocomplete.

## License

Workes.ConsoleSystem is available under the [MIT License](LICENSE).

