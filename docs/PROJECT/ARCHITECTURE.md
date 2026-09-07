# ARCHITECTURE

## Purpose

Describe how the project is structured internally and how the main systems relate to each other.

This is a project-control document for maintaining architectural consistency. It is not intended to replace focused user documentation under docs/.

## Usage

Use this file to understand the internal design before making structural changes.

## Maintenance

Update when the internal structure, core abstractions, dependency flow, or major implementation strategy changes.

## Rules

- Explain current architecture, not wishful future architecture.
- Prefer clear sections and diagrams when useful.
- Link to DECISIONS.md when architectural choices need rationale.
- Do not use this as a task list.
- Do not treat this as exhaustive user documentation.

## Current Architecture

Workes.ConsoleSystem is currently a small engine-neutral package centered on `ConsoleManager`.

`ConsoleManager` owns the shared console history, command input history, logging facade, and command registry. It is the normal root object for a host application, but it is not a singleton and does not use static global state.

## Main Components

- `ConsoleManager` coordinates the package-level systems.
- `ConsoleHistory` stores one bounded chronological stream of `IConsoleEntry` values.
- `ConsoleLog` is the developer-facing facade for adding `LogEntry` values to the shared history.
- `CommandHistory` stores bounded submitted command input strings for future UI navigation.
- `CommandSystem` is the read-only command registry used by parsing and future execution.
- Entry types under `Workes.ConsoleSystem.Entries` represent log entries, command input, and command output.
- Presentation types under `Workes.ConsoleSystem.Presentation` represent semantic console text, formatting models, markup profiles, themes, colors, and formatters.

## Root Options

`ConsoleManager` accepts an optional options object with complete defaults. Passing no options produces the normal recommended behavior. Passing an options object with one changed value changes only that value.

Implemented option areas:

- command parsing;
- history capacity/overflow behavior;
- optional formatting subsystem defaults;
- command execution defaults such as future input echo behavior.

Command parsing options should include an option value syntax setting:

```csharp
public enum OptionValueStyle
{
    SpaceSeparated,
    EqualSeparated,
    AnySeparated
}
```

Default command parsing behavior:

- `OptionValueStyle.SpaceSeparated`, supporting `--delay 10`;
- case-insensitive matching for command paths, flags, and options;
- quoted string support;
- flags and options may appear in any order after path and required positional arguments.
- boolean option values accept configured true/false literal aliases, defaulting to `true` and `false`.

If strict flag/option ordering is added, it should use schema/builder order.

Options are mutable setup objects, but `ConsoleManager` snapshots them during construction. Null nested option sections resolve to defaults. Capacity values must be greater than zero.

Flag and option schema names are defined without their command-line prefix. `CommandParsingOptions.FlagAndOptionPrefix` controls the prefix later applied during parsing and defaults to `--`.

History capacity options are active. When either retained history reaches capacity, adding a new item drops the oldest retained item. `CommandHistory` rejects consecutive duplicate command inputs by default using trimmed, case-insensitive comparison while preserving the originally submitted text for retained entries.

Formatting options are disabled by default and become enabled when a formatter-backed setup is configured. When enabled, they carry a `ConsoleFormatModel`, `ConsoleMarkupProfile`, `ConsoleTheme`, and `IConsoleTextFormatter`. Execution options include future command input echo defaults and default echo style.

## Command Model

Command registration and parsing are implemented, while execution is still unimplemented.

A command should be defined by one schema model rather than separate public command types for non-parameterized, flag-parameterized, option-parameterized, or positional commands.

The intended input order is:

```text
<path> <required positional arguments...> [flags and options...]
```

The command schema should support:

- command paths such as `noclip`, `server.restart`, or `player.give`;
- required positional arguments for core command state;
- boolean flags for modifiers;
- typed key-value options for named configurable state;
- aliases for flags and options;
- option defaults, ranges, allowed values, and similar validation metadata;
- constraints over the fully bound command state;
- autocomplete for paths, flag names, option names, and command-provided value candidates.

Optional positional arguments should not be part of the first command model. Optional state should be represented with named options or flags.

Simple commands do not require a state type. Semi-complex or complex commands should prefer small immutable typed state records, with schema bindings expressed against record properties. A command definition is created through `CommandBuilder.Build()` and then registered through `ConsoleManager`.

The preferred registration style is explicit fluent command creation and manager-owned registration:

