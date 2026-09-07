# Command Results And Output

Command handlers return `CommandResult`.

`CommandResult` represents whether a command succeeded and what output it produced. Command execution is not implemented yet, but command schemas can already store handlers that return this result type.

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

Automatic command execution does not write output entries yet. For now, entries can be created directly by tests, custom host code, or future execution code.

## Styling

Command output uses the package-wide `ConsoleText` model.

See [Formatting](FORMATTING.md) for opt-in markup, themes, Unity rich text formatting, and Godot BBCode formatting.
