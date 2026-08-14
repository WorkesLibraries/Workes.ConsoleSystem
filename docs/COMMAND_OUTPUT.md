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
    CommandOutput.InlineText("Noclip enabled.", defaultStyle: "Success"));
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
    CommandOutput.InlineText("Player was not found.", defaultStyle: "Error"));
```

The result exposes the same failure through `result.Failure`. The factory is named `Failed` because C# cannot expose both a `Failure` property and a `Failure(...)` factory method on the same type.

## Semantic Output

`CommandOutput` stores plain text as ordered semantic segments. A segment can carry:

- text;
- an optional style ID;
- optional structured data.

```csharp
var output = CommandOutput.Inline(defaultStyle: "Success")
    .Text("Gave ")
    .Value("10", style: "Amount", data: 10)
    .Text(" gold to ")
    .Value("Anthony5172", style: "Player", data: "Anthony5172")
    .Build();

Console.WriteLine(output.PlainText); // Gave 10 gold to Anthony5172
```

Segments without an explicit style inherit the output's default style. Segments with their own style use that style instead.

## Inline And Block Output

Inline output is intended for short single-line responses:

```csharp
var output = CommandOutput.InlineText("Saved.", defaultStyle: "Success");
```

Block output is intended for larger or multi-line responses:

```csharp
var output = CommandOutput.BlockText(
    "Commands:\nhelp\nnoclip\nserver.restart",
    defaultStyle: "Information");
```

Both forms preserve semantic segments and derive plain text by concatenating segment text.

## Entries

`CommandOutputEntry` wraps command output with a timestamp for the shared console history:

```csharp
using Workes.ConsoleSystem.Entries;

var entry = new CommandOutputEntry(DateTimeOffset.UtcNow, output);

Console.WriteLine(entry.PlainText);
```

Automatic command execution does not write output entries yet. For now, entries can be created directly by tests, custom host code, or future execution code.

## Styling

Command output can carry semantic style IDs now, but package-wide themes, markup parsing, and formatter integration are not implemented yet.

The intended direction is that plain text remains available for simple hosts, while richer hosts can use semantic output and future formatting APIs to render styled console history without storing Unity, Godot, HTML, or terminal-specific markup as the only representation.
