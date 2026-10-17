# Command autocomplete

`ConsoleManager.GetAutocomplete(input, cursorIndex)` suggests schema-aware completions for the text at the cursor. Autocomplete returns data for a host UI; it does not own a selected item, keyboard behavior, or rendered menu.

> Stateless autocomplete is included in the published 0.3.0 package. Command permission and availability filtering described in [Command access](COMMAND_ACCESS.md) is unreleased development work.

## Query and apply a candidate

```csharp
CommandAutocompleteResult result = console.GetAutocomplete(input, cursorIndex);

foreach (CommandAutocompleteCandidate candidate in result.Candidates)
{
    System.Console.WriteLine(candidate.DisplayText);
}

if (result.Candidates.Count > 0)
{
    input = result.Apply(result.Candidates[0]);
}
```

`cursorIndex` is a zero-based index into the .NET input string and may be anywhere from `0` through `input.Length`. The result includes the original input and cursor, `ReplacementStart`, `ReplacementLength`, and the candidates. `Apply(candidate)` or `Apply(index)` replaces that range and keeps the rest of the input. Invalid cursor positions and candidate indexes throw standard argument exceptions.

Each `CommandAutocompleteCandidate` exposes:

- `Text`: the exact insertion text for the replacement range.
- `DisplayText`: text suitable for showing in a suggestion list; it defaults to `Text`.
- `Kind`: one of `CommandPath`, `ArgumentValue`, `Flag`, `Option`, or `OptionValue`.
- `Description`: optional description. Command paths use the command description; the current builder does not expose member descriptions, so built-in flag and option suggestions have an empty description.
- `IsAvailable` and `UnavailableReason`: access status for command path candidates when blocked-command suggestions are enabled in the development API.

Candidates are returned in command registration and schema order. The UI chooses which candidate is selected and how to cycle or display candidates.

## Candidate sources

The manager suggests matching command paths, then the unused flag names and option names for the matched command. Named candidates include the configured `FlagAndOptionPrefix`; aliases are suggested as their own candidates. A flag or option already supplied before the cursor is omitted.

Positional argument values and option values can be suggested by a provider attached to that schema member. For options, `AllowedValues(...)` remains validation-only unless followed by `SuggestAllowedValues()`. This opt-in suggests the allowed set; an explicit `ValueCandidates(...)` provider takes precedence and can return a chosen subset or dynamic candidates.

```csharp
var command = new CommandBuilder("player.give")
    .Argument<GiveState>(state => state.Player, "player")
        .ValueCandidates(context => FindPlayers(context.PartialValue))
    .Argument<GiveState>(state => state.Item, "item")
        .ValueCandidates(context => new[] { "wood", "stone", "gold" })
    .Option<GiveState>(state => state.Amount, "amount", "a")
        .ValueCandidates(context => new[] { "1", "10", "100" })
    .Execute<GiveState>((context, state) => CommandResult.Success())
    .Build();

console.RegisterCommand(command);

internal sealed record GiveState(string Player, string Item, int Amount);
```

Providers are synchronous and run when a query reaches their value position. They receive `CommandAutocompleteContext` with the full `Input`, `CursorIndex`, current `PartialValue`, matched `Command`, active argument or option `Member`, and token texts before the cursor. This lets a provider use earlier input or current application state to build suggestions. A provider's exception propagates to the caller; autocomplete does not turn provider errors into command execution failures. Null candidate strings are ignored.

For a fixed option value set, opt in without repeating the values in a provider:

```csharp
var mode = new CommandBuilder("game.mode")
    .Option<ModeState>(state => state.Mode, "mode")
        .AllowedValues("creative", "survival", "spectator")
        .SuggestAllowedValues()
    .Execute<ModeState>((context, state) => CommandResult.Success())
    .Build();
```

## Cursor, token, and option behavior

Autocomplete is best-effort while the user types:

- Empty input and a partial first token suggest registered command paths.
- Command path and named-member matching follow `CommandParsing.IsCaseSensitive`.
- When the cursor is inside a token, matching uses the text to its left while the replacement range covers the token's value, so applying a candidate replaces the whole current fragment.
- Quoted values are completed inside their quotes. An opening quote may be incomplete; suggestions still work, and applying a candidate does not insert a missing closing quote.
- A quoted token that begins with the flag/option prefix is treated as a value.
- Named option value suggestions follow `OptionValueStyle`. With `EqualSeparated` or `AnySeparated`, a query such as `--amount=1` completes only the value after `=`; with `SpaceSeparated`, use `--amount 1`.
- Positional arguments retain their schema order. When `AllowInterleavedOptions` is enabled, options may precede or appear between positional values and autocomplete tracks consumed values accordingly.
- A value position without a provider returns no value candidates.

Autocomplete shares command tokenization rules with parsing, while allowing an incomplete quote during interactive input. It does not parse final typed state, run validation constraints, invoke handlers, record command inputs, or write visible history. Candidate providers are application callbacks, so any side effects inside a provider are owned by that application.

## Full path and dot-segment completion

`CommandAutocomplete.PathCompletionMode` defaults to `FullPath`. Typing `ser` can suggest `server.restart`; the candidate replaces the current command path.

For command sets with hierarchical dot-separated paths, `DotSegment` suggests one segment at a time:

```csharp
var console = new ConsoleManager(new ConsoleManagerOptions
{
    Autocomplete = new CommandAutocompleteOptions
    {
        PathCompletionMode = CommandPathCompletionMode.DotSegment
    }
});
```

With commands such as `Player.Inventory.SetModifier` and `Platoon.Spawn`:

```text
Pl                         -> Player, Platoon
Player.Inventory.Set       -> SetModifier
```

In dot-segment mode, `Text` contains only the segment to insert and `DisplayText` shows the full matching path. Applying a candidate replaces the current path segment and preserves the rest of the input.

See [Command registration](COMMAND_REGISTRATION.md), [Parsing](COMMAND_PARSING.md), and [Manager and configuration](MANAGER_AND_CONFIGURATION.md) for the related schema and parser settings.
