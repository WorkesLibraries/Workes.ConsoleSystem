# Configurable help command

`ConsoleManager` can register an optional help command. It is built separately from ordinary commands because the manager supplies the live command registry and applies its caller's permission and availability checks. ContentSystem remains outside the public API.

## Register help

```csharp
using System.Collections.Generic;
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Core;

var help = new HelpCommandBuilder()
    .EnableSearchOption("search", aliases: new[] { "s" })
    .EnableCommandHelpOption("command")
    .Build();

console.RegisterHelpCommand(help);
```

The help command path defaults to `help`; pass another path to `HelpCommandBuilder` to customize it. The default formatter is `HelpCommandFormatters.Simple`, which writes the path and (when present) description. `HelpCommandFormatters.Detailed` shows available command and binding metadata. Use `FormatWith(...)` to replace the shared formatter. Search and command-specific formatters are optional and otherwise fall back to the shared formatter. A formatter receives a `CommandHelpContext` and returns `List<string>` lines. Each command's returned lines become one block output. Returning an empty list hides that command's formatted output.

The help command is opt-in. Register it once per `ConsoleManager`; each manager uses its own live registry and caller access provider.

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
    .EnableSearchOption("search", FormatSearch, aliases: new[] { "s" })
    .EnableCommandHelpPositional(FormatCommand)
    .EnableCommandHelpOption("command", FormatCommand)
    .Build();
```

Each option mode requires its option name without the configured prefix; aliases are supplied as an optional `IReadOnlyList<string>`. For the example above, callers can use `help PlayerGive`, `help --search give`, or `help --command player.give`. Use either a positional query or one query option per invocation; mixed query sources are rejected.

Positional search performs a substring search over registered command paths using the manager's configured case-sensitivity setting. If positional search is the only positional feature enabled, even an exact command path is treated as a search term, so `help PlayerGive` can show both `PlayerGive` and `PlayerGiveStat`. If positional command help is also enabled, an exact accessible command path uses the command-help formatter; other terms use positional search if enabled.

An explicit search option always searches the overview, even if the term exactly matches a command. An explicit command-help option requests details for that command. Search and command-help options cannot be used together. Supplying any positional query together with either option is also rejected.

Unknown terms produce no output when no applicable search mode is enabled. Lookup errors are opt-in with `.Errors(...)`; enabling it uses defaults for omitted callbacks. Return `null` or an empty string from an individual callback to suppress that message.

## Format with context

`CommandHelpContext` exposes:

- `Command`: the command currently being formatted.
- `SearchTerm`: the positional or option query, or `null` for the overview.
- `IsAccessible` and `AccessFailure`: the result of this manager's caller permission and availability checks.
- `MatchedCommands`: a read-only list of path matches admitted by the access policy for this overview/search. A command-specific request has a one-command match list.
- `Syntax`: manual or generated syntax for this command, or `null` if syntax was not configured.
- `OptionPrefix`: the manager's configured flag/option prefix.

Formatters can use the command path, description, notes, invocation examples, syntax, arguments, flags, options, defaults, constraints, permission metadata, and echo/output metadata from `CommandDefinition`. Argument/option descriptions and sample-value examples are available from each binding definition. Missing strings are empty and missing collections are empty read-only lists. Command examples are complete command lines; binding examples illustrate values for one argument or option. They can also use `MatchedCommands` for related-command output or return no lines for a command they do not want shown.

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
- `HelpCommandAccessDecision.UseConfiguredError()` to format a permission or availability message.

Built-in policies are `HelpCommandAccessPolicies.HideBlocked` (the default), `ShowBlocked`, and `ExplainBlocked`. `ExplainBlocked` delegates to configured errors; call `.Errors()` to enable the default permission/unavailability text. A custom policy can inspect `AccessFailure.Kind` and `AccessFailure.Message` and choose any behavior:

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
    .Errors()
    .Build();

console.RegisterHelpCommand(help);
```

Access policies run only for commands that fail the manager's access checks. Availability checks receive the help input and a `null` parsed-command value, as they do during autocomplete. Exceptions from access checks or formatters follow the normal command execution failure path.

## Syntax and detailed formatting

The Detailed formatter omits empty sections and absent metadata. A command can set `.Syntax("teleport <player> [<zone>]")` manually, or call `.GenerateSyntaxWith()` to use the canonical parser-aware formatter. A custom `CommandSyntaxFormatter` can replace the canonical formatter. Manual and generated syntax are mutually exclusive. Generated syntax uses the active manager's option prefix and option value separator style, and includes optional positionals, flags, and options. When an optional positional and option share an ID, syntax presents them as alternate ways to supply the same value.

```csharp
new CommandBuilder("player.teleport")
    .Description("Move a player.")
    .Notes(new[] { "The destination must be loaded." })
    .Examples(new[] { "player.teleport Alex spawn" })
    .Argument<TeleportState>(state => state.Player, id: "player", name: "player", description: "Player to move.")
    .Argument<TeleportState>(state => state.Zone, id: "zone", name: "zone", examples: new[] { "spawn", "home" })
        .Optional()
    .Option<TeleportState>(state => state.Zone, id: "zone", name: "zone", description: "Destination zone.", examples: new[] { "spawn", "home" })
    .GenerateSyntaxWith()
    .Execute<TeleportState>((context, state) => CommandResult.Success())
    .Build();
```

## Errors, matching, and result order

`.Errors(...)` enables four independently customizable outcomes: unknown command target, search without visible results, permission denial, and command unavailability. Omitted callbacks use `HelpCommandErrorMessages` defaults; a callback returning `null` or an empty string suppresses that message. Errors are silent until `.Errors(...)` is called. A blocked command is hidden by default, and direct help treats it as unknown. The access policy decides whether blocked details may be disclosed; explicit `Respond(lines)` output takes precedence over configured error text.

Search defaults to case-aware substring matching against registered command paths. `.SearchWith(...)` accepts a predicate of `(command, term, isCaseSensitive)`. The manager limits candidates to its registered commands and applies access checks after matching. Results keep registration order by default; use `.OrderResults(HelpCommandResultOrder.Alphabetical)` or provide an `IComparer<CommandDefinition>`.

## Alias migration

Alias parameters use optional `IReadOnlyList<string>` collections instead of `params` strings. Replace `.EnableSearchOption("search", FormatSearch, "s")` with `.EnableSearchOption("search", FormatSearch, aliases: new[] { "s" })`; command `.Flag(...)` and `.Option(...)` calls use the same `aliases: new[] { ... }` shape.
