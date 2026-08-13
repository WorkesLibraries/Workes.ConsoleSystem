# ARCHITECTURE

## Purpose

Describe how the project is structured internally and how the main systems relate to each other.

This is a project-control document for maintaining architectural consistency. It is not intended to replace focused user documentation under docs/.

## Usage

Use this file to understand the internal design before making structural changes.

## Maintenance

Update when the internal structure, core abstractions, dependency flow, or major implementation strategy changes.

## Rules

- Explain current architecture, not wishful future architecture.
- Prefer clear sections and diagrams when useful.
- Link to DECISIONS.md when architectural choices need rationale.
- Do not use this as a task list.
- Do not treat this as exhaustive user documentation.

## Current Architecture

Workes.ConsoleSystem is currently a small engine-neutral package centered on `ConsoleManager`.

`ConsoleManager` owns the shared console history, command input history, logging facade, and command-system placeholder. It is the normal root object for a host application, but it is not a singleton and does not use static global state.

## Main Components

- `ConsoleManager` coordinates the package-level systems.
- `ConsoleHistory` stores one chronological stream of `IConsoleEntry` values.
- `ConsoleLog` is the developer-facing facade for adding `LogEntry` values to the shared history.
- `CommandHistory` stores submitted command input strings for future UI navigation.
- `CommandSystem` is present as the future command registry/execution surface, but command behavior is intentionally not implemented yet.
- Entry types under `Workes.ConsoleSystem.Entries` represent log entries, command input, and command output.

## Root Options

`ConsoleManager` should accept an optional options object with complete defaults. Passing no options should produce the normal recommended behavior. Passing an options object with one changed value should change only that value.

Initial option areas:

- command parsing;
- history capacity/overflow behavior;
- presentation theme and output formatting defaults.

Command parsing options should include an option value syntax setting:

```csharp
public enum OptionValueStyle
{
    SpaceSeparated,
    EqualSeparated,
    AnySeparated
}
```

Default command parsing behavior:

- `OptionValueStyle.SpaceSeparated`, supporting `--delay 10`;
- case-insensitive matching for command paths, flags, and options;
- quoted string support;
- flags and options may appear in any order after path and required positional arguments.

If strict flag/option ordering is added, it should use schema/builder order.

## Planned Command Model

Command behavior is still unimplemented, but the intended design direction is settled enough to guide the next implementation work.

A command should be defined by one schema model rather than separate public command types for non-parameterized, flag-parameterized, option-parameterized, or positional commands.

The intended input order is:

```text
<path> <required positional arguments...> [flags and options...]
```

The command schema should support:

- command paths such as `noclip`, `server.restart`, or `player.give`;
- required positional arguments for core command state;
- boolean flags for modifiers;
- typed key-value options for named configurable state;
- aliases for flags and options;
- option defaults, ranges, allowed values, and similar validation metadata;
- constraints over the fully bound command state;
- autocomplete for paths, flag names, option names, and command-provided value candidates.

Optional positional arguments should not be part of the first command model. Optional state should be represented with named options or flags.

Simple commands should not require a state type. Semi-complex or complex commands should prefer small immutable typed state records, with schema bindings expressed against record properties. After a command definition is complete, registration should validate and freeze it so runtime command execution reads immutable command definitions.

The preferred registration style is explicit fluent registration against typed state records:

```csharp
commands.Register<RestartCommand>("server.restart")
    .Flag(x => x.IgnorePlayers, "--ignore-players", "-i")
    .Option(x => x.DelaySeconds, "--delay", "-d")
        .Default(10)
        .Range(0, 3600)
    .MutuallyExclusive(
        x => x.IgnorePlayers,
        x => x.DelaySeconds,
        "Ignore players cannot be combined with delayed restart.")
    .Execute((ctx, state) => RestartServer(state));
```

Constraints should carry error messages that can be surfaced in command failure entries when user input violates the constraint.

Synchronous command handlers should be the default. The design should leave room for intuitive async command handlers later, but the first implementation should prioritize synchronous debug/game-console commands.

Commands should return `CommandResult`. A result can contain zero, one, or many command output entries. Intentional command output should be returned as result entries rather than written through an imperative output sink during execution.

Framework-generated command failure entries should cover parse errors, constraint errors, and execution errors.

## Planned Output And Styling Model

Console entries should remain engine-neutral. Command output should support semantic content segments so styling can be applied by renderers without storing Unity rich text, Godot BBCode, HTML, or terminal-specific markup in the entry.

Output content should support:

- inline output;
- block or multi-line output;
- plain text derivation;
- structured command-specific data when useful;
- optional default style for an output object;
- per-segment style overrides.

Conceptual output flow:

```text
CommandResult
-> CommandOutputEntry
-> semantic text content segments
-> formatter/theme
-> plain text, Unity rich text, Godot BBCode, terminal output, or custom UI spans
```

Themes should map semantic style IDs such as `Information`, `Warning`, `Error`, `Success`, `Amount`, `Item`, or `Player` to style values. Formatters decide how those style values become a string or UI representation.

Actual engine UI rendering remains outside this package.

## Data Flow / Control Flow

Log calls flow through `ConsoleManager.Log` into `ConsoleLog`, which appends `LogEntry` instances to `ConsoleManager.History`.

Console UI code is expected to read `ConsoleManager.History.Entries` and render entries according to their concrete type.

Command input, parsing, execution, permissions, aliases, arguments, options, and autocomplete are not part of the implemented flow yet.

The planned command execution flow is:

```text
raw input
-> parse path, required positional arguments, flags, and options
-> resolve command definition
-> bind values into a typed command state record or simple command context
-> run schema validation and constraints
-> execute handler
-> receive CommandResult
-> append command input, output, and failure entries to console history
```

Autocomplete should be schema-driven where possible. Command path, flag name, and option name completion should come from registered command definitions. Positional argument values and option values should require command-provided candidate functions.

## Important Constraints

- Keep ownership explicit and testable.
- Keep UI and engine dependencies outside this package.
- Preserve one shared chronological console history for logs, command input, command output, and future command failures.
- Keep simple commands simple.
- Prefer typed state records for complex commands rather than string IDs or mutable parameter handles as the primary model.
- Do not introduce separate public command-type hierarchies for each parameterization style.
- Do not force log severity onto every console entry; severity belongs to log entries, while command output and custom entries should own their own metadata.
- Do not hard-code engine-specific text styling into entries.
- Prefer returned command output entries over imperative output writes during command execution.
