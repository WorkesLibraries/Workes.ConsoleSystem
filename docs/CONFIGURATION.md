# Configuration

`ConsoleManagerOptions` configures a `ConsoleManager` during construction.

All options have defaults. You only need to set values that differ from the normal behavior.

```csharp
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Presentation;

var console = new ConsoleManager(new ConsoleManagerOptions
{
    CommandParsing = new CommandParsingOptions
    {
        OptionValueStyle = OptionValueStyle.AnySeparated,
        FlagAndOptionPrefix = "--",
        BooleanLiterals = new BooleanLiteralOptions
        {
            TrueLiterals = new[] { "true", "yes", "on" },
            FalseLiterals = new[] { "false", "no", "off" }
        }
    },
    History = new HistoryOptions
    {
        ConsoleHistoryCapacity = 200,
        CommandHistoryCapacity = 100,
        CommandHistoryDuplicatePolicy = CommandHistoryDuplicatePolicy.RejectConsecutive
    },
    Formatting = ConsoleFormattingOptions.UnityRichText(
        new ConsoleTheme(new Dictionary<string, ConsoleStyle>
        {
            ["Success"] = ConsoleStyle.Standard(
                foregroundColor: ConsoleColor.FromHex("#4ade80"),
                bold: true)
        })),
    Execution = new CommandExecutionOptions
    {
        EchoInput = true,
        EchoInputDefaultStyle = "CommandInput"
    }
});
```

## Snapshot Behavior

Options are mutable setup objects. This keeps object-initializer setup simple.

`ConsoleManager` copies the supplied values during construction. Changing the original options object later does not change the manager.

```csharp
var options = new ConsoleManagerOptions
{
    History = new HistoryOptions
    {
        ConsoleHistoryCapacity = 50
    }
};

var console = new ConsoleManager(options);

options.History.ConsoleHistoryCapacity = 500;

Console.WriteLine(console.History.Capacity); // 50
```

`console.Options` exposes the resolved option values for inspection.

## Command Parsing Options

Command parsing options control active `ConsoleManager.ParseCommand(...)` behavior.

Defaults:

- `OptionValueStyle = OptionValueStyle.SpaceSeparated`
- `FlagAndOptionPrefix = "--"`
- `IsCaseSensitive = false`
- `AllowQuotedStrings = true`
- `AllowFlagsAndOptionsInAnyOrder = true`
- `BooleanLiterals.TrueLiterals = [ "true" ]`
- `BooleanLiterals.FalseLiterals = [ "false" ]`

Flag and option schema names are defined without this prefix. The parser applies the prefix to command input.

Boolean literals configure accepted values for boolean options. Matching follows `IsCaseSensitive`, so aliases are case-insensitive by default. The true and false literal lists cannot be empty and cannot overlap.

## History Options

History options are active now.

Defaults:

- `ConsoleHistoryCapacity = 200`
- `CommandHistoryCapacity = 100`
- `CommandHistoryDuplicatePolicy = CommandHistoryDuplicatePolicy.RejectConsecutive`

Both capacities must be greater than zero.

## Formatting Options

Formatting options configure the optional formatting subsystem.

Defaults:

- formatting is disabled when no formatter is configured
- `Model = null`
- `MarkupProfile = null`
- `Theme = null`
- `Formatter = null`

Use `ConsoleFormattingOptions.UnityRichText()` or `ConsoleFormattingOptions.GodotBbCode()` for plug-and-play engine formatting. These presets configure a formatter, so formatting is enabled automatically. Advanced users can use `ConsoleFormattingOptions.Custom(...)` with a custom model, markup profile, theme, and formatter.

The core entries store semantic content and plain text, not engine-specific markup.

## Command Execution Options

Execution options configure behavior that later command execution will use.

Defaults:

- `EchoInput = true`
- `EchoInputDefaultStyle = null`

`EchoInputDefaultStyle = null` means echoed command input is plain/un-styled unless a command-specific override or future execution code applies another style.

## Related Guides

- [ConsoleManager](CONSOLE_MANAGER.md)
- [Console History](CONSOLE_HISTORY.md)
- [Command History](COMMAND_HISTORY.md)
- [Command Registration](COMMAND_REGISTRATION.md)
- [Command Parsing](COMMAND_PARSING.md)
- [Command Results And Output](COMMAND_OUTPUT.md)
- [Formatting](FORMATTING.md)
