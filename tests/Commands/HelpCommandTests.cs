using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Entries;
using Workes.ConsoleSystem.Permissions;

namespace Workes.ConsoleSystem.Tests.Commands;

public sealed class HelpCommandTests
{
    [Test]
    public void RegisterHelpCommand_OverviewFormatsEachRegisteredCommandAsOneBlock()
    {
        var console = new ConsoleManager();
        RegisterSimple(console, "player.give");
        RegisterSimple(console, "player.giveStat");
        RegisterSimple(console, "status");
        RegisterHelp(console, new HelpCommandBuilder().FormatWith(context => new List<string> { context.Command.Path }));
        RegisterSimple(console, "registered.after.help");

        CommandResult result = console.ExecuteCommand("help");

        Assert.That(result.Outputs.Select(output => output.PlainText), Is.EqualTo(new[]
        {
            "player.give", "player.giveStat", "status", "help", "registered.after.help"
        }));
        Assert.That(result.Outputs.All(output => output.Kind == CommandOutputKind.Block), Is.True);
    }

    [Test]
    public void RegisterHelpCommand_UsesCustomPathAndRecordsOneBlockPerFormattedCommand()
    {
        var console = new ConsoleManager();
        RegisterSimple(console, "status");
        RegisterHelp(console, new HelpCommandBuilder("assist")
            .FormatWith(context => new List<string> { context.Command.Path, context.Command.Description }));

        CommandResult result = console.ExecuteCommand("assist");

        Assert.That(result.Outputs.Count, Is.EqualTo(2));
        Assert.That(result.Outputs[0].PlainText, Is.EqualTo($"status{Environment.NewLine}"));
        Assert.That(result.Outputs[1].PlainText, Is.EqualTo($"assist{Environment.NewLine}Shows help for registered commands."));
        Assert.That(console.History.Entries.OfType<CommandOutputEntry>().Count(), Is.EqualTo(2));
    }

    [Test]
    public void SearchPositional_UsesSubstringMatchEvenWhenQueryIsExactCommandPath()
    {
        var console = new ConsoleManager();
        RegisterSimple(console, "PlayerGive");
        RegisterSimple(console, "PlayerGiveStat");
        RegisterSimple(console, "Other");
        RegisterHelp(console, new HelpCommandBuilder()
            .FormatWith(context => new List<string> { context.Command.Path })
            .EnableSearchPositional());

        CommandResult result = console.ExecuteCommand("help PlayerGive");

        Assert.That(result.Outputs.Select(output => output.PlainText), Is.EqualTo(new[] { "PlayerGive", "PlayerGiveStat" }));
    }

    [Test]
    public void PositionalSearchAndCommandHelp_ExactPathUsesCommandFormatterAndOtherTermsSearch()
    {
        var console = new ConsoleManager();
        RegisterSimple(console, "player.give");
        RegisterSimple(console, "player.giveStat");
        RegisterHelp(console, new HelpCommandBuilder()
            .FormatWith(context => new List<string> { "overview:" + context.Command.Path })
            .EnableSearchPositional(context => new List<string> { "search:" + context.Command.Path })
            .EnableCommandHelpPositional(context => new List<string> { "command:" + context.Command.Path }));

        CommandResult exact = console.ExecuteCommand("help player.give");
        CommandResult search = console.ExecuteCommand("help give");

        Assert.That(exact.Outputs.Select(output => output.PlainText), Is.EqualTo(new[] { "command:player.give" }));
        Assert.That(search.Outputs.Select(output => output.PlainText), Is.EqualTo(new[] { "search:player.give", "search:player.giveStat" }));
    }

    [Test]
    public void SearchOption_UsesConfiguredNameAndAlias()
    {
        var console = new ConsoleManager();
        RegisterSimple(console, "player.give");
        RegisterSimple(console, "player.giveStat");
        RegisterSimple(console, "other");
        RegisterHelp(console, new HelpCommandBuilder()
            .FormatWith(context => new List<string> { context.Command.Path })
            .EnableSearchOption("search", aliases: new[] { "s" }));

        CommandResult result = console.ExecuteCommand("help --s give");

        Assert.That(result.Outputs.Select(output => output.PlainText), Is.EqualTo(new[] { "player.give", "player.giveStat" }));
    }

    [Test]
    public void CommandHelpOption_UsesConfiguredNameAndItsFormatter()
    {
        var console = new ConsoleManager();
        RegisterSimple(console, "player.give");
        RegisterHelp(console, new HelpCommandBuilder()
            .FormatWith(context => new List<string> { "overview:" + context.Command.Path })
            .EnableCommandHelpOption("command", context => new List<string> { "detail:" + context.Command.Path }));

        CommandResult result = console.ExecuteCommand("help --command player.give");

        Assert.That(result.Outputs.Select(output => output.PlainText), Is.EqualTo(new[] { "detail:player.give" }));
    }

