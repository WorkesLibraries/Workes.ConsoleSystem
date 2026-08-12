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

## Data Flow / Control Flow

Log calls flow through `GameConsole.Log` into `ConsoleLog`, which appends `LogEntry` instances to `GameConsole.History`.

Console UI code is expected to read `GameConsole.History.Entries` and render entries according to their concrete type.

Command input, parsing, execution, permissions, aliases, arguments, options, and autocomplete are not part of the implemented flow yet.

## Important Constraints

- Keep ownership explicit and testable.
- Keep UI and engine dependencies outside this package.
- Preserve one shared chronological console history for logs, command input, command output, and future command failures.
- Do not add speculative command abstractions before the registration and execution models are decided.