```csharp
var restart = new CommandBuilder("server.restart")
    .Flag<RestartCommand>(x => x.IgnorePlayers, "ignore-players", "i")
    .Option<RestartCommand>(x => x.DelaySeconds, "delay", "d")
        .Default(10)
        .Range(0, 3600)
    .Constraint(
        "delay-ignore-players",
        "Ignore players cannot be combined with delayed restart.")
    .Execute<RestartCommand>((ctx, state) =>
    {
        RestartServer(state);
        return new CommandResult();
    })
    .Build();

console.RegisterCommand(restart);
```

Constraints currently carry error messages as metadata. Constraint evaluation is not implemented yet.

Synchronous command handlers should be the default. The design should leave room for intuitive async command handlers later, but the first implementation should prioritize synchronous debug/game-console commands.

Commands should return `CommandResult`. A result can contain zero, one, or many command output objects and can carry a structured `ConsoleFailure` for expected command-level failure. Intentional command output should be returned through the result rather than written through an imperative output sink during execution.

Framework-generated command failure entries should cover parse failures, constraint failures, and execution failures.

The package uses a shared failure and exception model following the `Workes.InventorySystem` pattern: expected domain rejection is structured `ConsoleFailure` data, programmer/setup misuse uses standard .NET exceptions, and expected-success wrappers throw package-owned exceptions carrying the same structured failure.

## Presentation Model

Console entries remain engine-neutral. Console-visible text should use `ConsoleText` so formatting can be applied by renderers without storing Unity rich text, Godot BBCode, HTML, or terminal-specific markup as the source of truth.

Console text supports:

- plain text derivation;
- semantic style IDs;
- manager-owned lenient nested markup parsing for formatting-aware strings when formatting is enabled;
- direct color/bold/italic/underline markup;
- optional structured segment data when useful.

Conceptual output flow:

```text
CommandResult
-> CommandOutputEntry
-> ConsoleText
-> optional formatting context
-> plain text, Unity rich text, Godot BBCode, terminal output, or custom UI spans
```

The optional formatting subsystem is made of a model, markup profile, theme, and formatter. The model defines available formatting attributes. The markup profile maps known tags such as `<color=#4ade80>` or `<b>` to those attributes. Unknown tags and ordinary angle-bracket text stay literal. Themes map semantic style IDs such as `Information`, `Warning`, `Error`, `Success`, `Amount`, `Item`, or `Player` to style values. Formatters decide how those style values become a string or UI representation.

Actual engine UI rendering remains outside this package.

## Data Flow / Control Flow

Log calls flow through `ConsoleManager.Log` into `ConsoleLog`, which appends `LogEntry` instances to `ConsoleManager.History`.

Console UI code is expected to read `ConsoleManager.History.Entries` and render entries according to their concrete type.

Console UI code may use `ConsoleManager.RecordCommandInput(...)` to retain submitted command input strings for navigation. Command input history is separate from the shared console entry stream until command execution is implemented.

Command registration, command input parsing, structured failures, semantic command output, and optional package-wide formatting are implemented. Execution, permissions, constraint evaluation, automatic output history writes, and autocomplete are not part of the implemented flow yet.

The current parse flow is:

```text
raw input
-> tokenize with quoted-string support
-> resolve command path
-> bind required positional arguments
-> bind flags and options
-> create typed command state through constructor binding
-> return CommandParseResult with BoundCommand or ConsoleFailure
```

Parsing has no history side effects and does not invoke stored handlers.

The planned command execution flow is:

```text
raw input
-> parse path, required positional arguments, flags, and options
-> resolve command definition
-> bind values into a typed command state record or simple command context
-> run schema validation and constraints
-> execute handler
-> receive CommandResult
-> append command input, output, and failure entries to console history
```

Autocomplete should be schema-driven where possible. Command path, flag name, and option name completion should come from registered command definitions. Positional argument values and option values should require command-provided candidate functions.

## Important Constraints

- Keep ownership explicit and testable.
- Keep UI and engine dependencies outside this package.
- Preserve one shared chronological console history for logs, command input, command output, and future command failures.
- Keep simple commands simple.
- Prefer typed state records for complex commands rather than string IDs or mutable parameter handles as the primary model.
- Do not introduce separate public command-type hierarchies for each parameterization style.
- Do not force log severity onto every console entry; severity belongs to log entries, while command output uses output kind and semantic style IDs.
- Do not hard-code engine-specific text styling into entries.
- Prefer returned command output entries over imperative output writes during command execution.
