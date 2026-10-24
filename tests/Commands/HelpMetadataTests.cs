using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;

namespace Workes.ConsoleSystem.Tests.Commands;

public sealed class HelpMetadataTests
{
    [Test]
    public void CommandMetadata_SnapshotsCollectionsAndKeepsUnsetMetadataEmpty()
    {
        var notes = new List<string> { "Keep the player online." };
        var invocations = new List<string> { "teleport Mira spawn" };
        var values = new List<string> { "spawn", "home" };
        var aliases = new List<string> { "s" };
        CommandDefinition command = new CommandBuilder("teleport")
            .Description("Move a player.")
            .Notes(notes)
            .Examples(invocations)
            .Argument<TeleportState>(x => x.Player, "player", "player", "Target player.")
            .Argument<TeleportState>(x => x.Zone, "zone", "zone", examples: values).Optional()
            .Option<TeleportState>(x => x.Zone, "zone", "zone", examples: values)
            .Flag<TeleportState>(x => x.Safe, "safe", "safe", aliases: aliases)
            .Execute<TeleportState>((_, _) => CommandResult.Success())
            .Build();
        notes[0] = "changed";
        invocations.Clear();
        values[0] = "changed";
        aliases[0] = "changed";

        Assert.That(command.Notes, Is.EqualTo(new[] { "Keep the player online." }));
        Assert.That(command.Examples, Is.EqualTo(new[] { "teleport Mira spawn" }));
        Assert.That(command.Arguments[0].Description, Is.EqualTo("Target player."));
        Assert.That(command.Arguments[1].Examples, Is.EqualTo(new[] { "spawn", "home" }));
        Assert.That(command.Options[0].Examples, Is.EqualTo(new[] { "spawn", "home" }));
        Assert.That(command.Flags[0].Aliases, Is.EqualTo(new[] { "s" }));
        Assert.That(command.Syntax, Is.Null);
        Assert.That(command.Notes, Is.InstanceOf<IReadOnlyList<string>>());
    }

