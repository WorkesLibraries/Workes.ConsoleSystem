# Changelog

This file records notable user-facing changes to `Workes.ConsoleSystem`. The package keeps its existing NuGet identity. The previously published 0.1.0 package remains unchanged; version 0.2.0 is the substantial rebuild around `Workes.ContentSystem` and is not intended as a drop-in upgrade from the earlier implementation.

## [24-10-2026]

### Changed

- Refactored command bindings around required logical value IDs, resolved values, bulk defaults, and one optional trailing positional argument.
- Kept argument, flag, and option source maps limited to explicitly supplied input and added `BoundCommand.Values` for resolved values.
- Extended binding-specific range, allowed-value, and autocomplete metadata to positional arguments.

### Documentation

- Updated command registration, parsing, validation, autocomplete, API reference, Quick Start, README, architecture, and API guidelines for the new model.

## [0.4.0] - 17-10-2026

### Added

- Added opt-in command permission requirements, reusable permission roles, explicit denials, and runtime availability checks.
- Added configurable autocomplete visibility for commands blocked by permissions or availability.
- Added a focused command permissions and availability guide, including provider, role, failure, and autocomplete behavior.

## [0.3.0] - 17-10-2026

### Added

- Added stateless command autocomplete, with command path, flag, option, and command-provided value suggestions.
- Added opt-in dot-segment path completion and candidate replacement helpers.
- Added opt-in autocomplete suggestions from an option's `AllowedValues` set; explicit value providers take precedence.
- Added optional synchronous change events for visible console history and command input history, with ConsoleSystem-owned payloads and manager forwarding events.
- Added focused command autocomplete and history change hook guides.

## [0.2.0] - 17-10-2026

### Rebuilt

- Rebuilt the package around `Workes.ContentSystem` for retained visible console content. ConsoleSystem exposes its own manager, entries, text, history, and command APIs; ContentSystem's managers, sequences, records, and entry contracts remain outside the public API.
- Re-established `ConsoleManager` as the root for one console instance, including snapshotted configuration, manager-centered logging, visible history, command input history, registration, parsing, validation, and synchronous execution.

### Added

- Added ConsoleSystem-owned `IConsoleEntry`, `LogEntry`, `CommandInputEntry`, `CommandOutputEntry`, and `CommandFailureEntry` contracts.
- Added semantic `ConsoleText` and text segments, leaving rendering and formatting to the host application.
- Added Trace, Debug, Information, Warning, Error, and Critical logging methods on `ConsoleManager`, with string and `ConsoleText` overloads. All levels are recorded without a package-side minimum-level filter.
- Added bounded, oldest-first visible history and separate bounded command input history.
- Added typed command definitions with positional arguments, flags, named options, aliases, option defaults/ranges/allowed values, and custom validation constraints.
- Added configurable command parsing, including option value styles, boolean literals, quoted strings, case matching, and the default-off `AllowInterleavedOptions` setting. Interleaving preserves positional schema order; named flags and options can be reordered in either mode.
- Added side-effect-free parsing and validation APIs, plus synchronous `TryExecuteCommand` and `ExecuteCommand` workflows.
- Added command input echo settings, declared and handler-produced inline/block outputs, and structured command failures.
- Added package README, Quick Start, focused API guides, a public API reference, and package metadata describing the rebuild.

### Changed

- The public logging workflow is manager-centered; there is no public logger object.
- `ConsoleManager.History` is the ConsoleSystem-facing history access path. Its entry snapshots are detached and oldest-first; `Clear()` clears retained visible entries.
- `CommandResult` is created through the explicit `Success(...)` and `Failed(...)` factories.
- `ConsoleFailure` follows the established constructor and factory pattern used by Workes.InventorySystem's failure type.

### Not included in this release

- Command completion operations, history change events, persistence, asynchronous handlers, UI controls, markup parsing, and theme/rendering systems are not part of the 0.2.0 public API.
