# API GUIDELINES

## Purpose

Describe the design philosophy and conventions for the package's public API.

This is a project-control document for keeping the public API consistent. It is not intended to be an exhaustive public API reference.

## Usage

Use this file before changing public-facing types, method names, configuration flows, extension points, or usage patterns.

## Maintenance

Update when the project's API design principles, naming conventions, consistency rules, or overall API philosophy changes.

## Rules

- Document API design intent, not every public member.
- Separate stable API principles from experimental ideas.
- Include short examples of preferred API shape when useful.
- Keep README examples aligned with the normal workflow and explicit package-version installation style.
- Do not document private implementation details here.
- Do not use this as a task list.

## API Design Principles

- Keep the package engine-neutral and UI-neutral.
- Prefer explicit ownership over global/static access.
- Keep the public surface small until workflows are proven.
- Separate implemented behavior from future placeholders in names, docs, and examples.
- Favor simple immutable entry models for console history.
- Keep simple command registration simple, while letting complex commands opt into an explicit typed state model.

## Naming And Structure

- Use `ConsoleManager` as the root object for the package-level console system.
- Keep namespaces grouped by responsibility: `Core`, `History`, `Logging`, `Entries`, `Commands`, and `Presentation`.
- Use clear domain names such as `ConsoleHistory`, `ConsoleLog`, `LogEntry`, and `CommandHistory`.
- Avoid names that imply a game engine, UI framework, storage layer, or singleton lifecycle.

## Configuration Style

Configuration uses constructor options. Avoid hidden global configuration.

`ConsoleManager` should be constructible with no arguments and should also accept an options object with complete defaults:

```csharp
var console = new ConsoleManager();

var configuredConsole = new ConsoleManager(new ConsoleManagerOptions
{
    CommandParsing = new CommandParsingOptions
    {
        OptionValueStyle = OptionValueStyle.AnySeparated
    }
});
```

Changing one option should not require the caller to specify every other option.

Options objects are mutable setup objects for convenient object-initializer use. `ConsoleManager` snapshots the supplied values during construction, so later caller mutations do not alter manager behavior. Null nested option sections resolve to defaults.

History options control active retained history behavior. Histories are bounded and use drop-oldest retention. Command input history rejects consecutive duplicate submissions by default using trimmed, case-insensitive comparison.

Formatting options control the optional console formatting subsystem. Execution options store behavior that later command execution uses, including command input echo defaults.

## History API Direction

`ConsoleHistory` is the shared rendered console entry stream. It should stay controlled by package systems for now, except that callers may inspect `Entries`, inspect `Capacity`, and clear retained entries.

`CommandHistory` is a UI helper for submitted command strings. It should be written through `ConsoleManager.RecordCommandInput(string input)` and may be cleared with `Clear()`. It stores strings only; adding `CommandInputEntry` values to the shared console history belongs to command execution.

## User Documentation Direction

Each implemented first-class public concept should have a focused user-facing guide under `docs/`.

README should position the package and link to guides. Quick Start should remain beginner-first and should not become the exhaustive documentation page. Project-control docs under `docs/PROJECT/` should explain design intent, not replace focused user documentation.

## Command API Direction

Command registration uses one schema model instead of separate command types for non-parameterized, flag-parameterized, option-parameterized, and positional commands.

Simple commands should be possible without a command state type:

```csharp
var noclip = new CommandBuilder("noclip")
    .Execute(ctx => new CommandResult())
    .Build();

console.RegisterCommand(noclip);
```

Complex commands should prefer small immutable state records:

