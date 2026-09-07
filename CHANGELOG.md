# Changelog

This file records notable user-facing changes to `Workes.ConsoleSystem`.

## [0.1.0] - Unreleased

First public release of the engine-neutral console MVP.

### Added

- Added `ConsoleManager` as the root object for one console instance.
- Added bounded `ConsoleHistory` for visible console entries and bounded `CommandHistory` for submitted input navigation.
- Added manager-owned logging methods and `LogEntry` values.
- Added immutable command registration through `CommandBuilder` and read-only registry inspection through `CommandSystem`.
- Added configurable command parsing, including quoted strings, flag/option prefixes, option value styles, boolean literal aliases, and case sensitivity.
- Added `CommandParseResult` and `CommandValidationResult` with `IsSuccess` and structured `ConsoleFailure` values.
- Added typed state binding for positional arguments, flags, and options.
- Added typed command constraints plus option range and allowed-value validation.
- Added synchronous command execution through `TryExecuteCommand(...)` and `ExecuteCommand(...)`.
- Added `CommandInputEntry`, `CommandOutputEntry`, and `CommandFailureEntry` for the shared console history.
- Added `CommandResult`, semantic `CommandOutput`, inline/block output, style IDs, segment data, and plain text derivation.
- Added stateless command autocomplete with replacement helpers, command path candidates, flag/option candidates, and command-provided value candidates.
- Added dot-segment command path completion for path-like command sets.
- Added package-wide `ConsoleText`, optional formatting, lenient markup parsing, themes, styles, colors, format models, markup profiles, and formatter abstractions.
- Added plain text, Unity rich text, and Godot BBCode formatters.
- Added focused user-facing guides for concepts, configuration, histories, UI integration, command registration, execution, parsing, validation, autocomplete, failures, output, and formatting.
- Added executable example tests for logging/history, command execution, formatting, and autocomplete.