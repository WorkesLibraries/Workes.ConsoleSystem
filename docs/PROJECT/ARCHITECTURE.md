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

Workes.ConsoleSystem is currently a small engine-neutral package centered on `GameConsole`.

`GameConsole` owns the shared console history, command input history, logging facade, and command-system placeholder. It is the normal root object for a host application, but it is not a singleton and does not use static global state.

## Main Components

- `GameConsole` coordinates the package-level systems.
- `ConsoleHistory` stores one chronological stream of `IConsoleEntry` values.
- `ConsoleLog` is the developer-facing facade for adding `LogEntry` values to the shared history.
- `CommandHistory` stores submitted command input strings for future UI navigation.
- `CommandSystem` is present as the future command registry/execution surface, but command behavior is intentionally not implemented yet.
- Entry types under `Workes.ConsoleSystem.Entries` represent log entries, command input, and command output.

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

## Data Flow / Control Flow

Log calls flow through `GameConsole.Log` into `ConsoleLog`, which appends `LogEntry` instances to `GameConsole.History`.

Console UI code is expected to read `GameConsole.History.Entries` and render entries according to their concrete type.

Command input, parsing, execution, permissions, aliases, arguments, options, and autocomplete are not part of the implemented flow yet.

The planned command execution flow is:

```text
raw input
-> parse path, required positional arguments, flags, and options
-> resolve command definition
-> bind values into a typed command state record or simple command context
-> run schema validation and constraints
-> execute handler
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
