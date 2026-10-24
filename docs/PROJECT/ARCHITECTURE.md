# ARCHITECTURE

## Purpose

Describe the intended internal structure of Workes.ConsoleSystem and its relationship to Workes.ContentSystem. This is project-control documentation, not a user manual.

## Current State And Target

The published 0.5.0 package includes manager configuration, ContentSystem-backed visible history, typed entries and semantic text, manager-centered logging, command input history, command registration, parsing, validation, synchronous command execution, stateless autocomplete, optional history change events, command permission and availability checks, and the redesigned logical command value model. The current repository source additionally includes a configurable help command for a future release. `ConsoleManager` exposes six logging levels with string and semantic-text overloads; all calls append typed `LogEntry` values to shared history. An internal `ConsoleLog` helper creates those entries through the same history path and is not public. `CommandHistory` separately retains submitted strings for UI navigation and raises optional change notifications. This document records the architecture carried forward from Workes.ConsoleSystem.Old and distinguishes the published package from current source and planned work.

The public console workflows and overall design philosophy should remain close to the old package. The principal architectural change is that console-visible retained content is stored through Workes.ContentSystem instead of a second console-owned history collection.

## Architectural Boundary

Workes.ContentSystem is the reusable content storage foundation. It owns content structures, record identity, retention, ordering, mutation, queries, snapshots, and content-level change notifications. Workes.ConsoleSystem owns console-specific concepts and orchestration: log severity, command input/output/failure entries, semantic console text, command registration and execution, command history, and the manager-centered console API.

ConsoleSystem should depend on ContentSystem rather than copy its sequence/history storage responsibilities. This dependency is an implementation detail: public consumers use ConsoleSystem managers, entry types, text, results, and history APIs, and do not construct or call ContentSystem managers, sequences, records, or entries. ContentSystem should remain unaware of console commands, log levels, console UI conventions, and console-specific entry kinds.

## Main Components

The following components are implemented in the 0.5.0 package unless explicitly marked as future work.

- `ConsoleManager` is the root for one console instance. It coordinates console history, command input history, logging, and command workflows. It is not a singleton and does not rely on global state.
- The manager accepts an `IConsolePermissionProvider` associated with its caller. The provider is an injected runtime service, while configuration snapshots continue to contain settings only.
- `ConsoleHistory` is the console-facing history API. It preserves the old package's convenient entry-oriented view and operations while delegating retained storage to a ContentSystem sequence manager. It translates ContentSystem change notifications into ConsoleSystem-owned `Changed` event payloads. It should not maintain a second authoritative list or expose ContentSystem record types.
- `IConsoleEntry` is a ConsoleSystem-owned contract. It must not inherit from `IContentEntry` or require consumers to reference ContentSystem. An internal adapter maps console entries and timestamps to ContentSystem's entry contract.
- `LogEntry`, `CommandInputEntry`, `CommandOutputEntry`, `CommandFailureEntry`, and host-defined entries remain ConsoleSystem domain payloads. Log level belongs to log entries, not every content record.
- `ConsoleManager` is the public logging surface. Its internal `ConsoleLog` helper creates typed log entries and appends them to the shared console history without exposing a separate logger object to consumers.
- `CommandHistory` stores submitted command strings for UI navigation. It remains separate from the visible console stream because it has different retention and duplicate-handling semantics.
- `CommandEchoInputDefinition` carries a nullable command-level echo override: `null` inherits the manager's `CommandExecutionOptions.EchoInput` setting. Synchronous execution applies that policy and records visible input entries when echo is enabled.
- `CommandSystem` owns the command definitions used by parsing, validation, autocomplete, and execution.
- `HelpCommandBuilder` creates opt-in help configuration. `ConsoleManager.RegisterHelpCommand` turns it into a normal registered command whose handler reads that manager's live command registry and evaluates access through the manager's caller provider.
- `ConsoleText`, `ConsoleTextSegment`, and `ConsoleTextBuilder` represent plain or semantically structured text without owning rendering or styling.

## ContentSystem Integration

The normal console history uses a ContentSystem sequence with drop-oldest retention. The console's configured history capacity maps to that sequence's capacity. Internally, an adapter wraps console entries for storage; ContentSystem records and IDs stay private to the implementation. `ConsoleManager.RecordEntry(IConsoleEntry)` writes custom or built-in entries, and `ConsoleManager.History` exposes configured capacity, detached oldest-first `Entries` snapshots, and `Clear()`. Manager logging methods use the internal `ConsoleLog` helper to create `LogEntry` values and submit them through the same ConsoleHistory write path.

The intended flow is:

```text
host application or command execution
-> ConsoleManager / internal ConsoleLog / ConsoleHistory
-> ConsoleSystem entry and text types
-> internal ContentSystem adapter and sequence
-> ConsoleSystem history projection
-> host consumer
```

`ConsoleHistory` projects stored records to ConsoleSystem-owned entries or console-specific history results. Its public signatures must not accept or return `ContentEntryRecord`, `IContentEntry`, sequence managers, or ContentSystem event types. It maps ContentSystem add/overflow and clear notifications to `ConsoleHistoryChangedEventArgs`. Keep ContentSystem identity internal unless a separate console-owned identity is justified by a real use case.

ContentSystem change notifications are the source of truth for the 0.3.0 console history events. The console layer translates them into console vocabulary, including overflow drops and clears. The published 0.2.0 package has no history-change event; a UI using that package can use pull-based reads through `History.Entries` and clear through `History.Clear()`.

Snapshots are an available future integration point, not a required first implementation feature. Console entry snapshot support must be explicitly provided and registered before console histories can be round-tripped through ContentSystem snapshots. Do not claim persistence support until that work exists.

## Command And Presentation Flow

Commands use one schema model built from logical value IDs and input bindings. Required positional arguments, flags, and named options all bind values into one typed state; an ID can be shared only between one optional trailing positional argument and one option targeting the same property and type. Resolved values come from explicit input, then a bulk ID default, then `default(T)`. Simple commands should not need a state type, while complex commands should prefer small immutable typed state records.

Normal command submission is manager-owned and runs parsing, access checks, validation, handler invocation, and output/failure recording as one workflow. Parsing, validation, and autocomplete are also available as side-effect-free inspection operations, although autocomplete may invoke the permission provider and availability checks to filter command paths. Permission and availability checks were introduced in 0.4.0. Expected command rejection is represented through the package's structured failure/result model; programmer and setup misuse use standard .NET exceptions.

The parser and autocomplete share `CommandTokenizer`. Parsing rejects unclosed quoted input; autocomplete accepts it while the user is editing. Autocomplete reads registered command definitions, returns candidates and replacement ranges, and invokes optional synchronous value providers without writing history or executing commands.

The optional help command remains manager-owned. Its formatters return ConsoleSystem output lines and receive command definitions, a read-only matched-command list, query text, and access status. A policy controls whether blocked commands are hidden, formatted, or replaced with response lines. The default hides blocked commands.

`BoundCommand.Values` is the resolved logical-value view keyed by ID. Its `Arguments`, `Flags`, and `Options` maps retain only explicitly supplied source bindings. Binding-level rules and autocomplete belong to the specific positional or option binding; typed state constraints see the final resolved state. Optional positional input is limited to one final argument so it does not make later positional interpretation ambiguous.

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