    [Test]
    public void CanonicalSyntax_UsesManagerParserSettingsAndSharedInputSources()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            CommandParsing = new CommandParsingOptions
            {
                FlagAndOptionPrefix = "-",
                OptionValueStyle = OptionValueStyle.EqualSeparated
            }
        });
        console.RegisterCommand(new CommandBuilder("teleport")
            .Argument<TeleportState>(x => x.Player, "player", "player")
            .Argument<TeleportState>(x => x.Zone, "zone", "zone").Optional()
            .Option<TeleportState>(x => x.Zone, "zone", "zone")
            .Flag<TeleportState>(x => x.Safe, "safe", "safe")
            .GenerateSyntaxWith()
            .Execute<TeleportState>((_, _) => CommandResult.Success())
            .Build());
        console.RegisterHelpCommand(new HelpCommandBuilder()
            .EnableCommandHelpOption("command")
            .FormatWith(HelpCommandFormatters.Detailed)
            .Build());

        string output = console.ExecuteCommand("help -command=teleport").Outputs.Single().PlainText;

        Assert.That(output, Does.Contain("Usage: teleport <player> [<zone> | -zone=<zone>] [-safe]"));
    }

    [Test]
    public void ManualAndGeneratedSyntax_AreMutuallyExclusive()
    {
        var manual = new CommandBuilder("manual").Syntax("manual <target>");
        Assert.Throws<InvalidOperationException>(() => manual.GenerateSyntaxWith());

        var generated = new CommandBuilder("generated").GenerateSyntaxWith();
        Assert.Throws<InvalidOperationException>(() => generated.Syntax("custom"));
    }

    [Test]
    public void CustomSyntaxFormatterReceivesTheCommand()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("custom")
            .GenerateSyntaxWith(context => "usage:" + context.Command.Path)
            .Execute(_ => CommandResult.Success()).Build());
        console.RegisterHelpCommand(new HelpCommandBuilder()
            .EnableCommandHelpOption("command")
            .FormatWith(HelpCommandFormatters.Detailed)
            .Build());

        Assert.That(console.ExecuteCommand("help --command custom").Outputs.Single().PlainText, Does.Contain("Usage: usage:custom"));
    }

    [Test]
    public void DefaultFormatterWorksWithoutConfiguration_AndDetailedFormatterSkipsMissingMetadata()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("status").Description("Show status.").Execute(_ => CommandResult.Success()).Build());
        console.RegisterHelpCommand(new HelpCommandBuilder().Build());
        Assert.That(console.ExecuteCommand("help").Outputs.Select(x => x.PlainText), Does.Contain("status : Show status."));

        console.RegisterHelpCommand(new HelpCommandBuilder("details")
            .EnableCommandHelpOption("command")
            .FormatWith(HelpCommandFormatters.Detailed)
            .Build());
        string detailed = console.ExecuteCommand("details --command status").Outputs.Single().PlainText;
        Assert.That(detailed, Does.Contain("status"));
        Assert.That(detailed, Does.Contain("Show status."));
        Assert.That(detailed, Does.Not.Contain("Notes:"));
        Assert.That(detailed, Does.Not.Contain("Examples:"));
    }

    [Test]
    public void ErrorsAreOptInAndCanBeOverriddenOrSuppressed()
    {
        var silent = new ConsoleManager();
        silent.RegisterHelpCommand(new HelpCommandBuilder().EnableCommandHelpOption("command").Build());
        Assert.That(silent.ExecuteCommand("help --command absent").Outputs, Is.Empty);

        var configured = new ConsoleManager();
        configured.RegisterHelpCommand(new HelpCommandBuilder().EnableCommandHelpOption("command").Errors(
            unknownCommand: context => "missing:" + context.Query,
            noSearchResults: _ => string.Empty).EnableSearchOption("search").Build());
        Assert.That(configured.ExecuteCommand("help --command absent").Outputs.Single().PlainText, Is.EqualTo("missing:absent"));
        Assert.That(configured.ExecuteCommand("help --search absent").Outputs, Is.Empty);
    }

    [Test]
    public void ErrorsRespectBlockedVisibilityAndExplicitPolicyResponses()
    {
        var hidden = new ConsoleManager();
        hidden.RegisterCommand(new CommandBuilder("admin.kick").RequirePermission("admin").Execute(_ => CommandResult.Success()).Build());
        hidden.RegisterHelpCommand(new HelpCommandBuilder().EnableCommandHelpOption("command").Errors().Build());
        Assert.That(hidden.ExecuteCommand("help --command admin.kick").Outputs.Single().PlainText,
            Is.EqualTo("No command named 'admin.kick' is available."));

        var response = new ConsoleManager();
        response.RegisterCommand(new CommandBuilder("server.restart")
            .AvailableWhen(_ => CommandAvailabilityResult.Unavailable("busy"))
            .Execute(_ => CommandResult.Success()).Build());
        response.RegisterHelpCommand(new HelpCommandBuilder()
            .EnableCommandHelpOption("command")
            .Errors(unavailable: _ => "configured")
            .AccessPolicy(_ => HelpCommandAccessDecision.Respond(new[] { "policy response" }))
            .Build());
        Assert.That(response.ExecuteCommand("help --command server.restart").Outputs.Single().PlainText, Is.EqualTo("policy response"));
    }

    [Test]
    public void SearchCanUseCustomMatcherAndAlphabeticalOrdering()
    {
        var console = new ConsoleManager();
        Register(console, "zeta");
        Register(console, "alpha");
        Register(console, "middle");
        console.RegisterHelpCommand(new HelpCommandBuilder()
            .EnableSearchOption("search")
            .SearchWith((command, term, _) => command.Path.Contains(term, StringComparison.OrdinalIgnoreCase))
            .OrderResults(HelpCommandResultOrder.Alphabetical)
            .FormatWith(context => new List<string> { context.Command.Path })
            .Build());

        Assert.That(console.ExecuteCommand("help --search a").Outputs.Select(x => x.PlainText), Is.EqualTo(new[] { "alpha", "zeta" }));

        var reversed = new ConsoleManager();
        Register(reversed, "zeta");
        Register(reversed, "alpha");
        reversed.RegisterHelpCommand(new HelpCommandBuilder()
            .EnableSearchOption("search")
            .OrderResults(System.Collections.Generic.Comparer<CommandDefinition>.Create((left, right) =>
                StringComparer.Ordinal.Compare(right.Path, left.Path)))
            .FormatWith(context => new List<string> { context.Command.Path })
            .Build());
        Assert.That(reversed.ExecuteCommand("help --search a").Outputs.Select(x => x.PlainText), Is.EqualTo(new[] { "zeta", "alpha" }));
    }

    private static void Register(ConsoleManager manager, string path) => manager.RegisterCommand(
        new CommandBuilder(path).Execute(_ => CommandResult.Success()).Build());

    private sealed class TeleportState
    {
        public string Player { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public bool Safe { get; set; }
    }
}
