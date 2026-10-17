# Manager and configuration

`ConsoleManager` is the root for one console instance. It owns the registered command set, visible console history, command input history, logging methods, and command workflows. Create one manager per console; no global manager or logger object is required.

```csharp
var console = new ConsoleManager();
```

Pass `ConsoleManagerOptions` to customize behavior. Nested option sections can be omitted or set to `null`; omitted sections resolve to their defaults.

```csharp
var console = new ConsoleManager(new ConsoleManagerOptions
{
    CommandParsing = new CommandParsingOptions
    {
        OptionValueStyle = OptionValueStyle.AnySeparated,
        AllowInterleavedOptions = true
    },
    History = new HistoryOptions
    {
        ConsoleHistoryCapacity = 500,
        CommandHistoryCapacity = 150
    },
    Execution = new CommandExecutionOptions
    {
        EchoInput = false
    }
});
```

## Configuration defaults

| Setting | Default | Purpose |
| --- | --- | --- |
| `CommandParsing.OptionValueStyle` | `SpaceSeparated` | How named option values are written. |
| `CommandParsing.FlagAndOptionPrefix` | `--` | Prefix for flags and named options. |
| `CommandParsing.IsCaseSensitive` | `false` | Whether command paths and named members require exact case. |
| `CommandParsing.AllowQuotedStrings` | `true` | Whether quoted tokens are accepted. |
| `CommandParsing.AllowInterleavedOptions` | `false` | Whether flags/options may appear among positional values. |
| `CommandParsing.BooleanLiterals` | `true` / `false` | Accepted boolean option values. |
| `Autocomplete.PathCompletionMode` | `FullPath` | Selects whole-path or dot-segment suggestions. |
| `History.ConsoleHistoryCapacity` | `200` | Maximum visible console entries retained. |
| `History.CommandHistoryCapacity` | `100` | Maximum submitted command strings retained. |
| `History.CommandHistoryDuplicatePolicy` | `RejectConsecutive` | Whether the newest duplicate command string is suppressed. |
| `Execution.EchoInput` | `true` | Default input echo during command execution. |

Changing a capacity to zero or less, using an invalid enum value, providing an empty or overlapping boolean literal set, or configuring an invalid flag/option prefix fails when the manager snapshots the options. Boolean literal arrays are copied. The manager snapshots the complete option graph at construction; later changes to the original setup object do not affect it. Each read of `console.Options` is a new detached snapshot.

See [Command parsing](COMMAND_PARSING.md), [history](HISTORY.md), and [execution](COMMAND_EXECUTION.md) for how these values affect behavior. See [Command autocomplete](COMMAND_AUTOCOMPLETE.md) for the completion API included in the 0.3.0 release target.
