# Decision Log

This file records settled package-shape and architecture decisions.

Use short entries. Each entry should explain what was decided and why, without turning into a full design document.

## Accepted

### 2026-06-19: One logical console, not a singleton

`GameConsole` represents the normal root object for one running game console. Applications are expected to create one instance during startup and keep it for the game lifetime, but the package does not enforce global static state or singleton behavior.

Reasoning:
- Keeps ownership explicit and testable.
- Avoids hidden global state.
- Leaves room for unusual hosts or tests to create more than one console.

### 2026-06-19: Console history is the shared chronological record

Logs, command input, command output, and future command failures should all flow into one shared `ConsoleHistory`.

Reasoning:
- The console and log are not separate systems.
- UI consumers can render one chronological stream.
- Command behavior can remain internally separated while still producing shared console output.

### 2026-06-19: Keep initial command system non-functional

The first skeleton includes `CommandSystem`, `Command`, `CommandDefinition`, `CommandContext`, and `CommandResult`, but does not implement registration, parsing, execution, permissions, autocomplete, aliases, arguments, or options.

Reasoning:
- The package needs public shape before behavior.
- Command behavior has several unsettled design choices.
- Avoids speculative abstractions before the normal workflow is approved.

### 2026-06-19: Use plain Markdown for project tracking

Project tracking starts with a small `docs/project` Markdown structure rather than a generated dashboard or local website.

Reasoning:
- Easy to edit by hand.
- Easy for Codex to read and update.
- Low maintenance while the package is still early.

## Proposed

No open decisions.

