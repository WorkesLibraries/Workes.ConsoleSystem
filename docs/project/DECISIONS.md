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
