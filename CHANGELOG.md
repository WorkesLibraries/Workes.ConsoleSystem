# Changelog

This file records notable user-facing changes to `Workes.ConsoleSystem`.

## [0.1.0] - Unreleased

### Added

- Initial package scaffold.
- Added `ConsoleManagerOptions` and nested command parsing, history, formatting, and execution option objects.
- Added `OptionValueStyle` for future command option parsing syntax.
- Added resolved `ConsoleManager.Options` snapshot access.
- Added bounded `ConsoleHistory` and `CommandHistory` retention.
- Added public command input history add/clear behavior.
- Added `CommandHistoryDuplicatePolicy` for command input duplicate handling.
- Added focused user-facing guides for `ConsoleManager`, configuration, console history, and command history.
- Added immutable command registration definitions through `CommandBuilder`.
- Added manager-owned command registration with read-only registry inspection.
- Added focused user-facing command registration documentation.
- Added configurable `CommandParsingOptions.FlagAndOptionPrefix` for future flag/option parsing.
- Added side-effect-free `ConsoleManager.ParseCommand(...)`.
- Added structured command parse result, error, and bound command types.
- Added typed command value binding for required positional arguments, flags, and options.
- Added focused user-facing command parsing documentation.
- Added stateless command autocomplete through `ConsoleManager.GetAutocomplete(...)`.
- Added command autocomplete result, candidate, candidate kind, and context types.
- Added `CommandAutocompleteResult.Apply(...)` helpers.
- Added `CommandAutocompleteOptions` and `CommandPathCompletionMode` for full-path or dot-segment path completion.
- Added command value candidate providers through `CommandBuilder.ValueCandidates(...)`.
- Added focused user-facing command autocomplete documentation.
- Added configurable boolean literal aliases for boolean option parsing.
- Added package-wide `ConsoleFailure`, failure codes, failure kinds, and project exception types.
- Added focused user-facing failure handling documentation.
- Added `CommandResult` success/failure result data with ordered command outputs.
- Added semantic `CommandOutput`, inline/block output, output segments, default style inheritance, and plain text derivation.
- Added focused user-facing command result and output documentation.
- Added package-wide `ConsoleText`, opt-in markup parsing, formatting models, markup profiles, themes, styles, colors, and formatter abstractions.
- Added plain text, Unity rich text, and Godot BBCode console text formatters.
- Added `ConsoleManager.CreateText(...)` for manager-owned formatting-aware text creation.
- Added `CommandExecutionOptions` for future command input echo behavior.
- Added command definition metadata for input echo overrides and static success output.
- Added focused user-facing formatting documentation.

### Changed

- Renamed the root console coordinator to `ConsoleManager`.
- `ConsoleManager` can now be constructed with options while keeping `new ConsoleManager()` as the default path.
- `HistoryOptions` now controls active console and command input history capacity.
- Logging and command input recording now use manager-centered public methods.
- Command flag and option schema names are now defined without their command-line prefix.
- Command parsing options now control active parse behavior instead of being configuration placeholders only.
- Failure and exception handling now uses the shared package model planned before command output/execution work.
- Command parsing failures now use package-wide `ConsoleFailure` instead of parse-specific error types.
- `CommandOutputEntry` now stores semantic `CommandOutput` instead of message text plus `CommandOutputLevel`.
- `CommandOutput` now uses package-wide `ConsoleText` internally.
- `LogEntry` and `CommandInputEntry` now expose semantic `ConsoleText` while preserving plain text properties.
- Formatting is now configured through `ConsoleFormattingOptions`, disabled by default, with Unity rich text and Godot BBCode presets.
- Markup parsing is now manager-owned through normal string APIs on `ConsoleManager` and command output.
- Formatting enabled state is now derived from configured formatting options instead of being manually toggled.
- Formatting-aware strings now stay literal when formatting is disabled and parse known markup tags when formatting is enabled.
- Markup parsing is now lenient: unknown tags and ordinary angle-bracket text stay literal, while malformed known tags still throw `FormatException`.
- Command output builder factories are now `CommandOutput.BuildInline(...)` and `CommandOutput.BuildBlock(...)`.

### Removed

- Removed `CommandOutputLevel` in favor of semantic command output style IDs.
- Removed explicit markup-specific public helpers in favor of formatting-aware strings.
