using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;

namespace Workes.ConsoleSystem.Tests.Commands;

public sealed class CommandParsingTests
{
    [Test]
    public void ParseCommand_SimpleRegisteredCommandByPath_Succeeds()
    {
        var console = new ConsoleManager();
        CommandDefinition command = SimpleCommand("noclip");
        console.RegisterCommand(command);

        CommandParseResult result = console.ParseCommand("noclip");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Input, Is.EqualTo("noclip"));
        Assert.That(result.Command!.Definition, Is.SameAs(command));
        Assert.That(result.Command.State, Is.Null);
        Assert.That(result.Failure, Is.Null);
    }

    [Test]
    public void ParseCommand_UnknownCommand_Fails()
    {
        var console = new ConsoleManager();

        CommandParseResult result = console.ParseCommand("missing");

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Command, Is.Null);
        Assert.That(result.Failure!.Kind, Is.EqualTo(ConsoleFailureKind.CommandParsing));
        Assert.That(result.Failure.Code, Is.EqualTo(ConsoleFailureCodes.CommandUnknown));
    }

    [Test]
    public void ParseCommand_MatchesPathCaseInsensitivelyByDefault()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(SimpleCommand("noclip"));

        CommandParseResult result = console.ParseCommand("NOCLIP");

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void ParseCommand_WhenCaseSensitive_RequiresExactPathCase()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            CommandParsing = new CommandParsingOptions
            {
                IsCaseSensitive = true
            }
        });
        console.RegisterCommand(SimpleCommand("noclip"));

        CommandParseResult result = console.ParseCommand("NOCLIP");

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure!.Code, Is.EqualTo(ConsoleFailureCodes.CommandUnknown));
    }

    [Test]
    public void ParseCommand_RequiredPositionalArguments_BindIntoTypedState()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("give")
            .Argument<GiveState>(x => x.Player, "player", "player")
            .Argument<GiveState>(x => x.Item, "item", "item")
            .Argument<GiveState>(x => x.Amount, "amount", "amount")
            .Execute<GiveState>((ctx, state) => CommandResult.Success())
            .Build());

        CommandParseResult result = console.ParseCommand("give Anthony5172 gold 10");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Command!.Arguments["player"], Is.EqualTo("Anthony5172"));
        Assert.That(result.Command.Arguments["item"], Is.EqualTo("gold"));
        Assert.That(result.Command.Arguments["amount"], Is.EqualTo(10));
        Assert.That(result.Command.GetState<GiveState>()!.Amount, Is.EqualTo(10));
    }

    [Test]
    public void ParseCommand_MissingRequiredPositionalArgument_Fails()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(GiveCommand());

        CommandParseResult result = console.ParseCommand("give Anthony5172 gold");

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure!.Code, Is.EqualTo(ConsoleFailureCodes.CommandArgumentMissing));
    }

    [Test]
    public void ParseCommand_ExtraPositionalToken_Fails()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(GiveCommand());

        CommandParseResult result = console.ParseCommand("give Anthony5172 gold 10 extra");

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure!.Code, Is.EqualTo(ConsoleFailureCodes.CommandArgumentUnexpected));
    }

    [Test]
    public void ParseCommand_FlagsUseDefaultPrefix()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(RestartCommand());

        CommandParseResult result = console.ParseCommand("server.restart maintenance --ignore-players");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Command!.Flags["ignore-players"], Is.True);
        Assert.That(result.Command.GetState<RestartState>()!.IgnorePlayers, Is.True);
    }

    [Test]
    public void ParseCommand_FlagsAndOptionsUseCustomPrefix()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            CommandParsing = new CommandParsingOptions
            {
                FlagAndOptionPrefix = "/"
            }
        });
        console.RegisterCommand(RestartCommand());

        CommandParseResult result = console.ParseCommand("server.restart maintenance /ignore-players /delay 5");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Command!.Flags["ignore-players"], Is.True);
        Assert.That(result.Command.Options["delay"], Is.EqualTo(5));
    }

    [Test]
    public void ParseCommand_SpaceSeparatedOptions_AreAcceptedByDefault()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(RestartCommand());

        CommandParseResult result = console.ParseCommand("server.restart maintenance --delay 5");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Command!.Options["delay"], Is.EqualTo(5));
    }

    [Test]
    public void ParseCommand_EqualSeparatedOptions_AreAcceptedWhenConfigured()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            CommandParsing = new CommandParsingOptions
            {
                OptionValueStyle = OptionValueStyle.EqualSeparated
            }
        });
        console.RegisterCommand(RestartCommand());

        CommandParseResult result = console.ParseCommand("server.restart maintenance --delay=5");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Command!.Options["delay"], Is.EqualTo(5));
    }

    [Test]
    public void ParseCommand_AnySeparatedOptions_AcceptBothStyles()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            CommandParsing = new CommandParsingOptions
            {
                OptionValueStyle = OptionValueStyle.AnySeparated
            }
        });
        console.RegisterCommand(RestartCommand());

        CommandParseResult spaceResult = console.ParseCommand("server.restart maintenance --delay 5");
        CommandParseResult equalResult = console.ParseCommand("server.restart maintenance --delay=6");

        Assert.That(spaceResult.IsSuccess, Is.True);
        Assert.That(equalResult.IsSuccess, Is.True);
        Assert.That(spaceResult.Command!.Options["delay"], Is.EqualTo(5));
        Assert.That(equalResult.Command!.Options["delay"], Is.EqualTo(6));
    }

    [Test]
    public void ParseCommand_DisallowedOptionSyntax_Fails()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(RestartCommand());

        CommandParseResult result = console.ParseCommand("server.restart maintenance --delay=5");

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure!.Code, Is.EqualTo(ConsoleFailureCodes.CommandOptionValueSyntaxInvalid));
        Assert.That(result.Input, Is.EqualTo("server.restart maintenance --delay=5"));
    }

    [Test]
    public void ParseCommand_UnknownFlagOrOption_Fails()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(RestartCommand());

        CommandParseResult result = console.ParseCommand("server.restart maintenance --now");

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure!.Code, Is.EqualTo(ConsoleFailureCodes.CommandMemberUnknown));
    }

    [Test]
    public void ParseCommand_DuplicateFlagOrOption_Fails()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(RestartCommand());

        CommandParseResult result = console.ParseCommand("server.restart maintenance --delay 5 --d 6");

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure!.Code, Is.EqualTo(ConsoleFailureCodes.CommandMemberDuplicate));
    }

    [Test]
    public void ParseCommand_MissingOptions_BindConfiguredDefaultAndTypeDefault()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("player.teleport")
            .Argument<TeleportState>(x => x.Player, "player", "player")
            .Option<TeleportState>(x => x.Zone, "zone", "zone")
            .Defaults(("zone", "spawn"))
            .Option<TeleportState>(x => x.Height, "height", "height")
            .Execute<TeleportState>((ctx, state) => CommandResult.Success())
            .Build());

        CommandParseResult result = console.ParseCommand("player.teleport Anthony5172");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Command!.Options, Is.Empty);
        Assert.That(result.Command.Values["zone"], Is.EqualTo("spawn"));
        Assert.That(result.Command.Values["height"], Is.EqualTo(0));
        Assert.That(result.Command.GetState<TeleportState>()!.Zone, Is.EqualTo("spawn"));
    }

    [Test]
    public void ParseCommand_ConvertsBoolNumericEnumAndNullableValues()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("configure")
            .Option<ConfigureState>(x => x.Enabled, "enabled", "enabled")
            .Option<ConfigureState>(x => x.Count, "count", "count")
            .Option<ConfigureState>(x => x.Mode, "mode", "mode")
            .Option<ConfigureState>(x => x.OptionalAmount, "amount", "amount")
            .Execute<ConfigureState>((ctx, state) => CommandResult.Success())
            .Build());

        CommandParseResult result = console.ParseCommand("configure --enabled TRUE --count 12 --mode Fast --amount 3.5");

        Assert.That(result.IsSuccess, Is.True);
        ConfigureState state = result.Command!.GetState<ConfigureState>()!;
        Assert.That(state.Enabled, Is.True);
        Assert.That(state.Count, Is.EqualTo(12));
        Assert.That(state.Mode, Is.EqualTo(ParseMode.Fast));
        Assert.That(state.OptionalAmount, Is.EqualTo(3.5m));
    }

    [Test]
    public void ParseCommand_UsesConfiguredBooleanLiteralAliases()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            CommandParsing = new CommandParsingOptions
            {
                BooleanLiterals = new BooleanLiteralOptions
                {
                    TrueLiterals = new[] { "yes", "on" },
                    FalseLiterals = new[] { "no", "off" }
                }
            }
        });
        console.RegisterCommand(new CommandBuilder("configure")
            .Option<ConfigureState>(x => x.Enabled, "enabled", "enabled")
            .Option<ConfigureState>(x => x.Count, "count", "count")
            .Option<ConfigureState>(x => x.Mode, "mode", "mode")
            .Option<ConfigureState>(x => x.OptionalAmount, "amount", "amount")
            .Execute<ConfigureState>((ctx, state) => CommandResult.Success())
            .Build());

        CommandParseResult result = console.ParseCommand("configure --enabled ON --count 12 --mode Fast --amount 3.5");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Command!.GetState<ConfigureState>()!.Enabled, Is.True);
    }

    [Test]
    public void ParseCommand_UnconfiguredBooleanLiteral_Fails()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            CommandParsing = new CommandParsingOptions
            {
                BooleanLiterals = new BooleanLiteralOptions
                {
                    TrueLiterals = new[] { "yes" },
                    FalseLiterals = new[] { "no" }
                }
            }
        });
        console.RegisterCommand(new CommandBuilder("configure")
            .Option<ConfigureState>(x => x.Enabled, "enabled", "enabled")
            .Option<ConfigureState>(x => x.Count, "count", "count")
            .Option<ConfigureState>(x => x.Mode, "mode", "mode")
            .Option<ConfigureState>(x => x.OptionalAmount, "amount", "amount")
            .Execute<ConfigureState>((ctx, state) => CommandResult.Success())
            .Build());

        CommandParseResult result = console.ParseCommand("configure --enabled true --count 12 --mode Fast --amount 3.5");

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure!.Kind, Is.EqualTo(ConsoleFailureKind.CommandBinding));
        Assert.That(result.Failure.Code, Is.EqualTo(ConsoleFailureCodes.CommandValueInvalid));
    }

    [Test]
    public void ParseCommand_QuotedStringsSupportSingleDoubleAndEscapes()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("say")
            .Argument<SayState>(x => x.Message, "message", "message")
            .Option<SayState>(x => x.Target, "target", "target")
            .Execute<SayState>((ctx, state) => CommandResult.Success())
            .Build());

        CommandParseResult result = console.ParseCommand("say \"hello \\\"world\\\"\" --target '--admin'");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Command!.GetState<SayState>()!.Message, Is.EqualTo("hello \"world\""));
        Assert.That(result.Command.GetState<SayState>()!.Target, Is.EqualTo("--admin"));
    }

    [Test]
    public void ParseCommand_UnclosedQuote_Fails()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(SimpleCommand("say"));

        CommandParseResult result = console.ParseCommand("say \"hello");

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure!.Code, Is.EqualTo(ConsoleFailureCodes.CommandQuoteUnclosed));
    }

    [Test]
    public void ParseCommand_InvalidConversion_Fails()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(GiveCommand());

        CommandParseResult result = console.ParseCommand("give Anthony5172 gold ten");

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure!.Code, Is.EqualTo(ConsoleFailureCodes.CommandValueInvalid));
    }

    [Test]
    public void RegisterCommand_DuplicateFlagOrOptionAliasesUseConfiguredCaseSensitivity()
    {
        var console = new ConsoleManager();
        CommandDefinition command = new CommandBuilder("server.restart")
            .Flag<RestartState>(x => x.IgnorePlayers, "ignore-players", "ignore-players", "i")
            .Option<RestartState>(x => x.DelaySeconds, "delay", "delay", "I")
            .Execute<RestartState>((ctx, state) => CommandResult.Success())
            .Build();

        Assert.Throws<InvalidOperationException>(() => console.RegisterCommand(command));
    }

    [Test]
    public void ParseCommand_FailureIncludesSourceSpanForUnknownCommand()
    {
        var console = new ConsoleManager();
        const string input = "  missing --now";

        CommandParseResult result = console.ParseCommand(input);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.SourceOffset, Is.EqualTo(2));
        Assert.That(result.SourceLength, Is.EqualTo("missing".Length));
        Assert.That(input.Substring(result.SourceOffset!.Value, result.SourceLength!.Value), Is.EqualTo("missing"));
    }

    [Test]
    public void ParseCommand_UnclosedQuoteIncludesSpanFromOpeningQuoteToEnd()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(SimpleCommand("say"));
        const string input = "say \"unfinished value";

        CommandParseResult result = console.ParseCommand(input);

        Assert.That(result.Failure!.Code, Is.EqualTo(ConsoleFailureCodes.CommandQuoteUnclosed));
        Assert.That(result.SourceOffset, Is.EqualTo(4));
        Assert.That(result.SourceLength, Is.EqualTo(input.Length - 4));
    }

    [Test]
    public void ParseCommand_InvalidValueIncludesSpanForOffendingToken()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(GiveCommand());
        const string input = "give Anthony5172 gold ten";

        CommandParseResult result = console.ParseCommand(input);

        Assert.That(result.Failure!.Code, Is.EqualTo(ConsoleFailureCodes.CommandValueInvalid));
        Assert.That(input.Substring(result.SourceOffset!.Value, result.SourceLength!.Value), Is.EqualTo("ten"));
    }

    [Test]
    public void ParseCommand_RequiresPositionalArgumentsBeforeNamedMembersByDefault()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("player.teleport")
            .Argument<TeleportState>(x => x.Player, "player", "player")
            .Option<TeleportState>(x => x.Zone, "zone", "zone")
            .Execute<TeleportState>((ctx, state) => CommandResult.Success())
            .Build());

        CommandParseResult result = console.ParseCommand("player.teleport --zone spawn");

        Assert.That(result.Failure!.Code, Is.EqualTo(ConsoleFailureCodes.CommandArgumentMissing));
    }

    [Test]
    public void ParseCommand_AllowsInterleavedFlagsAndSpaceSeparatedOptionsWhenEnabled()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            CommandParsing = new CommandParsingOptions
            {
                AllowInterleavedOptions = true
            }
        });
        console.RegisterCommand(MoveCommand());

        CommandParseResult result = console.ParseCommand("move Alex --safe --reason because spawn");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Command!.Arguments["player"], Is.EqualTo("Alex"));
        Assert.That(result.Command.Arguments["destination"], Is.EqualTo("spawn"));
        Assert.That(result.Command.Options["reason"], Is.EqualTo("because"));
        Assert.That(result.Command.Flags["safe"], Is.True);
        Assert.That(result.Command.GetState<MoveState>(), Is.EqualTo(new MoveState("Alex", "spawn", true, "because")));
    }

    [Test]
    public void ParseCommand_AllowsInterleavedEqualSeparatedOptionsWhenConfigured()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            CommandParsing = new CommandParsingOptions
            {
                AllowInterleavedOptions = true,
                OptionValueStyle = OptionValueStyle.EqualSeparated
            }
        });
        console.RegisterCommand(MoveCommand());

        CommandParseResult result = console.ParseCommand("move --reason=carefully Alex --safe spawn");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Command!.Arguments["player"], Is.EqualTo("Alex"));
        Assert.That(result.Command.Arguments["destination"], Is.EqualTo("spawn"));
        Assert.That(result.Command.Options["reason"], Is.EqualTo("carefully"));
        Assert.That(result.Command.Flags["safe"], Is.True);
    }

    [TestCase(false, "move Alex spawn --reason carefully --safe")]
    [TestCase(false, "move Alex spawn --safe --reason carefully")]
    [TestCase(true, "move Alex spawn --reason carefully --safe")]
    [TestCase(true, "move Alex spawn --safe --reason carefully")]
    public void ParseCommand_NamedMembersRemainReorderable(bool allowInterleavedOptions, string input)
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            CommandParsing = new CommandParsingOptions
            {
                AllowInterleavedOptions = allowInterleavedOptions
            }
        });
        console.RegisterCommand(MoveCommand());

        CommandParseResult result = console.ParseCommand(input);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Command!.Options["reason"], Is.EqualTo("carefully"));
        Assert.That(result.Command.Flags["safe"], Is.True);
    }

    [Test]
    public void ParseCommand_DisabledInterleavingRejectsPositionalsAfterNamedMembersWithSpan()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(MoveCommand());
        const string input = "move Alex spawn --safe extra";

        CommandParseResult result = console.ParseCommand(input);

        Assert.That(result.Failure!.Code, Is.EqualTo(ConsoleFailureCodes.CommandArgumentUnexpected));
        Assert.That(input.Substring(result.SourceOffset!.Value, result.SourceLength!.Value), Is.EqualTo("extra"));
    }

    [Test]
    public void ParseCommand_QuotedPrefixValueRemainsPositional()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("say")
            .Argument<SingleMessageState>(x => x.Message, "message", "message")
            .Execute<SingleMessageState>((ctx, state) => CommandResult.Success())
            .Build());

        CommandParseResult result = console.ParseCommand("say \"--literal\"");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Command!.Arguments["message"], Is.EqualTo("--literal"));
    }

    [Test]
    public void ParseCommand_DoesNotExecuteHandlerOrChangeEitherHistory()
    {
        var console = new ConsoleManager();
        int handlerCalls = 0;
        console.RegisterCommand(new CommandBuilder("status")
            .Execute(_ =>
            {
                handlerCalls++;
                return CommandResult.Success();
            })
            .Build());

        CommandParseResult result = console.ParseCommand("status");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(handlerCalls, Is.Zero);
        Assert.That(console.History.Entries, Is.Empty);
        Assert.That(console.CommandHistory.Entries, Is.Empty);
    }

    private static CommandDefinition SimpleCommand(string path)
    {
        return new CommandBuilder(path)
            .Execute(ctx => CommandResult.Success())
            .Build();
    }

    private static CommandDefinition GiveCommand()
    {
        return new CommandBuilder("give")
            .Argument<GiveState>(x => x.Player, "player", "player")
            .Argument<GiveState>(x => x.Item, "item", "item")
            .Argument<GiveState>(x => x.Amount, "amount", "amount")
            .Execute<GiveState>((ctx, state) => CommandResult.Success())
            .Build();
    }

    private static CommandDefinition RestartCommand()
    {
        return new CommandBuilder("server.restart")
            .Argument<RestartState>(x => x.Reason, "reason", "reason")
            .Flag<RestartState>(x => x.IgnorePlayers, "ignore-players", "ignore-players", "i")
            .Option<RestartState>(x => x.DelaySeconds, "delay", "delay", "d")
            .Defaults(("delay", 10))
            .Execute<RestartState>((ctx, state) => CommandResult.Success())
            .Build();
    }

    private static CommandDefinition MoveCommand()
    {
        return new CommandBuilder("move")
            .Argument<MoveState>(x => x.Player, "player", "player")
            .Argument<MoveState>(x => x.Destination, "destination", "destination")
            .Flag<MoveState>(x => x.Safe, "safe", "safe")
            .Option<MoveState>(x => x.Reason, "reason", "reason")
            .Execute<MoveState>((ctx, state) => CommandResult.Success())
            .Build();
    }

    private sealed record GiveState(string Player, string Item, int Amount);

    private sealed record RestartState(string Reason, bool IgnorePlayers, int DelaySeconds);

    private sealed record TeleportState(string Player, string Zone, int Height);

    private sealed record ConfigureState(bool Enabled, int Count, ParseMode Mode, decimal? OptionalAmount);

    private sealed record SayState(string Message, string Target);

    private sealed record SingleMessageState(string Message);

    private sealed record MoveState(string Player, string Destination, bool Safe, string? Reason);

    private enum ParseMode
    {
        Slow,
        Fast
    }
}
