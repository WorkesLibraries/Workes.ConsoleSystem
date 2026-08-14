# DECISIONS

## Purpose

Record long-lived architectural and design decisions.

## Usage

Use this file to explain why important choices were made, especially when alternatives were rejected.

## Maintenance

Append new decisions as they are made.

If a decision changes, add a new entry instead of deleting the old one.

## Rules

- Use decision IDs like D-001, D-002, D-003.
- Include context, decision, reasoning, and consequences.
- Prefer append-only history.
- Do not use this as a task list.

## Decision Format

Use this format for new decisions:

### D-001: Decision title

#### Context

What situation, problem, or tradeoff led to this decision?

#### Decision

What did we decide?

#### Reasoning

Why was this chosen?

#### Consequences

What does this make easier, harder, or more constrained?

## Accepted Decisions

### D-001: One Logical Console Manager, Not A Singleton

#### Context

The package needs a normal root object for a running game console, but hosts and tests should not be forced into hidden global state.

#### Decision

`ConsoleManager` represents the normal root object for one running game console. Applications are expected to create one instance during startup and keep it for the game lifetime, but the package does not enforce singleton behavior.

#### Reasoning

This keeps ownership explicit and testable, avoids hidden global state, and leaves room for unusual hosts or tests to create more than one console.

#### Consequences

Consumers are responsible for owning and passing the `ConsoleManager` instance. The package remains easier to test and less coupled to a specific host lifecycle.

### D-002: Console History Is The Shared Chronological Record

#### Context

Logs, command input, command output, and future command failures all need to be visible to console UI consumers in the order they occurred.

#### Decision

All console-facing entries should flow into one shared `ConsoleHistory`.

#### Reasoning

The console and log are not separate systems. UI consumers can render one chronological stream while command behavior remains internally separated.

#### Consequences

Future features should preserve the shared entry stream. Specialized systems may keep their own state, but user-visible console output should still become console history entries.

### D-003: Keep Initial Command System Non-Functional

#### Context

The first package skeleton needs public shape before command behavior is settled.

#### Decision

The initial package includes `CommandSystem`, `Command`, `CommandDefinition`, `CommandContext`, and `CommandResult`, but does not implement registration, parsing, execution, permissions, autocomplete, aliases, arguments, or options.

#### Reasoning

Command behavior has several unsettled design choices. Keeping the first command surface non-functional avoids speculative abstractions before the normal workflow is approved.

#### Consequences

Current public documentation must not imply commands are executable yet. Future command work should first settle registration and execution design before adding behavior.

### D-004: Trello Owns Task Tracking

#### Context

The project originally used a small Markdown board under `docs/project`, but the current project formula uses Trello for task state.

#### Decision

Task state should follow `docs/PROJECT/TRELLO_WORKFLOW.md`. The old Markdown board cards were migrated into Trello during project setup.

#### Reasoning

Trello is now the formula-level source of truth for current work, next work, backlog, bugs, blocked work, and completed work.

#### Consequences

Do not recreate a Markdown task board under `docs/PROJECT`. Assistants should use the mapped Trello board for task state.

### D-005: Commands Use One Schema Model Instead Of Separate Command Types

#### Context

The command system needs to support simple commands, flags, key-value options, positional arguments, aliases, defaults, validation constraints, and autocomplete without forcing separate command types such as non-parameterized commands, flag-parameterized commands, and option-parameterized commands.

#### Decision

Commands should be modeled as one command definition with a path, positional arguments, flags, options, constraints, and an execution handler.

The command input shape should be:

```text
<path> <required positional arguments...> [flags and options...]
```

Positional arguments should represent required core command state. Optional command state should be expressed through named options or flags instead of optional positional arguments.

#### Reasoning

A single schema model allows command creators to mix flags, options, and positional arguments naturally. Required positional arguments keep common commands bearable to type, while named options and flags make optional behavior explicit and easier to validate, document, and autocomplete.

Avoiding optional positional arguments keeps parsing, help text, autocomplete, and user expectations simpler.

#### Consequences

The parser and registration API should support required positional arguments, boolean flags, typed options, aliases for flags/options, defaults for options, and validation constraints over the fully bound command state.

Future command design should not introduce separate public command-type hierarchies for each parameterization style.

### D-006: Complex Commands Prefer Typed State Records

#### Context

