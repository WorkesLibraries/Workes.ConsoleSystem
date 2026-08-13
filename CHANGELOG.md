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

### Changed

- Renamed the root console coordinator to `ConsoleManager`.
- `ConsoleManager` can now be constructed with options while keeping `new ConsoleManager()` as the default path.
- `HistoryOptions` now controls active console and command input history capacity.

