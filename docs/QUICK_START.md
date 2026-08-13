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
        CommandHistoryCapacity = 100,
        CommandHistoryDuplicatePolicy = CommandHistoryDuplicatePolicy.RejectConsecutive
    }
});
```

The manager snapshots supplied options during construction. Changing the options object afterwards does not change the manager.

Current option areas are command parsing preferences, history capacities, command input duplicate handling, and presentation extension slots. Parsing behavior and concrete presentation formatting are planned later stages.

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

console.Log.Information("First");
console.Log.Information("Second");
console.Log.Information("Third");

Console.WriteLine(console.History.Entries.Count); // 2
```

Command input history stores submitted command strings for UI navigation. Blank inputs are ignored. Consecutive duplicate inputs are rejected by default using trimmed, case-insensitive comparison, but the original submitted text is preserved when it is stored.

```csharp
console.CommandHistory.Add("noclip");      // true
console.CommandHistory.Add(" NOCLIP ");    // false
console.CommandHistory.Add("help");        // true
console.CommandHistory.Add("noclip");      // true
```

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

- [ConsoleManager](CONSOLE_MANAGER.md) for the root object and ownership model.
- [Configuration](CONFIGURATION.md) for options, defaults, and snapshot behavior.
- [Console History](CONSOLE_HISTORY.md) for rendered console entries and retention.
- [Command History](COMMAND_HISTORY.md) for submitted command input history.
- [CHANGELOG.md](../CHANGELOG.md) for release history and migration-sensitive changes.

