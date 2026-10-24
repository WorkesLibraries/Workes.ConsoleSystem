# Configurable help command

`ConsoleManager` can register an optional help command. It is built separately from ordinary commands because the manager supplies the live command registry and applies its caller's permission and availability checks. ContentSystem remains outside the public API.

## Register help

```csharp
using System.Collections.Generic;
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Core;

var help = new HelpCommandBuilder()
    .FormatWith(context => new List<string>
    {
        $"{context.Command.Path} : {context.Command.Description}"
    })
    .EnableSearchOption("search", aliases: new[] { "s" })
    .EnableCommandHelpOption("command")
    .Build();

console.RegisterHelpCommand(help);
```

The help command path defaults to `help`; pass another path to `HelpCommandBuilder` to customize it. The shared formatter is required. Search and command-specific formatters are optional and fall back to the shared formatter. A formatter receives a `CommandHelpContext` and returns `List<string>` lines. Each command's returned lines become one block output. Returning an empty list hides that command's formatted output.

The help builder and registration API are in the current repository source after the published 0.5.0 package; they will ship in a future package release.

## Choose query forms

Overview output is always available with no query:

```text
help
```

Search and command-specific help are independent opt-ins. Positional and option forms can be enabled independently:

```csharp
new HelpCommandBuilder()
    .FormatWith(FormatOverview)
    .EnableSearchPositional(FormatSearch)
    .EnableSearchOption("search", FormatSearch, "s")
    .EnableCommandHelpPositional(FormatCommand)
    .EnableCommandHelpOption("command", FormatCommand)
    .Build();
```

Each option mode requires its option name without the configured prefix; aliases are optional. For the example above, callers can use `help PlayerGive`, `help --search give`, or `help --command player.give`. Use either a positional query or one query option per invocation; mixed query sources are rejected.

Positional search performs a substring search over registered command paths using the manager's configured case-sensitivity setting. If positional search is the only positional feature enabled, even an exact command path is treated as a search term, so `help PlayerGive` can show both `PlayerGive` and `PlayerGiveStat`. If positional command help is also enabled, an exact accessible command path uses the command-help formatter; other terms use positional search if enabled.

An explicit search option always searches the overview, even if the term exactly matches a command. An explicit command-help option requests details for that command. Search and command-help options cannot be used together. Supplying any positional query together with either option is also rejected.

Unknown terms produce no output when no applicable search mode is enabled. A search with no visible matches also produces no output; applications can use the command context and formatters to control output for matching commands.

## Format with context

`CommandHelpContext` exposes:

- `Command`: the command currently being formatted.
- `SearchTerm`: the positional or option query, or `null` for the overview.
- `IsAccessible` and `AccessFailure`: the result of this manager's caller permission and availability checks.
- `MatchedCommands`: a read-only list of path matches admitted by the access policy for this overview/search. A command-specific request has a one-command match list.

Formatters can use the command path, description, arguments, flags, options, defaults, constraints, permission metadata, and echo/output metadata from `CommandDefinition`. They can also use `MatchedCommands` for related-command output or return no lines for a command they do not want shown.

```csharp
using System.Collections.Generic;
using System.Linq;

static List<string> FormatOverview(CommandHelpContext context) => new()
{
    $"{context.Command.Path} : {context.Command.Description}"
};

static List<string> FormatSearch(CommandHelpContext context) => new()
{
    $"{context.Command.Path} : {context.Command.Description}",
    $"Related: {string.Join(", ", context.MatchedCommands.Select(command => command.Path))}"
};

static List<string> FormatCommand(CommandHelpContext context) => new()
{
    context.Command.Path,
    context.Command.Description,
    $"Arguments: {context.Command.Arguments.Count}",
    $"Options: {context.Command.Options.Count}"
};
```

## Blocked commands

The default policy hides commands blocked by permissions or availability checks and treats direct requests as nonexistent. Supply an `AccessPolicy` to change that behavior. The policy receives `CommandHelpAccessContext`, which includes the command, query, path-matched command set (including blocked candidates), and structured access failure. It returns one of:

- `HelpCommandAccessDecision.Hide()` to omit the blocked command.
- `HelpCommandAccessDecision.Show()` to pass it to the configured formatter.
- `HelpCommandAccessDecision.Respond(lines)` to return custom output instead of formatted details.

Built-in policies are `HelpCommandAccessPolicies.HideBlocked` (the default), `ShowBlocked`, and `ExplainBlocked`. The explanation policy distinguishes permission denial from runtime unavailability. A custom policy can inspect `AccessFailure.Kind` and `AccessFailure.Message` and choose any behavior:

```csharp
using System.Collections.Generic;
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Core;

var help = new HelpCommandBuilder()
    .FormatWith(FormatOverview)
    .EnableCommandHelpOption("command")
    .AccessPolicy(context => context.AccessFailure.Kind switch
    {
        ConsoleFailureKind.CommandAuthorization => HelpCommandAccessDecision.Respond(
            new[] { "You do not have access to this command." }),
        ConsoleFailureKind.CommandAvailability => HelpCommandAccessDecision.Respond(
            new[] { "This command cannot be used at this time." }),
        _ => HelpCommandAccessDecision.Hide()
    })
    .Build();

console.RegisterHelpCommand(help);
```

Access policies run only for commands that fail the manager's access checks. Availability checks receive the help input and a `null` parsed-command value, as they do during autocomplete. Exceptions from access checks or formatters follow the normal command execution failure path.
