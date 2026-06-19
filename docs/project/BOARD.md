# Project Board

This board tracks package work in a Trello-like Markdown format.

Each card should describe one coherent piece of work. Keep acceptance notes small and observable so a person can tell when the card is done.

## Backlog

### Design command registration model

Define the setup-time API for registering commands, including how duplicate command paths are rejected and when the registry becomes immutable.

Acceptance notes:
- Normal registration workflow is documented.
- Duplicate path behavior is decided.
- No parsing or execution behavior is implemented as part of the design-only step.

### Design command execution surface

Decide whether commands expose an abstract `ExecuteAsync`, a separate handler/delegate model, or another small execution contract.

Acceptance notes:
- The selected API supports testable command logic.
- The API does not require a UI or engine dependency.
- The design explains how command output returns to console history.

### Design command path representation

Decide whether command paths remain strings initially or become a small value type before registration behavior is implemented.

Acceptance notes:
- Hierarchical paths such as `Item Add` and `Player Add` are addressed.
- Comparison rules are documented.
- The design avoids argument, alias, and autocomplete behavior.

### Design console history limits

Decide how bounded history should work without introducing storage policies too early.

Acceptance notes:
- Default capacity behavior is documented.
- Public read-only exposure remains clear.
- Failed or rejected additions, if any, have defined behavior.

### Draft README normal workflow

Write the first README example after the core API shape is stable enough to show normal usage.

Acceptance notes:
- Example starts from creating one `GameConsole`.
- Logging and command concepts are introduced without implying implemented features.
- Examples compile or are clearly marked as future-facing.

## Next

### Review minimal skeleton API

Review the current skeleton before adding behavior.

Acceptance notes:
- Public type names and namespaces are approved.
- Any placeholder types that feel premature are removed or renamed.
- Any missing minimal invariants are identified.

## In Progress

No active cards.

## Blocked

No blocked cards.

## Done

### Create minimal package skeleton

Created the initial public type structure for the package.

Completed notes:
- Added `GameConsole` as the central coordinator.
- Added console entry types and severity enums.
- Added minimal history, logging, and command placeholder types.
- Added construction and invariant tests.