The command API needs a clean way for handlers and constraints to access bound command values. String IDs are flexible but can become magic strings. Typed parameter handles avoid strings but make command definitions feel more mutable and less cleanly finalized. Class-per-command state initially felt like ceremony, but small immutable records can represent the command's input contract clearly.

#### Decision

Simple commands should remain simple and should not require a state type.

Complex commands should prefer a typed command state record as the primary clean model. The command builder should bind positional arguments, flags, and options to properties on that record, then execute handlers against a validated immutable state object.

Example intended shape:

```csharp
public sealed record RestartCommand(
    bool IgnorePlayers,
    int DelaySeconds);

commands.Register<RestartCommand>("server.restart")
    .Flag(x => x.IgnorePlayers, "--ignore-players", "-i")
    .Option(x => x.DelaySeconds, "--delay", "-d")
        .Default(0)
        .Range(0, 3600)
    .MutuallyExclusive(x => x.IgnorePlayers, x => x.DelaySeconds)
    .Execute((ctx, state) =>
    {
        // use state.IgnorePlayers and state.DelaySeconds
    });
```

#### Reasoning

Typed state records give the command definition a clear input contract, avoid magic strings, preserve type safety, allow immutable bound state, and keep constraints tied to real properties. They also let a command be fully defined and validated as a complete schema before use.

The small-record cost is acceptable for semi-complex commands because the record documents the command's public input model.

#### Consequences

The future command registration API should make the typed state-record path feel first-class. Dynamic/string-based command state may exist later as an advanced escape hatch, but it should not be the primary design target.

The command system should freeze or otherwise prevent mutation of command definitions once registration is complete.

### D-007: Console Entries Are Extensible And Type-Specific

#### Context

The console history needs to contain logs, command input, command output, command failures, and possible game-specific entries. Logging severity is useful for system log messages, but not every console entry is a log.

#### Decision

Console history should store extensible `IConsoleEntry` objects. Entries should carry their own timestamp and stable entry type/kind.

Built-in entry types should cover at least system/log messages, command input, command output, and command errors or failures. Users should be able to define custom entry types for game-specific console events.

Log severity should belong to `LogEntry`, not to all console entries. Command output should use command-output-specific metadata such as output level/type instead of being forced into log severity.

#### Reasoning

Keeping severity on log entries preserves a clean model: logs have log levels, command output has command output semantics, and custom entries can expose their own metadata. Storing all of them as `IConsoleEntry` values preserves one chronological console stream while allowing extensibility.

#### Consequences

The package should not make `LogLevel` a required member of `IConsoleEntry`. Future sinks/routing can be added later if needed, but the first design should focus on extensible entry objects and the shared chronological history.

### D-008: Public Root Is ConsoleManager

#### Context

`ConsoleManager` is the package root because it coordinates commands, logging, history, options, output formatting, and future console services rather than representing a UI console directly.

#### Decision

The public root object should be named `ConsoleManager`.

`ConsoleManager` remains an owned root object, not a singleton. Applications are expected to create and hold one manager for the normal console lifetime, but the package should not enforce global state.

#### Reasoning

`ConsoleManager` better communicates coordination and configuration responsibilities. It avoids implying that the package includes a rendered console UI, which remains outside package scope.

#### Consequences

Documentation and examples should use `ConsoleManager` when describing the public root object.

### D-009: ConsoleManager Uses Options With Complete Defaults

#### Context

The package needs configuration for parsing preferences, history limits, themes, and future behavior while staying easy to instantiate for the default case.

#### Decision

`ConsoleManager` should accept an optional options object. Every option should have a default, so users can pass no options or pass an options object that overrides only the values they care about.

Initial option areas should include command parsing, history behavior, and presentation/theme behavior.

#### Reasoning

This follows the intended pattern from related packages: root construction stays simple, advanced behavior is configurable, and changing one preference does not require copying every default value.

#### Consequences

Options classes should be designed as additive configuration surfaces. Avoid requiring callers to fully populate nested option objects.

### D-010: Command Parsing Is Configurable But Space-Separated By Default

#### Context

Different developers prefer different command syntaxes. The package should support common syntaxes without forcing one style everywhere.

#### Decision

Command option value syntax should be configured by an `OptionValueStyle`-like setting:

- `SpaceSeparated`: `--delay 10`
- `EqualSeparated`: `--delay=10`
- `AnySeparated`: both forms

