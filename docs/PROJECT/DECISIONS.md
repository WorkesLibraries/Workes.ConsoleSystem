# DECISIONS

## Purpose

Record long-lived design rationale for Workes.ConsoleSystem. These decisions guide implementation; they do not prove that a behavior or API has already been implemented.

## Migration From Workes.ConsoleSystem.Old

The old package's decision history was reviewed when this fresh package was started. IDs below are retained where a decision is carried forward so references remain traceable. The old file remains the complete historical record until migration is finished.

Some old decisions are intentionally not carried forward as current guidance:

- Old D-003 described a deliberately non-functional command scaffold; it was superseded by the command model and execution decisions that followed.
- Old D-023 and D-025 described package-owned formatting/markup behavior; old D-030 superseded them with semantic text and UI-owned rendering.
- Old D-029 was tied to the old package's 0.1.0 release timing and does not establish the release scope or timing of this fresh package.
- Old D-002 and D-013 retain useful console behavior intent, but their storage implementation is replaced by D-032 below.

## Accepted Decisions Carried Forward

### D-001: One Logical Console Manager, Not A Singleton

#### Context

Hosts may need separate consoles, tests need isolated state, and static state makes lifecycle and ownership unclear.

#### Decision

Use one explicit manager instance per logical console. Do not enforce singleton use or rely on global mutable state.

#### Reasoning

Explicit instances are easier to test, compose, and own across host lifecycles.

#### Consequences

The manager owns or coordinates the console workflows associated with one console instance. Host applications decide its lifetime.

### D-005: Commands Use One Schema Model

#### Context

Separate command types for simple, positional, flag, and option forms duplicate registration and execution concepts.

#### Decision

Use one command-definition model capable of representing command paths, positional arguments, flags, options, aliases, and validation metadata.

#### Reasoning

One schema keeps registration, parsing, validation, autocomplete, and execution consistent while allowing simple commands to remain simple.

#### Consequences

Avoid command-type hierarchies split by parameterization style. Keep the command schema immutable after it is built and registered.

### D-006: Complex Commands Prefer Typed State

#### Context

Commands with several inputs need a clear contract that avoids magic strings and loosely typed parameter handles.

#### Decision

Simple commands need no state type. Complex commands should bind input to small immutable typed state records.

#### Reasoning

Typed state makes command inputs, constraints, and handler behavior explicit and compiler-checked.

#### Consequences

Keep typed state as the normal advanced command path. Any dynamic escape hatch is secondary and requires a concrete use case.

### D-007: Console Entries Are Extensible And Type-Specific

#### Context

The visible stream contains logs, command input, command output, failures, and potentially host-defined console content. These do not all share log semantics.

#### Decision

Represent each console event with a type-specific entry. Keep log severity on log entries and allow host-defined entry types.

#### Reasoning

Typed entries let UIs choose behavior by entry meaning without forcing unrelated metadata onto every item.

#### Consequences

Console entry payloads and their public contract belong to ConsoleSystem. Under D-032, an internal adapter maps them to ContentSystem's timestamped entry contract; consumers do not need to reference ContentSystem to create, record, or read console entries.

### D-008: Public Root Is ConsoleManager

#### Context

Users need a clear place to access commands, visible history, command input history, and logging.

#### Decision

Keep `ConsoleManager` as the normal package root and coordination point.

#### Reasoning

It makes ownership and the common workflow easy to discover without requiring global access.

#### Consequences

Lower-level types may remain available for focused workflows, but ordinary usage should not require consumers to coordinate unrelated subsystems manually.

### D-009: Manager Options Have Complete Defaults And Snapshot Semantics

#### Context

Configuration should be easy for default users while allowing a caller to change one setting safely.

#### Decision

Provide a default manager construction path and an options path with complete defaults. Snapshot option values when constructing the manager.

#### Reasoning

This supports simple startup and prevents caller mutations from silently changing a live console.

#### Consequences

Changing one option must not require specifying every option. Validate setup values before the manager accepts them.

### D-010: Command Parsing Is Configurable With A Predictable Default

#### Context

Different hosts may want different option syntax, but a command line needs unsurprising default behavior.

