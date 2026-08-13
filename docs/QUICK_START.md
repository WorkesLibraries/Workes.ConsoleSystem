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
        IsCaseSensitive = false
    },
    History = new HistoryOptions
    {
        ConsoleHistoryCapacity = 200,
        CommandHistoryCapacity = 100
    }
});
```

The manager snapshots supplied options during construction. Changing the options object afterwards does not change the manager.

Current option areas are command parsing preferences, history capacities, and presentation extension slots. Bounded history, parsing behavior, and concrete presentation formatting are planned later stages.

## First Working Example

```csharp
using System;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Entries;

var console = new ConsoleManager();

console.Log.Information("Console ready.");
console.Log.Error("Example error message.");

foreach (var entry in console.History.Entries)
{
    if (entry is LogEntry log)
    {
        Console.WriteLine($"{log.Timestamp:u} [{log.Level}] {log.Message}");
    }
}
```

This example demonstrates the currently implemented behavior: logging writes `LogEntry` values into the shared chronological history.

Command registration, parsing, execution, permissions, aliases, arguments, and autocomplete are not implemented yet.

## What To Read Next

- README.md for package positioning and installation.
- CHANGELOG.md for release history and migration-sensitive changes.

