# Changelog

This file records notable user-facing changes to `Workes.ConsoleSystem`.

## [0.1.0] - Unreleased

### Added

- Added the ContentSystem-backed `ConsoleManager` foundation with snapshotted parsing, autocomplete, history, and execution options.
- Added ConsoleSystem-owned typed entries, semantic console text, and an internal adapter for storing entries in ContentSystem.
- Added `ConsoleManager.RecordEntry(IConsoleEntry)` for recording built-in and custom console entries.
- Added the ContentSystem-backed `ConsoleManager.History` facade with configured capacity, detached oldest-first entry snapshots, and clear support.
- Added command output and structured failure value types used by the console entry contracts.