#### Decision

Keep parsing configurable while preferring a space-separated, quoted-string-aware, case-insensitive default with a configurable flag/option prefix.

#### Reasoning

The default works for common game-console use and avoids tying command definitions to a particular UI.

#### Consequences

Keep syntax decisions in command parsing options. Do not make engine or UI behavior part of the parser.

### D-011: Commands Return Results And Semantic Output

#### Context

Command handlers need to report output and expected failures in a way the manager can record consistently.

#### Decision

Handlers return a command result containing zero or more output values and, when needed, structured failure data.

#### Reasoning

Returned results keep side effects and visible history ordering under manager coordination.

#### Consequences

Prefer result-based output over hidden imperative output sinks. Preserve output meaning for the host UI.

### D-012: Command Output Uses Semantic Text

#### Context

Plain text is sufficient for many outputs, while some output benefits from machine-readable semantic spans.

#### Decision

Support plain content and semantic text segments without assigning rendering or styling responsibilities to ConsoleSystem.

#### Reasoning

Semantic data is useful across host UIs and does not constrain them to one markup or theme system.

#### Consequences

Keep rendering, theme mapping, rich-text conversion, and controls in host applications or separate adapters.

### D-014: Options Are Setup Objects, Not Live Shared State

#### Context

Mutable object-initializer configuration is convenient, but retaining caller-owned mutable options creates surprising runtime changes.

#### Decision

Treat options as setup objects and copy their values into manager-owned configuration during construction.

#### Reasoning

The manager then has a stable configuration snapshot with clear ownership.

#### Consequences

Expose resolved configuration as read-only or as a defensive snapshot. Do not retain mutable caller options by reference.

### D-015: Command Input History Has Its Own Duplicate Policy

#### Context

Input navigation history is different from visible console output and repeated submissions can make navigation noisy.

#### Decision

Keep a separate command input history with configurable capacity and consecutive-duplicate handling. The default rejects consecutive semantic duplicates while preserving submitted text.

#### Reasoning

The navigation workflow has retention and comparison rules that differ from the visible stream.

#### Consequences

Do not store command navigation as a second view over ContentSystem console entries. Echoed command input, when enabled, remains a visible console entry.

### D-016: Commands Are Built Externally And Registered Through The Manager

#### Context

Command definitions benefit from staged construction and validation before becoming available to runtime parsing.

#### Decision

Build command definitions through a builder and register completed definitions through the console manager.

#### Reasoning

This keeps incomplete schema state out of the active registry and gives registration one validation boundary.

#### Consequences

Keep built definitions immutable. Validate duplicate paths and schema consistency during registration.

### D-017: Flag And Option Names Are Prefix-Free In Schemas

#### Context

The configured command-line prefix may vary by host, while command schemas should remain independent of that syntax choice.

#### Decision

Define flag and option names without their command-line prefix. Apply the configured prefix during parsing and autocomplete.

#### Reasoning

This keeps command definitions portable across parser configurations and avoids embedding syntax in the schema.

#### Consequences

Validate names and aliases consistently, then format them for display or input through the active parsing options.

### D-018: Parsing Is Side-Effect-Free And Returns Structured Results

#### Context

UIs and tools need to inspect incomplete or invalid input without executing commands or changing visible history.

#### Decision

Parsing returns a structured success/failure result and does not invoke handlers, write history, or throw for normal user-input mistakes.

#### Reasoning

This makes parsing safe for inspection and keeps user mistakes distinct from programmer misuse.

#### Consequences

Keep normal command submission as a separate manager-owned workflow that invokes parsing before validation and execution.

### D-019: Boolean Option Literals Are Configurable

#### Context

Hosts may need localized or domain-specific Boolean literals, but implicit permissive parsing is difficult to predict.

#### Decision

Use explicit configured true/false literal aliases. Keep `true` and `false` as the unsurprising defaults; aliases are opt-in.

#### Reasoning

Explicit literals make parsing deterministic while preserving customization.

#### Consequences

Do not rely on loosely defined conversions for Boolean options. Invalid configured aliases should be rejected during setup.

### D-020: ConsoleSystem Has A Package-Wide Failure Model

