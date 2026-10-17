# ARCHITECTURE

## Purpose

Describe the intended internal structure of Workes.ConsoleSystem and its relationship to Workes.ContentSystem. This is project-control documentation, not a user manual.

## Current State And Target

The fresh Workes.ConsoleSystem project now has the Stage 1 manager/options foundation, the Stage 2 entry, semantic-text, and internal storage-adapter foundation, and the Stage 3 `ConsoleHistory` read/clear facade. `ConsoleManager.History` exposes oldest-first detached entry snapshots and clears the ContentSystem-backed sequence. History change events, the logging facade, and command workflows remain future implementation work. This document records the architecture for carrying useful parts of Workes.ConsoleSystem.Old forward; later APIs remain intended design until implemented.

The public console workflows and overall design philosophy should remain close to the old package. The principal architectural change is that console-visible retained content is stored through Workes.ContentSystem instead of a second console-owned history collection.

## Architectural Boundary

Workes.ContentSystem is the reusable content storage foundation. It owns content structures, record identity, retention, ordering, mutation, queries, snapshots, and content-level change notifications. Workes.ConsoleSystem owns console-specific concepts and orchestration: log severity, command input/output/failure entries, semantic console text, command registration and execution, command history, and the manager-centered console API.

ConsoleSystem should depend on ContentSystem rather than copy its sequence/history storage responsibilities. This dependency is an implementation detail: public consumers use ConsoleSystem managers, entry types, text, results, and history APIs, and do not construct or call ContentSystem managers, sequences, records, or entries. ContentSystem should remain unaware of console commands, log levels, console UI conventions, and console-specific entry kinds.

## Main Components

The following are architectural responsibilities and intended components; names and signatures remain subject to implementation review.

- `ConsoleManager` is the root for one console instance. It coordinates console history, command input history, logging, and command workflows. It is not a singleton and does not rely on global state.
- `ConsoleHistory` is the console-facing history API. It preserves the old package's convenient entry-oriented view and operations while delegating retained storage to a ContentSystem sequence manager. It should not maintain a second authoritative list or expose ContentSystem record types.
- `IConsoleEntry` is a ConsoleSystem-owned contract. It must not inherit from `IContentEntry` or require consumers to reference ContentSystem. An internal adapter maps console entries and timestamps to ContentSystem's entry contract.
- `LogEntry`, `CommandInputEntry`, `CommandOutputEntry`, `CommandFailureEntry`, and host-defined entries remain ConsoleSystem domain payloads. Log level belongs to log entries, not every content record.
- `ConsoleLog` is the developer-facing logging facade. It creates the appropriate typed log entry and appends it to the shared console history.
- `CommandHistory` stores submitted command strings for UI navigation. It remains separate from the visible console stream because it has different retention and duplicate-handling semantics.
- `CommandSystem` owns the command definitions used by parsing, validation, autocomplete, and execution.
- `ConsoleText`, `ConsoleTextSegment`, and `ConsoleTextBuilder` represent plain or semantically structured text without owning rendering or styling.

## ContentSystem Integration

The normal console history uses a ContentSystem sequence with drop-oldest retention. The console's configured history capacity maps to that sequence's capacity. Internally, an adapter wraps console entries for storage; ContentSystem records and IDs stay private to the implementation. `ConsoleManager.RecordEntry(IConsoleEntry)` writes entries, and `ConsoleManager.History` exposes configured capacity, detached oldest-first `Entries` snapshots, and `Clear()`.

The intended flow is:

```text
host application or command execution
-> ConsoleManager / ConsoleLog / ConsoleHistory
-> ConsoleSystem entry and text types
-> internal ContentSystem adapter and sequence
-> ConsoleSystem history projection
-> host consumer
```

`ConsoleHistory` projects stored records to ConsoleSystem-owned entries or console-specific history results. Its public signatures must not accept or return `ContentEntryRecord`, `IContentEntry`, sequence managers, or ContentSystem event types. Keep ContentSystem identity internal unless a separate console-owned identity is justified by a real use case.

ContentSystem change notifications are the source of truth for committed history mutations. The console layer translates them into its established history-change vocabulary where compatibility and usability require it. In particular, overflow removals should be presented as dropped console entries, and clearing should identify the entries that were cleared. A console UI may continue to use pull-based reads; subscribing to events remains optional.

Snapshots are an available future integration point, not a required first implementation feature. Console entry snapshot support must be explicitly provided and registered before console histories can be round-tripped through ContentSystem snapshots. Do not claim persistence support until that work exists.

## Command And Presentation Flow

Commands use one schema model. Required positional arguments represent core input; flags and named options represent optional state. Simple commands should not need a state type, while complex commands should prefer small immutable typed state records.

Normal command submission is manager-owned and runs parsing, validation, handler invocation, and output/failure recording as one workflow. Parsing, validation, and autocomplete also remain available as side-effect-free inspection operations. Expected command rejection is represented through the package's structured failure/result model; programmer and setup misuse use standard .NET exceptions.

Console entries and semantic text remain engine-neutral. The host UI chooses how to render entry types and semantic segments. ConsoleSystem does not own a game engine, UI framework, markup language, theme, or rich-text conversion.

## Important Constraints

- Preserve the old package's manager-centered, engine-neutral, UI-owned design.
- Keep ContentSystem as the sole authoritative owner of retained console content records.
- Keep command input navigation history separate from visible console history.
- Keep console-specific entry meaning and log severity out of ContentSystem.
- Do not copy ContentSystem structure, manager, identity, event, query, or snapshot implementations into ConsoleSystem.
- Keep history change hooks optional and synchronous unless a later concrete requirement changes that contract.
- Keep package documentation aligned with APIs that are actually implemented in the fresh checkout.
- Keep the wrapper boundary strict: public constructors, methods, events, and returned values must use ConsoleSystem or standard .NET types, never ContentSystem types. Translate all entry writes, reads, retention setup, and notifications internally.
- Treat compatibility wrappers and projections as ConsoleSystem API design choices; do not expose ContentSystem details.

## Related Decisions

See [DECISIONS.md](DECISIONS.md), especially the migrated decisions on manager ownership, extensible entries, semantic presentation, history events, and the ContentSystem boundary.