The default should be `SpaceSeparated`.

Command paths, flags, and options should be case-insensitive by default. Quoted strings should be supported. Quotes should always work for string values, and command schema should be able to require quotes for string arguments/options when the value is expected to contain spaces.

Flags and options should be able to appear in any order after the command path and required positional arguments. If an ordering option is added, strict mode should use the schema/builder order.

#### Reasoning

Space-separated options are readable and familiar for game-console style commands. Case-insensitive matching is friendlier for interactive use. Quoted strings are required for natural text values such as full names, messages, and labels.

#### Consequences

The parser should separate raw tokenization, command path resolution, argument binding, option/flag binding, and parse error reporting. Parser behavior should read from `ConsoleManager` options instead of being hard-coded.

### D-011: Commands Return CommandResult With Output Entries

#### Context

Command output could be written imperatively during execution or returned as part of the command result. Mixing both as equal primary models would make command behavior harder to reason about and harder to validate.

#### Decision

Command execution should return a `CommandResult`.

`CommandResult` should be able to contain zero, one, or many command output entries. Command output should be represented as entries rather than being written directly through an output sink during execution.

The framework should create command failure entries for parse errors, constraint errors, and execution errors. Command implementations should create intentional output through returned result entries.

#### Reasoning

Returning output makes command execution easier to test, easier to reason about, and easier to convert into console history. It also supports dynamic output such as help text without forcing one output entry per line.

#### Consequences

Avoid making `ctx.Output.Write(...)` the primary output model. A context object may still be useful for execution metadata, services, caller/source information, and future async/cancellation support, but intentional user-visible command output should come from the returned result.

### D-012: Command Output Uses Semantic Content Segments

#### Context

Output needs to support plain text, multi-line blocks, structured command-specific data, and semantic styling without coupling the package to Unity rich text, Godot BBCode, HTML, terminal colors, or any UI framework.

#### Decision

Command output entries should store semantic text content rather than engine-specific formatted strings.

Output content may be inline or block/multi-line. Content should be representable as ordered segments, where each segment contains plain text and optional style information. Output builders such as `CommandOutput.Inline(...)` may accept a default style so unstylized segments inherit an output-level style.

Example intended concept:

```text
Inline(defaultStyle: Success)
  Text("Gave ")
  Value("10", style: Amount, data: amount)
  Text(" ")
  Value("gold", style: Item, data: item)
  Text(" to ")
  Value("Anthony5172", style: Player, data: receiver)
```

Entries should expose plain text as a derived representation, but should preserve semantic segments for renderers that can use them.

#### Reasoning

Plain text alone cannot express that `10` is an amount, `gold` is an item, and `Anthony5172` is a player. Semantic segments let formatters convert output to Unity rich text, Godot BBCode, plain text, or custom UI spans without making the core package engine-specific.

#### Consequences

The package should separate output content from rendering. Themes define style IDs and style values. Formatters/renderers consume semantic content and themes to produce engine-specific strings or UI elements. Building actual Unity/Godot UI remains outside this package.

### D-013: Console History Uses Configurable Drop-Oldest Capacity

#### Context

Console history should not grow without bound by default, but callers should be able to configure the capacity.

#### Decision

History capacity should be configurable through `ConsoleManager` options.

When capacity is reached, adding a new entry should drop the oldest entry so the history behaves like a chronological ring buffer. Public history exposure should remain chronological from oldest retained entry to newest retained entry.

#### Reasoning

Drop-oldest capacity keeps memory bounded and matches the expected behavior for an in-game console history.

#### Consequences

The implementation should avoid exposing storage details that make later capacity policy changes difficult. The first policy should be drop-oldest.

### D-014: ConsoleManager Options Are Mutable Setup Objects With Snapshot Semantics

#### Context

`ConsoleManager` needs a public configuration surface that is easy to use with object initializers, while avoiding surprising runtime behavior if a caller mutates the same options object after manager construction.

#### Decision

`ConsoleManagerOptions` and nested option types are mutable setup objects.

`ConsoleManager` snapshots supplied option values during construction. Null nested option sections resolve to defaults. The manager exposes resolved options for inspection, but callers should not treat that exposed object as live runtime configuration.

#### Reasoning