#### Context

Expected command rejection should be handled consistently across parsing, validation, registration, and execution.

#### Decision

Use a structured ConsoleSystem failure model for expected domain rejection. Use standard .NET exceptions for invalid caller arguments and programmer/setup misuse.

#### Reasoning

Consistent failures are easier for callers to inspect and render than unrelated strings or exception types.

#### Consequences

Try APIs expose structured failure information; expected-success wrappers may throw a package exception carrying the same failure.

### D-021: Parse Failures Use ConsoleFailure

#### Context

Invalid user input is expected during interactive use and should not require exception handling as ordinary control flow.

#### Decision

Represent parse failures with the same structured ConsoleSystem failure model used by other expected command rejections.

#### Reasoning

One failure model gives UIs and callers a consistent way to inspect and present rejected input.

#### Consequences

Parsing results expose failure data. Exceptions remain appropriate for invalid API arguments or invalid package setup.

### D-022: Output Meaning Is Not A Log Severity

#### Context

Command output and developer logs have different semantics, even when both appear in the same visible stream.

#### Decision

Represent command output meaning through output kinds and optional semantic text, not by assigning log levels to command results.

#### Reasoning

Keeping these concepts separate prevents log severity from becoming a generic presentation label.

#### Consequences

The host UI may render logs and command outputs differently by their concrete entry and output types.

### D-024: Command Echo Is Manager-Owned And Can Be Overridden

#### Context

Echoed input is useful in an interactive console, but some commands or hosts may want to suppress it.

#### Decision

Let manager execution options define the default echo behavior and allow a command definition to override it. Echo on/off only; styling remains a UI decision.

#### Reasoning

Manager defaults create consistency while per-command overrides cover special cases without requiring UI-specific styling APIs.

#### Consequences

When enabled, echo is recorded as a command-input entry in the shared visible history. The UI identifies its type to render it.

### D-026: Autocomplete Is Stateless And UI-Owned

#### Context

The package understands command schemas, while the host UI owns cursor state, selection, cycling, and display.

#### Decision

Expose autocomplete as a stateless query over input and cursor position. Return candidates and replacement information; leave interaction state to the UI.

#### Reasoning

This lets hosts query on each edit without embedding UI state in the package.

#### Consequences

Autocomplete must not execute commands, write history, or validate command effects. Value suggestions are provided by command authors when needed.

### D-027: Command Validation Runs After Parsing

#### Context

The package must distinguish input that cannot be bound from bound state that violates command rules.

#### Decision

Keep validation as a side-effect-free stage after parsing. Normal execution performs validation automatically before invoking a handler.

#### Reasoning

Separate stages clarify failure causes and allow advanced callers to inspect a parsed command before execution.

#### Consequences

Validation checks option metadata and typed command constraints. It must not invoke handlers or write history.

### D-028: Direct Command Execution Is The Normal Submission Path

#### Context

Making every UI manually parse, validate, execute, and record history is repetitive and risks inconsistent ordering.

#### Decision

Provide a manager-owned direct submission workflow. Keep parse and validation operations as advanced inspection APIs.

#### Reasoning

The manager can consistently coordinate input history, optional echo, validation, handler invocation, output, and failure entries.

#### Consequences

Normal submission returns a structured command result or uses an expected-success throwing wrapper. History writes belong to execution, not parsing or validation.

### D-030: Semantic Text And Custom Entries, Not Package-Owned Formatting

#### Context

Package-owned markup, themes, and engine-specific conversion either constrain host applications or grow into a UI framework.

#### Decision

Keep plain strings, semantic text segments, and custom typed entries in the core. Do not make markup parsing, themes, rich-text rendering, or engine controls part of ConsoleSystem.

#### Reasoning

Hosts already own their rendering technology and visual rules. Semantic data keeps the console expressive without taking over that layer.

#### Consequences

Old formatting decisions D-023 and D-025 are superseded. Future engine adapters may be separate packages. This decision remains active after adopting ContentSystem.

### D-031: History Change Hooks Are Optional And Detailed

#### Context

Reactive UIs benefit from incremental updates, while polling and pull-based reads should remain valid.

