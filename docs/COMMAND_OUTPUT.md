# Command Results And Output

Command handlers return `CommandResult`.

`CommandResult` represents whether a command succeeded and what output it produced. Command handlers return this type during command execution.

## Empty Success

Simple commands can return an empty successful result:

```csharp
using Workes.ConsoleSystem.Commands;

return new CommandResult();
```

This is equivalent to a successful command with no output.

## Output Results

Use `CommandResult.Success(...)` when a command should return one or more output objects:

```csharp
return CommandResult.Success(
    CommandOutput.Inline("<style=Success>Noclip enabled.</style>"));
```

Outputs are stored in the order supplied.

## Failure Results

Use `CommandResult.Failed(...)` for expected command-level failures:

```csharp
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Core;

var failure = ConsoleFailure.Create(
    ConsoleFailureKind.CommandExecution,
    ConsoleFailureCodes.CommandExecutionRejected,
    "Player was not found.");

return CommandResult.Failed(
    failure,
    CommandOutput.Inline("<style=Error>Player was not found.</style>"));
```

The result exposes the same failure through `result.Failure`. The factory is named `Failed` because C# cannot expose both a `Failure` property and a `Failure(...)` factory method on the same type.

## Semantic Output

`CommandOutput` can be authored from strings or ordered semantic segments. A segment can carry:

- text;
- an optional style ID;
- optional structured data.

```csharp
var output = CommandOutput.Inline(
    "Gave <style=Amount>10</style> gold to <style=Player>Anthony5172</style>",
    defaultStyle: "Success");

Console.WriteLine(output.PlainText); // Gave 10 gold to Anthony5172
```

String-authored output stores the original text and derives plain text immediately. A formatting-enabled `ConsoleManager` resolves known markup into `ConsoleText` when formatting is needed. If formatting is disabled, the original string stays literal.

The builder is useful when a command needs structured segment data:

```csharp
var output = CommandOutput.BuildInline(defaultStyle: "Success")
    .Text("Gave ")
    .Value("10", style: "Amount", data: 10)
    .Text(" gold.")
    .Build();
```

`CommandOutputBuilder` is returned by `CommandOutput.BuildInline(...)` and `CommandOutput.BuildBlock(...)`. Use `Text(...)` for plain segments and `Value(...)` when a segment should carry a style ID and optional structured data.

## Inline And Block Output

Inline output is intended for short single-line responses:

```csharp
var output = CommandOutput.Inline("Saved.", "Success");
```

The same string API can contain markup when formatting is enabled:

```csharp
var output = CommandOutput.Inline("<style=Success>Saved.</style>");
```

Block output is intended for larger or multi-line responses:

```csharp
var output = CommandOutput.Block(
    "Commands:\nhelp\nnoclip\nserver.restart",
    defaultStyle: "Information");
```

Both forms preserve semantic content and derive plain text by concatenating segment text.

## Entries

`CommandOutputEntry` wraps command output with a timestamp for the shared console history:

```csharp
using Workes.ConsoleSystem.Entries;

var entry = new CommandOutputEntry(DateTimeOffset.UtcNow, output);

Console.WriteLine(entry.PlainText);
```

Command execution writes output entries automatically for declared success output and handler-returned output. Entries can also be created directly by tests or custom host code.

`CommandOutput.Kind` is `CommandOutputKind.Inline` or `CommandOutputKind.Block`. Inline output is normally rendered as one line. Block output may contain multiple lines and should be treated as one intentional output entry.

## Styling

Command output uses the package-wide `ConsoleText` model.

See [Formatting](FORMATTING.md) for opt-in markup, themes, Unity rich text formatting, and Godot BBCode formatting.

See [Command Execution](COMMAND_EXECUTION.md) for how command results become console history entries.
