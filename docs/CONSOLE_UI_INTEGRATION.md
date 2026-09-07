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

Execution records valid nonblank submitted input in `CommandHistory`. When input echo is enabled, execution also writes a `CommandInputEntry` to `ConsoleHistory`.

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

If formatting is disabled, render `ConsoleText.PlainText`.

If formatting is enabled, use the manager formatter:

```csharp
string rendered = console.Format(log.Content);
```

Unity projects can configure `ConsoleFormattingOptions.UnityRichText()`. Godot projects can configure `ConsoleFormattingOptions.GodotBbCode()`. Custom UIs can either write their own formatter or consume `ConsoleText.Segments` directly.

## Related Guides

- [Console History](CONSOLE_HISTORY.md)
- [Command History](COMMAND_HISTORY.md)
- [Command Execution](COMMAND_EXECUTION.md)
- [Command Autocomplete](COMMAND_AUTOCOMPLETE.md)
- [Formatting](FORMATTING.md)
