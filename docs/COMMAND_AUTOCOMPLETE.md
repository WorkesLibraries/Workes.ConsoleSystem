# Command Autocomplete

`ConsoleManager.GetAutocomplete(...)` returns schema-driven autocomplete candidates for command input.

Autocomplete is stateless. It does not execute commands, parse into typed state, write command history, or write console history.

## Basic Usage

```csharp
CommandAutocompleteResult result = console.GetAutocomplete(input, cursorIndex);

foreach (CommandAutocompleteCandidate candidate in result.Candidates)
{
    Console.WriteLine(candidate.DisplayText);
}
```

Use `ReplacementStart` and `ReplacementLength` to replace the token fragment being completed with `candidate.Text`.

The result also has helpers for applying a candidate:

```csharp
string completed = result.Apply(result.Candidates[0]);
string alsoCompleted = result.Apply(0);
```

## What Can Be Completed

Autocomplete can suggest:

- command paths;
- flag names;
- option names;
- positional argument values when the command defines a provider;
- option values when the option defines a provider.

Flag and option candidates include the configured prefix:

```csharp
// With the default prefix:
--delay

// With FlagAndOptionPrefix = "-":
-delay
```

## Value Candidates

Use `ValueCandidates(...)` after a positional argument or option:

```csharp
CommandDefinition command = new CommandBuilder("player.give")
    .Argument<GiveState>(x => x.Player, "player")
        .ValueCandidates(ctx => new[] { "@me", "Anthony5172" })
    .Argument<GiveState>(x => x.Item, "item")
        .ValueCandidates(ctx => new[] { "wood", "stone", "gold" })
    .Option<GiveState>(x => x.Amount, "amount", "a")
        .ValueCandidates(ctx => new[] { "1", "10", "100" })
    .Execute<GiveState>((ctx, state) => new CommandResult())
    .Build();
```

The provider receives `CommandAutocompleteContext`, including the full input, cursor index, partial value, matched command, active member, and raw token text before the cursor.

If a value position has no provider, autocomplete returns no value candidates for that position.

## Typing Behavior

Autocomplete is best-effort while the user is typing:

- empty input suggests registered command paths;
- partial first tokens suggest matching command paths;
- incomplete quotes still allow value suggestions;
- replacement ranges cover only the current token fragment;
- already-used flags and options are not suggested again.

Option value completion follows `OptionValueStyle`:

```csharp
// SpaceSeparated
server.restart maintenance --delay 3

// EqualSeparated
server.restart maintenance --delay=3

// AnySeparated
server.restart maintenance --delay 3
server.restart maintenance --delay=3
```

## Dot-Path Completion

By default, command paths are completed as whole paths. This is good for small command sets:

```text
Input:  ser
Output: server.restart
```

For larger dot-separated command sets, segment-by-segment completion can feel better. It lets the user walk a command path one segment at a time instead of immediately suggesting every full command path that happens to start with the same letters.

This is useful for command sets shaped like:

```text
Player.AddItem
Player.ModAv
Player.SetAv
Player.Inventory.SetModifier
Player.Inventory.Clear
Platoon.Spawn
Platoon.Clear
Server.Restart
```

Enable it through autocomplete options:

```csharp
var console = new ConsoleManager(new ConsoleManagerOptions
{
    Autocomplete = new CommandAutocompleteOptions
    {
        PathCompletionMode = CommandPathCompletionMode.DotSegment
    }
});
```

With dot-segment completion:

```text
Input:        Pl
Candidates:   Player, Platoon
Applied:      Player

Input:        Player.Add
Candidates:   AddItem
Applied:      Player.AddItem

Input:        Player.Inventory.Set
Candidates:   SetModifier
Applied:      Player.Inventory.SetModifier
```

In this mode, candidate `Text` contains only the segment that should be inserted. `DisplayText` can show the larger path context when that is helpful for UI:

```csharp
CommandAutocompleteResult result = console.GetAutocomplete("Player.Add", "Player.Add".Length);
CommandAutocompleteCandidate candidate = result.Candidates[0];

Console.WriteLine(candidate.Text);        // AddItem
Console.WriteLine(candidate.DisplayText); // Player.AddItem

string completed = result.Apply(candidate); // Player.AddItem
```

The dot is treated as a path separator, and completion always works from the current path prefix to the next matching segment:

```text
Pl                      -> Player
Player.In               -> Player.Inventory
Player.Inventory.Set    -> Player.Inventory.SetModifier
```

Dot-segment completion only changes command path suggestions. Once a command path is complete and the cursor is in an argument, flag, option, or option value position, autocomplete uses the normal member-completion rules.

Use `FullPath` when the command set is small or when users usually know the exact command name. Use `DotSegment` when commands are naturally grouped by prefixes and users are likely to browse one path segment at a time.

## Related Guides

- [ConsoleManager](CONSOLE_MANAGER.md)
- [Command Registration](COMMAND_REGISTRATION.md)
- [Command Parsing](COMMAND_PARSING.md)
- [Configuration](CONFIGURATION.md)
