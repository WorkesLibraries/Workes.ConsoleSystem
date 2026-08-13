# DECISIONS

## Purpose

Record long-lived architectural and design decisions.

## Usage

Use this file to explain why important choices were made, especially when alternatives were rejected.

## Maintenance

Append new decisions as they are made.

If a decision changes, add a new entry instead of deleting the old one.

## Rules

- Use decision IDs like D-001, D-002, D-003.
- Include context, decision, reasoning, and consequences.
- Prefer append-only history.
- Do not use this as a task list.

## Decision Format

Use this format for new decisions:

### D-001: Decision title

#### Context

What situation, problem, or tradeoff led to this decision?

#### Decision

What did we decide?

#### Reasoning

Why was this chosen?

#### Consequences

What does this make easier, harder, or more constrained?

## Accepted Decisions

### D-001: One Logical Console, Not A Singleton

#### Context

The package needs a normal root object for a running game console, but hosts and tests should not be forced into hidden global state.

#### Decision

`GameConsole` represents the normal root object for one running game console. Applications are expected to create one instance during startup and keep it for the game lifetime, but the package does not enforce singleton behavior.

#### Reasoning

This keeps ownership explicit and testable, avoids hidden global state, and leaves room for unusual hosts or tests to create more than one console.

#### Consequences

Consumers are responsible for owning and passing the `GameConsole` instance. The package remains easier to test and less coupled to a specific host lifecycle.

### D-002: Console History Is The Shared Chronological Record

#### Context

Logs, command input, command output, and future command failures all need to be visible to console UI consumers in the order they occurred.

#### Decision

All console-facing entries should flow into one shared `ConsoleHistory`.

#### Reasoning

The console and log are not separate systems. UI consumers can render one chronological stream while command behavior remains internally separated.

#### Consequences

Future features should preserve the shared entry stream. Specialized systems may keep their own state, but user-visible console output should still become console history entries.

### D-003: Keep Initial Command System Non-Functional

#### Context

The first package skeleton needs public shape before command behavior is settled.

#### Decision

The initial package includes `CommandSystem`, `Command`, `CommandDefinition`, `CommandContext`, and `CommandResult`, but does not implement registration, parsing, execution, permissions, autocomplete, aliases, arguments, or options.

#### Reasoning

Command behavior has several unsettled design choices. Keeping the first command surface non-functional avoids speculative abstractions before the normal workflow is approved.

#### Consequences

Current public documentation must not imply commands are executable yet. Future command work should first settle registration and execution design before adding behavior.

### D-004: Trello Owns Task Tracking

#### Context

The project originally used a small Markdown board under `docs/project`, but the current project formula uses Trello for task state.

#### Decision

Task state should follow `docs/PROJECT/TRELLO_WORKFLOW.md`. The old Markdown board cards were migrated into Trello during project setup.

#### Reasoning

Trello is now the formula-level source of truth for current work, next work, backlog, bugs, blocked work, and completed work.

#### Consequences

Do not recreate a Markdown task board under `docs/PROJECT`. Assistants should use the mapped Trello board for task state.

### D-005: Commands Use One Schema Model Instead Of Separate Command Types

#### Context

The command system needs to support simple commands, flags, key-value options, positional arguments, aliases, defaults, validation constraints, and autocomplete without forcing separate command types such as non-parameterized commands, flag-parameterized commands, and option-parameterized commands.

#### Decision

Commands should be modeled as one command definition with a path, positional arguments, flags, options, constraints, and an execution handler.

The command input shape should be:

```text
<path> <required positional arguments...> [flags and options...]
```

Positional arguments should represent required core command state. Optional command state should be expressed through named options or flags instead of optional positional arguments.

#### Reasoning

A single schema model allows command creators to mix flags, options, and positional arguments naturally. Required positional arguments keep common commands bearable to type, while named options and flags make optional behavior explicit and easier to validate, document, and autocomplete.

Avoiding optional positional arguments keeps parsing, help text, autocomplete, and user expectations simpler.

#### Consequences

The parser and registration API should support required positional arguments, boolean flags, typed options, aliases for flags/options, defaults for options, and validation constraints over the fully bound command state.

Future command design should not introduce separate public command-type hierarchies for each parameterization style.

### D-006: Complex Commands Prefer Typed State Records

#### Context

The command API needs a clean way for handlers and constraints to access bound command values. String IDs are flexible but can become magic strings. Typed parameter handles avoid strings but make command definitions feel more mutable and less cleanly finalized. Class-per-command state initially felt like ceremony, but small immutable records can represent the command's input contract clearly.

#### Decision

Simple commands should remain simple and should not require a state type.

Complex commands should prefer a typed command state record as the primary clean model. The command builder should bind positional arguments, flags, and options to properties on that record, then execute handlers against a validated immutable state object.

Example intended shape:

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
    .Execute((ctx, state) =>
    {
        // use state.IgnorePlayers and state.DelaySeconds
    });
```

#### Reasoning

Typed state records give the command definition a clear input contract, avoid magic strings, preserve type safety, allow immutable bound state, and keep constraints tied to real properties. They also let a command be fully defined and validated as a complete schema before use.

The small-record cost is acceptable for semi-complex commands because the record documents the command's public input model.

#### Consequences

The future command registration API should make the typed state-record path feel first-class. Dynamic/string-based command state may exist later as an advanced escape hatch, but it should not be the primary design target.

The command system should freeze or otherwise prevent mutation of command definitions once registration is complete.

### D-007: Console Entries Are Extensible And Type-Specific

#### Context

The console history needs to contain logs, command input, command output, command failures, and possible game-specific entries. Logging severity is useful for system log messages, but not every console entry is a log.

#### Decision

Console history should store extensible `IConsoleEntry` objects. Entries should carry their own timestamp and stable entry type/kind.

Built-in entry types should cover at least system/log messages, command input, command output, and command errors or failures. Users should be able to define custom entry types for game-specific console events.

Log severity should belong to `LogEntry`, not to all console entries. Command output should use command-output-specific metadata such as output level/type instead of being forced into log severity.

#### Reasoning

Keeping severity on log entries preserves a clean model: logs have log levels, command output has command output semantics, and custom entries can expose their own metadata. Storing all of them as `IConsoleEntry` values preserves one chronological console stream while allowing extensibility.

#### Consequences

The package should not make `LogLevel` a required member of `IConsoleEntry`. Future sinks/routing can be added later if needed, but the first design should focus on extensible entry objects and the shared chronological history.
