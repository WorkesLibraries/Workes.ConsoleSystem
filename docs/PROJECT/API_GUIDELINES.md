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
- Keep namespaces grouped by responsibility: `Core`, `History`, `Logging`, `Entries`, and `Commands`.
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

Parsing is exposed through `ConsoleManager.ParseCommand(string input)`. It should return a structured success/failure result, not throw for normal user input mistakes. Parsing should not execute command handlers or write to history.

Successful parse results should expose the matched definition, typed state object when present, and bound argument/flag/option values by schema name. Failed parse results should expose a stable `CommandParseErrorCode` and a human-readable message.

Synchronous command handlers should be the default. Leave API room for async handlers later without making async the initial baseline.

## Command Result And Output Direction

Commands should return `CommandResult`.

`CommandResult` should support success/failure status and zero, one, or many output entries. Output entries may be inline or block/multi-line. A help command, for example, should be able to return one multi-line output entry rather than one timestamped entry per rendered line.

Prefer returned command output entries over imperative output methods on `CommandContext`.

`CommandContext` should remain available for command execution metadata such as the originating input, future caller/source information, service access if needed, and future async/cancellation integration. It should not be the primary output writing model.

## Console Entry API Direction

`IConsoleEntry` should remain an extensibility point for all console-visible entries. It should require timestamp and entry type/kind information, but it should not require log severity.

Built-in entry types should include log entries, command input entries, command output entries, and command failure/error entries. Users should be able to add custom entry types for game-specific console events.

Keep `LogLevel` on `LogEntry`. Command output should use command-output-specific metadata such as `CommandOutputLevel`. Custom entries should be free to expose metadata that makes sense for their domain.

## Styling And Formatting Direction

Entries should not store engine-specific formatted strings as their only representation.

Command output should support semantic content segments:

```csharp
CommandOutput.Inline(defaultStyle: "Success")
    .Text("Gave ")
    .Value(state.Amount.ToString(), style: "Amount", data: state.Amount)
    .Text(" ")
    .Value(state.ItemName, style: "Item", data: state.ItemName)
    .Text(" to ")
    .Value(state.ReceiverName, style: "Player", data: state.ReceiverName);
```

Themes should map semantic style IDs to style values. Formatters should convert semantic content and theme values into plain text, Unity rich text, Godot BBCode, terminal output, or custom UI representations.

Actual Unity/Godot UI controls remain outside the package. Advanced UIs should be able to consume semantic segments directly instead of relying on string markup.

## Error Handling And Validation

- Throw `ArgumentNullException` for null values that cannot be represented safely.
- Return structured parse errors for ordinary invalid user command input.
- Keep invalid-state rules close to the type that owns the state.
- Do not add broad exception hierarchies until command behavior or history policies create real error cases.

## Examples Of Preferred API Shape

```csharp
var console = new ConsoleManager();

console.LogInformation("Console ready.");
```

Prefer examples that start from one owned `ConsoleManager` instance and show implemented behavior only.