#### Decision

Keep synchronous, optional history change events. Console-facing events should provide enough information to update visible rows incrementally, including additions, overflow drops, clears, and retained count where applicable.

#### Reasoning

Detailed notifications improve UI integration without requiring subscription or a framework dependency.

#### Consequences

ContentSystem is the source of committed storage changes under D-032. ConsoleSystem should adapt its notifications to console vocabulary without storing duplicate history state. Preserve no-op event behavior and document that handlers run synchronously.

## New Decision

### D-032: ContentSystem Owns Retained Console Content Storage

#### Context

The old console history combined console-specific entry semantics with generic ordered, bounded content retention. That storage layer is useful to chat, logs, and other systems, so maintaining another implementation in ConsoleSystem would duplicate increasingly capable functionality.

#### Decision

Use Workes.ContentSystem as the authoritative storage foundation for the visible console stream. ConsoleSystem internally adapts its own entries to ContentSystem's entry contract and stores them in a sequence configured with the console's drop-oldest capacity. Keep ConsoleManager and ConsoleHistory as ConsoleSystem-owned APIs; callers must not need to construct, configure, or call ContentSystem managers, sequences, records, entries, or events. Do not maintain a parallel authoritative list. Keep command input navigation history independent.

#### Reasoning

ContentSystem already owns content identity, sequence retention, mutation, change notifications, and related reusable workflows. ConsoleSystem should add console-specific entry types and command behavior rather than duplicate generic content storage.

#### Consequences

ConsoleSystem's public entry contracts must not inherit from or expose ContentSystem contracts. An internal adapter must satisfy ContentSystem's timestamped entry contract and wrap ConsoleSystem-owned entry values. ConsoleHistory projects records back to ConsoleSystem-owned entries/results and translates content changes into ConsoleSystem-owned history events; public signatures must not expose ContentSystem types or IDs. ContentSystem remains unaware of log severity, command schemas, and console UI semantics. Snapshot support for console entries requires explicit snapshot serializers/factories and is not implied until implemented. The new project must add and validate the actual package/project dependency before user-facing docs claim it.

This carries forward the observable intent of old D-002 (one chronological stream for logs, echoed input, command output, failures, and custom entries) and D-013 (configurable bounded drop-oldest retention) while replacing their console-owned storage mechanism. ContentSystem now owns the records and retention behavior.

### D-033: Manager Logging Records Every Supported Level

#### Context

The old logging surface accepts all six log severities and provides no minimum-level filter configuration. Adding a new filter option would change that contract and require a separate decision about whether suppressed entries remain observable elsewhere.

#### Decision

Keep logging manager-centered and record every call at Trace, Debug, Information, Warning, Error, or Critical severity. Support plain strings and semantic `ConsoleText`; do not add package-side minimum-level filtering in this stage.

#### Reasoning

This preserves the old behavior and keeps filtering/rendering decisions with the host application.

#### Consequences

Every manager logging call appends one `LogEntry` to `ConsoleHistory` and the ContentSystem-backed sequence. The host UI may choose which levels to display, but the package does not discard them.

### D-034: Command Input Navigation History Is Separate From Visible History

#### Context

Interactive command editing needs a compact list of submitted strings for navigation. These strings are not themselves visible transcript entries unless command execution later echoes them.

#### Decision

Keep bounded `CommandHistory` independent of the ContentSystem-backed visible `ConsoleHistory`. Record submissions through `ConsoleManager.RecordCommandInput`, preserve the submitted string, ignore blank input, reject consecutive duplicates by trimmed ordinal case-insensitive comparison by default, and allow an explicit duplicate policy override. Keep global echo configuration on `CommandExecutionOptions` and command-specific nullable echo metadata on `CommandEchoInputDefinition`; execution writes echoed `CommandInputEntry` values in Stage 11.

#### Reasoning

Command navigation and visible console content have different data, retention, and presentation needs. Keeping them separate prevents editing history from silently changing the visible transcript.

#### Consequences

Command input retention uses the configured capacity and drops its oldest retained string when full. Stage 5 defines recording, reading, clearing, and echo policy only; it does not execute commands or write echo entries.
