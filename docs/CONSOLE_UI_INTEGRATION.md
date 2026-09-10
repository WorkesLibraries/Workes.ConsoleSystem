# Console UI Integration

`Workes.ConsoleSystem` does not include a rendered console UI. Unity, Godot, terminal, editor, and custom tool UIs can all use the same manager-owned history and command APIs.

## Basic Rendering Loop

Read `console.History.Entries` and branch on concrete entry type:

```csharp
foreach (IConsoleEntry entry in console.History.Entries)
{
    switch (entry)
    {
        case LogEntry log:
            RenderLine(log.Timestamp, log.Level.ToString(), log.Content);
            break;

        case CommandInputEntry input:
            RenderLine(input.Timestamp, "Input", input.Content);
            break;

        case CommandOutputEntry output:
            RenderLine(output.Timestamp, "Output", console.ResolveOutput(output.Output));
            break;

        case CommandFailureEntry failure:
            RenderLine(failure.Timestamp, "Failure", failure.Content);
            break;

        default:
            RenderCustom(entry);
            break;
    }
}
```

The concrete entry type is the entry discriminator. `IConsoleEntry` only guarantees a timestamp.

## Submitting Input

Use `TryExecuteCommand(...)` for player-authored text:

```csharp
if (!console.TryExecuteCommand(inputText, out CommandResult result))
{
    ShowCommandFailure(result.Failure!.Message);
}
```

Execution records nonblank submitted input in `CommandHistory`. When input echo is enabled, execution also writes a `CommandInputEntry` to `ConsoleHistory`.

Use `ExecuteCommand(...)` for setup, tests, or scripted developer tooling where failure should throw `ConsoleOperationException`.

## Command Input Navigation

`console.CommandHistory.Entries` is ordered from oldest retained input to newest retained input. UI code owns cursor position and navigation behavior.

```csharp
IReadOnlyList<string> submittedInputs = console.CommandHistory.Entries;
```

Use `console.RecordCommandInput(...)` only when you need to record text without executing it.

## Autocomplete

Call `GetAutocomplete(...)` as the user edits input:

```csharp
CommandAutocompleteResult autocomplete = console.GetAutocomplete(inputText, cursorIndex);

foreach (CommandAutocompleteCandidate candidate in autocomplete.Candidates)
{
    RenderCandidate(candidate.DisplayText);
}

string completed = autocomplete.Apply(0);
```

The package returns candidates and replacement ranges. The UI owns selected candidate index, cycling, menus, keyboard input, and caret placement.

## Formatting

Formatting is implemented, but it is optional. UI code can render every console entry as plain text without configuring formatting at all.

If formatting is disabled, render `ConsoleText.PlainText`:

```csharp
void RenderText(ConsoleText text)
{
    AddConsoleLine(text.PlainText);
}
```

If formatting is enabled, use the manager formatter before sending text to the UI control:

```csharp
void RenderText(ConsoleText text)
{
    AddConsoleLine(console.Format(text));
}
```

Unity projects can configure `ConsoleFormattingOptions.UnityRichText()` and pass the formatted string to a Unity text component that supports rich text. Godot projects can configure `ConsoleFormattingOptions.GodotBbCode()` and pass the formatted string to a `RichTextLabel` with BBCode enabled.

```csharp
var console = new ConsoleManager(new ConsoleManagerOptions
{
    Formatting = ConsoleFormattingOptions.UnityRichText()
});

console.LogInformation("<style=Success><b>Console ready.</b></style>");

foreach (IConsoleEntry entry in console.History.Entries)
{
    if (entry is LogEntry log)
    {
        string rendered = console.Format(log.Content);
        AddConsoleLine(rendered);
    }
}
```

Command output needs one extra step because string-authored output is resolved through the active manager before it is formatted:

```csharp
if (entry is CommandOutputEntry output)
{
    string rendered = console.Format(output.Output);
    AddConsoleLine(rendered);
}
```

`console.Format(output.Output)` resolves the command output into `ConsoleText` and then applies the active formatter. This is the normal path for UI code.

Custom UIs do not have to use string formatting. They can consume `ConsoleText.Segments` directly and translate each segment into native UI spans:

```csharp
void RenderSegments(ConsoleText text)
{
    foreach (ConsoleTextSegment segment in text.Segments)
    {
        string? styleId = segment.ResolveStyleId(text.DefaultStyleId);
        RenderSpan(segment.Text, styleId, segment.Data);
    }
}
```

Direct segment rendering is useful when the UI has native style spans, separate labels per span, or custom controls that should not receive Unity rich text or Godot BBCode strings.

See [Formatting](FORMATTING.md) for the full formatting model, markup rules, themes, built-in formatters, and structured text builder API.

## Related Guides

- [Console History](CONSOLE_HISTORY.md)
- [Command History](COMMAND_HISTORY.md)
- [Command Execution](COMMAND_EXECUTION.md)
- [Command Autocomplete](COMMAND_AUTOCOMPLETE.md)
- [Formatting](FORMATTING.md)
