# API GUIDELINES

## Purpose

Describe the public API design principles for Workes.ConsoleSystem. This is project-control guidance, not a member-by-member API reference.

## Current Status

Published version 0.3.0 implements `ConsoleManager` and its snapshotted options, ConsoleSystem-owned entries and semantic text, an internal ContentSystem adapter, a `ConsoleHistory` read/clear facade, manager-centered logging for all six `LogLevel` values, bounded command input history, command registration, parsing, validation, synchronous execution, stateless autocomplete, and optional history change notifications. The 0.4.0 release target adds opt-in permission and availability checks. Logging accepts strings and semantic `ConsoleText`; every level is recorded without package-side filtering. Consumers use manager methods rather than a separate public logger. `ConsoleManager.RecordEntry(IConsoleEntry)` writes visible entries, `ConsoleManager.History` exposes capacity, detached oldest-first snapshots, and clearing, and command execution records submissions in `CommandHistory`. The guidance below records the package's design rules; public docs must remain aligned with published and release-target APIs.

## API Design Principles

- Keep the package engine-neutral and UI-neutral.
- Use `ConsoleManager` as the explicit root for one console instance; do not introduce a singleton or hidden global state.
- Keep the public workflow manager-centered and convenient for normal users.
- Keep logging methods directly on `ConsoleManager`; use internal helpers only to organize implementation, not as a second public route.
- Keep the core public surface focused on implemented workflows; avoid speculative extension points.
- Preserve the old package's command model and typed console domain concepts unless implementation evidence requires a change.
- Let Workes.ContentSystem own retained content storage and record identity. ConsoleSystem should offer console-appropriate APIs and payloads rather than mirror the content storage API.

## Content And History API

`IConsoleEntry` and all built-in/custom entry contracts belong to ConsoleSystem. They must not inherit from, accept, or return ContentSystem types. ConsoleSystem's internal adapter maps an `IConsoleEntry` and its timestamp to the ContentSystem entry contract when storing it; callers only create ConsoleSystem entries and submit them through ConsoleSystem APIs.

`ConsoleHistory` is a console-facing facade over a ContentSystem sequence, not a second record store. `ConsoleManager.History` is the supported access path; `Entries` returns a detached oldest-first snapshot on every read, and `Clear()` mutates the shared retained history. Keep entry-oriented reads and clear/add workflows in ConsoleSystem terms. Do not accept or return ContentSystem records, IDs, managers, sequence types, or event arguments in the public API. If a future console feature needs identity, define a console-owned identity type.

The normal history retention behavior is bounded drop-oldest. Keep the existing console configuration concept for history capacity and translate it internally to ContentSystem's sequence overflow policy. Do not add custom retention machinery to ConsoleSystem or require callers to configure a ContentSystem sequence.

History change events in published version 0.3.0 translate ContentSystem change events to ConsoleSystem-owned event arguments. Preserve added entries, dropped entries caused by overflow, cleared entries, and retained count. ContentSystem event types must not appear in public event signatures. Pull-based reads remain available and do not depend on subscriptions.

`CommandHistory` is intentionally separate: it stores command input strings for navigation, with its own duplicate policy and capacity. Do not combine it with ContentSystem's visible-entry sequence merely because both are called histories.

## Entry And Presentation API

Use typed console entries for logs, echoed command input, command output, command failures, and host-specific console events. Do not put log severity on every entry type.

Keep simple string logging and output paths simple. Use `ConsoleText` and semantic segments when callers need meaning within a line. Semantic IDs and data describe content; the host application decides appearance and interaction.

Allow custom console entries through a narrow entry contract and a controlled manager/history recording method. Custom entries should not require the package to know their rendering model.

## Command API Direction

Use one command schema model rather than separate public types for each combination of positional arguments, flags, and options.

- Give every input binding a required string ID that names its logical state value; keep that ID distinct from the positional or named input name. Use ordinal ID comparison and bulk `Defaults((id, value), ...)` configuration.
- Permit one optional positional argument at the end. It may share an ID with one option only when both target the same state property and type; reject supplying both sources in one parse.
- Expose resolved logical values by ID and retain argument, flag, and option maps as explicit-source views. Keep conversion, range, allowed-value, and autocomplete metadata on each input binding, then run typed constraints on the final state.

- Simple commands should work without a separate state type.
- Complex commands should prefer small immutable typed state records.
- Required positional arguments represent core command state; flags and named options represent optional state.
- Build immutable definitions before registering them through the manager.
- Keep command path, parsing, binding, validation, execution, and autocomplete concepts separate even when normal submission orchestrates them together.
- Keep handlers synchronous until a real asynchronous workflow is required.
- Return command output and expected command failures through a result object rather than writing through a hidden output sink.
- Construct `CommandResult` through the explicit `Success(...)` and `Failed(...)` factories; do not add equivalent public constructors.

The base command surface, stateless autocomplete, and history events are implemented in published version 0.3.0. Permission and availability APIs are implemented for the 0.4.0 release target. The focused user guides identify release-target APIs; this file describes the principles that should guide later changes.

## Configuration Style

Prefer a no-argument constructor with complete defaults and an options overload for customization. Options objects may be mutable setup objects for object-initializer ergonomics, but the manager should snapshot them at construction so later caller mutations do not change runtime behavior.

Changing one option should not require callers to specify unrelated settings. Validate invalid configuration before accepting it. Keep console history capacity as a console setting even though ContentSystem performs the underlying bounded retention.

## Failure Handling And Validation

- Expected domain or command rejection should be representable as structured failure data.
- Provide `Try...` operations when callers are expected to branch on rejection, with throwing wrappers for expected-success use where appropriate.
- Programmer misuse and invalid setup arguments should use standard .NET exceptions.
- Validate complete operations before committing mutation where practical.
- Parsing, validation, and autocomplete do not mutate history or invoke command handlers. Autocomplete providers are user callbacks and any side effects inside a provider are owned by that application.
- Direct execution should own the normal user-input workflow and write resulting input/output/failure entries in a consistent order.

## Documentation Boundary

Project-control documents may describe planned architecture and decisions, clearly identified as such while implementation is absent. Public README, Quick Start, and focused user guides describe only APIs and workflows verified in the current package. Recheck examples against the fresh implementation before changing the public API documentation.
