# Workes.ConsoleSystem

[![NuGet](https://img.shields.io/nuget/v/Workes.ConsoleSystem.svg)](https://www.nuget.org/packages/Workes.ConsoleSystem)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/LICENSE)

Workes.ConsoleSystem is an engine-neutral .NET package for building an in-game or tool-facing console around one shared chronological history.

The initial package shape provides the console root, log facade, entry model, and command placeholders. Command registration, parsing, and execution are intentionally still future work.

## Highlights

- Create one `GameConsole` to coordinate console state.
- Write log messages into a shared chronological `ConsoleHistory`.
- Keep command input history separate from the rendered console entry stream.
- Start from a command-system surface that can evolve without binding the package to a specific UI or engine.

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
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Entries;

var console = new GameConsole();

console.Log.Information("Console ready.");
console.Log.Warning("Example warning.");

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

Focused guides:

- Additional focused guides will be added as command behavior and history policies become stable.

See the [Changelog](CHANGELOG.md) for release history and migration-sensitive changes.

## License

Workes.ConsoleSystem is available under the [MIT License](LICENSE).

