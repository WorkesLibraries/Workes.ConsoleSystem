# Changelog

This file records notable user-facing changes to `Workes.ConsoleSystem`.

## [0.1.0] - Unreleased

### Added

- Initial package scaffold.
- Added `ConsoleManagerOptions` and nested command parsing, history, and presentation option objects.
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

### Changed

- Renamed the root console coordinator to `ConsoleManager`.
- `ConsoleManager` can now be constructed with options while keeping `new ConsoleManager()` as the default path.
- `HistoryOptions` now controls active console and command input history capacity.
- Logging and command input recording now use manager-centered public methods.
- Command flag and option schema names are now defined without their command-line prefix.

