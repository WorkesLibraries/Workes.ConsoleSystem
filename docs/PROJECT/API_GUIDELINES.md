# API GUIDELINES

## Purpose

Describe the design philosophy and conventions for the package's public API.

This is a project-control document for keeping the public API consistent. It is not intended to be an exhaustive public API reference.

## Usage

Use this file before changing public-facing types, method names, configuration flows, extension points, or usage patterns.

## Maintenance

Update when the project's API design principles, naming conventions, consistency rules, or overall API philosophy changes.

## Rules

- Document API design intent, not every public member.
- Separate stable API principles from experimental ideas.
- Include short examples of preferred API shape when useful.
- Keep README examples aligned with the normal workflow and explicit package-version installation style.
- Do not document private implementation details here.
- Do not use this as a task list.

## API Design Principles

- Keep the package engine-neutral and UI-neutral.
- Prefer explicit ownership over global/static access.
- Keep the public surface small until workflows are proven.
- Separate implemented behavior from future placeholders in names, docs, and examples.
- Favor simple immutable entry models for console history.
- Keep simple command registration simple, while letting complex commands opt into an explicit typed state model.

## Naming And Structure

- Use `GameConsole` as the root object for the package-level console system.
- Keep namespaces grouped by responsibility: `Core`, `History`, `Logging`, `Entries`, and `Commands`.
- Use clear domain names such as `ConsoleHistory`, `ConsoleLog`, `LogEntry`, and `CommandHistory`.
- Avoid names that imply a game engine, UI framework, storage layer, or singleton lifecycle.

## Configuration Style

There is no public configuration surface yet.

Future configuration should prefer constructor options or small option objects when configuration becomes necessary. Avoid hidden global configuration.

## Command API Direction

Command registration should use one schema model instead of separate command types for non-parameterized, flag-parameterized, option-parameterized, and positional commands.

Simple commands should be possible without a command state type:

```csharp
commands.Register("noclip")
    .Execute(ctx => ToggleNoclip());
```

Complex commands should prefer small immutable state records:

```csharp
public sealed record RestartCommand(
    bool IgnorePlayers,
    int DelaySeconds);

commands.Register<RestartCommand>("server.restart")
    .Flag(x => x.IgnorePlayers, "--ignore-players", "-i")
    .Option(x => x.DelaySeconds, "--delay", "-d")
        .Default(0)
        .Range(0, 3600)
    .MutuallyExclusive(x => x.IgnorePlayers, x => x.DelaySeconds)
    .Execute((ctx, state) => RestartServer(state));
```

Prefer this typed state-record model over string IDs or mutable parameter handles as the primary user-facing API.

Command input should follow this order:

```text
<path> <required positional arguments...> [flags and options...]
```

Use positional arguments for required core command state. Use named options or flags for optional state. Do not make optional positional arguments part of the first public design.

Flag and option registration should support multiple user-facing names or aliases. Option and positional-argument value autocomplete should be opt-in through command-provided candidate functions.

Constraints should run against the fully bound command state and should be expressible through state properties or reusable constraint helpers.

Command definitions should become immutable after registration/finalization so parsing and execution read a validated schema.

## Console Entry API Direction

`IConsoleEntry` should remain an extensibility point for all console-visible entries. It should require timestamp and entry type/kind information, but it should not require log severity.

Built-in entry types should include log entries, command input entries, command output entries, and command failure/error entries. Users should be able to add custom entry types for game-specific console events.

Keep `LogLevel` on `LogEntry`. Command output should use command-output-specific metadata such as `CommandOutputLevel`. Custom entries should be free to expose metadata that makes sense for their domain.

## Error Handling And Validation

- Throw `ArgumentNullException` for null values that cannot be represented safely.
- Keep invalid-state rules close to the type that owns the state.
- Do not add broad exception hierarchies until command behavior or history policies create real error cases.

## Examples Of Preferred API Shape

```csharp
var console = new GameConsole();

console.Log.Information("Console ready.");
```

Prefer examples that start from one owned `GameConsole` instance and show implemented behavior only.