    [Test]
    public void CommandHelpPositional_WorksAsTheOnlyQueryMode()
    {
        var console = new ConsoleManager();
        RegisterSimple(console, "player.give");
        RegisterHelp(console, new HelpCommandBuilder()
            .FormatWith(context => new List<string> { "overview:" + context.Command.Path })
            .EnableCommandHelpPositional(context => new List<string> { "detail:" + context.Command.Path }));

        CommandResult exact = console.ExecuteCommand("help player.give");
        CommandResult unknown = console.ExecuteCommand("help absent");

        Assert.That(exact.Outputs.Select(output => output.PlainText), Is.EqualTo(new[] { "detail:player.give" }));
        Assert.That(unknown.Outputs, Is.Empty);
    }

    [Test]
    public void RegisteredHelpConfiguration_UsesEachManagersLiveCommandSet()
    {
        HelpCommandDefinition definition = new HelpCommandBuilder()
            .FormatWith(context => new List<string> { context.Command.Path })
            .Build();
        var first = new ConsoleManager();
        var second = new ConsoleManager();
        RegisterSimple(first, "first.only");
        RegisterSimple(second, "second.only");
        first.RegisterHelpCommand(definition);
        second.RegisterHelpCommand(definition);

        Assert.That(first.ExecuteCommand("help").Outputs.Select(output => output.PlainText),
            Does.Contain("first.only").And.Not.Contain("second.only"));
        Assert.That(second.ExecuteCommand("help").Outputs.Select(output => output.PlainText),
            Does.Contain("second.only").And.Not.Contain("first.only"));
    }