Mutable setup objects keep the normal .NET options workflow concise and make partial overrides pleasant. Snapshotting keeps manager behavior stable after construction and avoids hidden coupling to caller-owned configuration objects.

#### Consequences

Later systems should read resolved settings from manager-owned configuration instead of retaining caller-owned option references. Runtime configuration changes should be introduced explicitly if they are ever needed, rather than emerging accidentally through mutable options.

### D-015: Command Input History Rejects Consecutive Semantic Duplicates By Default

#### Context

Command input history exists primarily to support UI navigation through submitted command strings. Retaining every submission is faithful, but repeated accidental submits of the same command can make navigation noisy.

#### Decision

`CommandHistory` rejects consecutive duplicate inputs by default.

Duplicate comparison trims leading and trailing whitespace and compares case-insensitively. The stored input preserves the original submitted string. Only the newest retained input is considered for duplicate rejection, so the same command can appear again later after another command is submitted.

The behavior is configurable through `CommandHistoryDuplicatePolicy`. The initial policies are `Allow` and `RejectConsecutive`.

#### Reasoning

This keeps command navigation useful for common game-console workflows without treating semantically distinct command strings as the same command. Preserving the original string avoids surprising users who expect history to recall what they actually typed.

#### Consequences

Blank command input is ignored. Consecutive duplicates return `false` from `ConsoleManager.RecordCommandInput(...)`. More advanced semantic comparison, command-aware normalization, or history search should be explicit future features rather than hidden behavior.

### D-016: Commands Are Built Externally And Registered Through ConsoleManager

#### Context

The command system needs validated immutable command definitions, but duplicate path validation depends on the manager's existing registry and parsing options. The root object should also be the main interaction surface for normal consumers.

#### Decision

Commands are created with `CommandBuilder`, committed with `Build()`, and registered through `ConsoleManager.RegisterCommand(...)` or `ConsoleManager.RegisterCommands(...)`.

`CommandSystem` remains public as an inspectable read-only registry. Registration is owned by `ConsoleManager`.

Logging and command input recording should also prefer manager methods such as `LogInformation(...)` and `RecordCommandInput(...)`.

#### Reasoning

Creating command definitions outside the manager keeps command schema construction testable and reusable. Registering through `ConsoleManager` lets the package validate against existing commands and manager-specific options. Making the manager the normal interaction surface keeps common usage coherent.

#### Consequences

Direct subsystem mutation should stay internal where possible. Future parsing and execution should use the manager-owned registry rather than allowing separate mutable command-system entrypoints. Batch command registration should remain atomic so setup failures do not leave partial command state.

### D-017: Flag And Option Schema Names Are Prefix-Free

#### Context

Command flags and options are typed by users with a prefix such as `--`, but storing that prefix inside every command schema makes definitions noisier and harder to adapt for projects that prefer a different prefix.

#### Decision

Flag and option schema names should be defined without their command-line prefix.

The prefix is configured through `CommandParsingOptions.FlagAndOptionPrefix`, which defaults to `--`. The parser will apply the configured prefix later when matching command input.

#### Reasoning

Prefix-free schema names keep command definitions focused on logical command vocabulary. A configurable prefix lets callers use styles such as `-delay` or another project-specific prefix when their command set can safely support it.

#### Consequences

Command registration should reject flag or option names that already include the manager's configured prefix. User-facing docs should show schema names such as `delay` and input examples such as `--delay 10`.

### D-018: Parsing Is Side-Effect-Free And Returns Structured Results

#### Context

The package needs command input validation and value binding before command execution is implemented. UI code and tests should be able to ask whether an input is valid without causing history writes, command output, or handler side effects.

#### Decision

`ConsoleManager.ParseCommand(string input)` parses registered commands and returns `CommandParseResult`.

Successful parse results expose the matched command definition, typed state object when present, and bound argument, flag, and option values. Failed parse results expose a stable `CommandParseErrorCode` and a human-readable message.

Parsing does not write to `ConsoleHistory`, does not write to `CommandHistory`, does not evaluate constraints, and does not invoke stored command handlers.

#### Reasoning

A side-effect-free parse API is useful for validation, UI feedback, tests, and future autocomplete/execution workflows. Structured results are easier for UIs to consume than exceptions or plain strings.

#### Consequences

Command execution must explicitly decide when to record command input, command failures, and command output. Parser behavior should remain deterministic and driven by `ConsoleManagerOptions`.