```csharp
public sealed record RestartCommand(
    bool IgnorePlayers,
    int DelaySeconds);

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

Prefer this typed state-record model over string IDs or mutable parameter handles as the primary user-facing API.

Command input should follow this order:

```text
<path> <required positional arguments...> [flags and options...]
```

Use positional arguments for required core command state. Use named options or flags for optional state. Do not make optional positional arguments part of the first public design.

Flag and option registration should support multiple user-facing logical names or aliases. These names should be defined without the configured command-line prefix; the parser applies `CommandParsingOptions.FlagAndOptionPrefix` later. Option and positional-argument value autocomplete should be opt-in through command-provided candidate functions.

Constraints should run against the fully bound command state and should be expressible through state properties or reusable constraint helpers.

Command definitions are immutable after `CommandBuilder.Build()`. Registration is owned by `ConsoleManager`, which validates command paths against the manager registry.

Command parser configuration should support:

```csharp
public enum OptionValueStyle
{
    SpaceSeparated,
    EqualSeparated,
    AnySeparated
}
```

Default command parsing uses `SpaceSeparated`, `FlagAndOptionPrefix = "--"`, case-insensitive matching, quoted string support, and order-independent flags/options after the path and required positional arguments. Prefixes such as `"-"` should be configurable for callers whose command schemas can safely support them.

Boolean option values should be parsed through `CommandParsingOptions.BooleanLiterals`, not through hard-coded `bool.TryParse` behavior. Defaults should remain strict and unsurprising: `true` means true, `false` means false, and aliases are opt-in.

Parsing is exposed through `ConsoleManager.ParseCommand(string input)`. It should return a structured success/failure result, not throw for normal user input mistakes. Parsing should not execute command handlers or write to history.

Successful parse results should expose the matched definition, typed state object when present, and bound argument/flag/option values by schema name. Failed parse results should expose a `ConsoleFailure`.

Autocomplete is exposed through `ConsoleManager.GetAutocomplete(string input, int cursorIndex)`. It should be stateless, side-effect-free, and best-effort while the user is typing. The UI owns selected-candidate/cycling state and applies candidates using the returned replacement range.

`CommandAutocompleteResult` should provide stateless apply helpers for candidate objects and candidate indexes. Autocomplete path completion should default to full command paths and offer opt-in dot-segment completion for large path-like command sets.

Synchronous command handlers should be the default. Leave API room for async handlers later without making async the initial baseline.

## Command Result And Output Direction

Commands should return `CommandResult`.

`CommandResult` supports success/failure status and zero, one, or many output objects. Output may be inline or block/multi-line. A help command, for example, should be able to return one multi-line output object rather than one timestamped entry per rendered line.

Use `CommandResult.Success(...)` for successful results with output and `CommandResult.Failed(...)` for expected command-level failures. The failure factory is named `Failed` because `CommandResult` already exposes a `Failure` property.

Prefer returned command output entries over imperative output methods on `CommandContext`.

`CommandContext` should remain available for command execution metadata such as the originating input, future caller/source information, service access if needed, and future async/cancellation integration. It should not be the primary output writing model.

## Console Entry API Direction

`IConsoleEntry` should remain an extensibility point for all console-visible entries. It should require timestamp and entry type/kind information, but it should not require log severity.

Built-in entry types should include log entries, command input entries, command output entries, and command failure/error entries. Users should be able to add custom entry types for game-specific console events.

Keep `LogLevel` on `LogEntry`. Command output should use semantic output kind and style IDs rather than log severity or a separate output-level enum. Custom entries should be free to expose metadata that makes sense for their domain.

## Styling And Formatting Direction

Entries should not store engine-specific formatted strings as their only representation.

Use `ConsoleText` as the shared semantic text model for console-visible text. Log entries, command input entries, command output entries, command failure entries, and future custom entries should be able to expose semantic text while preserving plain text convenience properties.

Formatting-aware string APIs are manager-owned. With formatting disabled, strings remain literal. With formatting enabled, known markup tags are parsed leniently while unknown tags and ordinary angle-bracket text remain literal:

```csharp
console.CreateText(
    "Gave <style=Player>Workes</style> <style=Amount>7</style> <style=Item>wood</style>",
    defaultStyle: "Success");
```

Malformed known markup in formatting-aware APIs is programmer-authored setup misuse and should throw standard .NET exceptions.

Command output should support formatting-aware strings as the primary authoring path:

```csharp
CommandOutput.Inline(
    "Gave <style=Player>Workes</style> <style=Amount>7</style> <style=Item>wood</style>",
    defaultStyle: "Success");
```

Command output should also support semantic content segments for structured data:

```csharp
CommandOutput.BuildInline(defaultStyle: "Success")
    .Text("Gave ")
    .Value(state.Amount.ToString(), style: "Amount", data: state.Amount)
    .Text(" ")
    .Value(state.ItemName, style: "Item", data: state.ItemName)
    .Text(" to ")
    .Value(state.ReceiverName, style: "Player", data: state.ReceiverName)
    .Build();
```

Formatting is package-wide rather than command-output-specific. The root formatting setup contains a model, markup profile, theme, and formatter. Formatting enablement should be derived from the configured setup, not manually toggled. Built-in Unity and Godot presets should make common engine setup one option object, while custom models/profiles/formatters should remain possible for advanced hosts.

Actual Unity/Godot UI controls remain outside the package. Advanced UIs should be able to consume semantic segments directly instead of relying on string markup.

## Error Handling And Validation

- Throw `ArgumentNullException` for null values that cannot be represented safely.
- Return `ConsoleFailure` for ordinary invalid user command input.
- Follow the `Workes.InventorySystem` split: expected domain rejection is structured failure data, expected-success wrappers may throw package-owned exceptions carrying that same failure, and programmer/setup misuse uses standard .NET exceptions.
- Prefer stable failure kinds/codes for branching and keep human-readable messages as display/debug text.
- Keep invalid-state rules close to the type that owns the state.

## Examples Of Preferred API Shape

```csharp
var console = new ConsoleManager();

console.LogInformation("Console ready.");
```

Prefer examples that start from one owned `ConsoleManager` instance and show implemented behavior only.