    [Test]
    public void OptionSearch_UsesManagerCaseSensitivitySetting()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            CommandParsing = new CommandParsingOptions { IsCaseSensitive = true }
        });
        RegisterSimple(console, "PlayerGive");
        RegisterHelp(console, new HelpCommandBuilder()
            .FormatWith(context => new List<string> { context.Command.Path })
            .EnableSearchOption("search"));

        CommandResult result = console.ExecuteCommand("help --search player");

        Assert.That(result.Outputs, Is.Empty);
    }

    [TestCase("help topic --search other")]
    [TestCase("help topic --command player.give")]
    [TestCase("help --search topic --command player.give")]
    public void MixedQuerySourcesAndModes_AreRejected(string input)
    {
        var console = new ConsoleManager();
        RegisterSimple(console, "player.give");
        RegisterHelp(console, new HelpCommandBuilder()
            .FormatWith(context => new List<string> { context.Command.Path })
            .EnableSearchPositional()
            .EnableSearchOption("search")
            .EnableCommandHelpPositional()
            .EnableCommandHelpOption("command"));

        Assert.That(console.TryExecuteCommand(input, out CommandResult result), Is.False);
        Assert.That(result.Failure!.Code, Is.EqualTo(ConsoleFailureCodes.CommandConstraintRejected));
    }

    [Test]
    public void SpecializedFormatters_FallBackToSharedFormatterIndependently()
    {
        var console = new ConsoleManager();
        RegisterSimple(console, "player.give");
        RegisterHelp(console, new HelpCommandBuilder()
            .FormatWith(context => new List<string> { "shared:" + context.Command.Path })
            .EnableSearchOption("search")
            .EnableCommandHelpOption("command"));

        CommandResult search = console.ExecuteCommand("help --search give");
        CommandResult detail = console.ExecuteCommand("help --command player.give");

        Assert.That(search.Outputs.Single().PlainText, Is.EqualTo("shared:player.give"));
        Assert.That(detail.Outputs.Single().PlainText, Is.EqualTo("shared:player.give"));
    }

    [Test]
    public void FormatterCanReturnEmptyAndReceivesVisibleMatchedCommandSet()
    {
        var console = new ConsoleManager();
        RegisterSimple(console, "player.give");
        RegisterSimple(console, "player.giveStat");
        var counts = new List<int>();
        RegisterHelp(console, new HelpCommandBuilder()
            .FormatWith(context =>
            {
                counts.Add(context.MatchedCommands.Count);
                return context.Command.Path == "player.giveStat"
                    ? new List<string>()
                    : new List<string> { context.Command.Path };
            })
            .EnableSearchOption("search"));

        CommandResult result = console.ExecuteCommand("help --search give");

        Assert.That(result.Outputs.Select(output => output.PlainText), Is.EqualTo(new[] { "player.give" }));
        Assert.That(counts, Is.EqualTo(new[] { 2, 2 }));
    }

    [Test]
    public void AccessPolicy_HidesBlockedCommandsByDefaultAndCanExplainThem()
    {
        var console = new ConsoleManager();
        RegisterProtected(console, "admin.kick");
        RegisterSimple(console, "status");
        RegisterHelp(console, new HelpCommandBuilder()
            .FormatWith(context => new List<string> { context.Command.Path })
            .EnableSearchOption("search")
            .EnableCommandHelpOption("command"));

        CommandResult overview = console.ExecuteCommand("help");
        CommandResult deniedDirect = console.ExecuteCommand("help --command admin.kick");

        Assert.That(overview.Outputs.Select(output => output.PlainText), Does.Not.Contain("admin.kick"));
        Assert.That(deniedDirect.Outputs, Is.Empty);

        var explainingConsole = new ConsoleManager();
        RegisterProtected(explainingConsole, "admin.kick");
        RegisterHelp(explainingConsole, new HelpCommandBuilder()
            .FormatWith(context => new List<string> { context.Command.Path })
            .EnableCommandHelpOption("command")
            .AccessPolicy(HelpCommandAccessPolicies.ExplainBlocked));

        CommandResult explained = explainingConsole.ExecuteCommand("help --command admin.kick");
        Assert.That(explained.Outputs.Single().PlainText, Is.EqualTo("You do not have access to command 'admin.kick'."));
    }

    [Test]
    public void AccessPolicyCanShowBlockedCommandToCustomFormatter()
    {
        var console = new ConsoleManager();
        RegisterProtected(console, "admin.kick");
        RegisterHelp(console, new HelpCommandBuilder()
            .FormatWith(context => new List<string> { context.Command.Path + ":" + context.IsAccessible })
            .EnableSearchOption("search")
            .AccessPolicy(HelpCommandAccessPolicies.ShowBlocked));

        CommandResult result = console.ExecuteCommand("help --search admin");

        Assert.That(result.Outputs.Single().PlainText, Is.EqualTo("admin.kick:False"));
    }

    [Test]
    public void AccessPolicyCanReturnCustomLinesForUnavailableCommands()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("server.restart")
            .AvailableWhen(_ => CommandAvailabilityResult.Unavailable("Server is starting."))
            .Execute(_ => CommandResult.Success())
            .Build());
        RegisterHelp(console, new HelpCommandBuilder()
            .FormatWith(context => new List<string> { context.Command.Path })
            .EnableCommandHelpOption("command")
            .AccessPolicy(context =>
            {
                Assert.That(context.AccessFailure.Kind, Is.EqualTo(ConsoleFailureKind.CommandAvailability));
                return HelpCommandAccessDecision.Respond(new[] { "Unavailable by policy." });
            }));

        CommandResult result = console.ExecuteCommand("help --command server.restart");

        Assert.That(result.Outputs.Single().PlainText, Is.EqualTo("Unavailable by policy."));
    }

    [Test]
    public void Build_RequiresSharedFormatterAndManagerRejectsDuplicateHelpPath()
    {
        Assert.Throws<InvalidOperationException>(() => new HelpCommandBuilder().Build());

        var console = new ConsoleManager();
        RegisterHelp(console, new HelpCommandBuilder().FormatWith(_ => new List<string>()));
        Assert.Throws<InvalidOperationException>(() => RegisterHelp(console, new HelpCommandBuilder().FormatWith(_ => new List<string>())));
    }

    [Test]
    public void HelpPublicSignatures_DoNotExposeContentSystemTypes()
    {
        var method = typeof(ConsoleManager).GetMethod(nameof(ConsoleManager.RegisterHelpCommand))!;

        Assert.That(method.ReturnType.Namespace, Is.Not.EqualTo("Workes.ContentSystem"));
        Assert.That(method.GetParameters().Single().ParameterType.Namespace, Is.Not.EqualTo("Workes.ContentSystem"));
        Assert.That(typeof(CommandHelpContext).GetProperties().Select(property => property.PropertyType.Namespace),
            Does.Not.Contain("Workes.ContentSystem"));
    }

    private static void RegisterHelp(ConsoleManager console, HelpCommandBuilder builder)
    {
        console.RegisterHelpCommand(builder.Build());
    }

    private static void RegisterSimple(ConsoleManager console, string path)
    {
        console.RegisterCommand(new CommandBuilder(path)
            .Execute(_ => CommandResult.Success())
            .Build());
    }

    private static void RegisterProtected(ConsoleManager console, string path)
    {
        console.RegisterCommand(new CommandBuilder(path)
            .RequirePermission("admin")
            .Execute(_ => CommandResult.Success())
            .Build());
    }
}
