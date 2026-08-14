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

        Assert.That(result.Success, Is.True);
        Assert.That(result.Input, Is.EqualTo("noclip"));
        Assert.That(result.Command!.Definition, Is.SameAs(command));
        Assert.That(result.Command.State, Is.Null);
    }

    [Test]
    public void ParseCommand_UnknownCommand_Fails()
    {
        var console = new ConsoleManager();

        CommandParseResult result = console.ParseCommand("missing");

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error!.Code, Is.EqualTo(CommandParseErrorCode.UnknownCommand));
    }

    [Test]
    public void ParseCommand_MatchesPathCaseInsensitivelyByDefault()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(SimpleCommand("noclip"));

        CommandParseResult result = console.ParseCommand("NOCLIP");

        Assert.That(result.Success, Is.True);
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

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error!.Code, Is.EqualTo(CommandParseErrorCode.UnknownCommand));
    }

    [Test]
    public void ParseCommand_RequiredPositionalArguments_BindIntoTypedState()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("give")
            .Argument<GiveState>(x => x.Player, "player")
            .Argument<GiveState>(x => x.Item, "item")
            .Argument<GiveState>(x => x.Amount, "amount")
            .Execute<GiveState>((ctx, state) => new CommandResult())
            .Build());

        CommandParseResult result = console.ParseCommand("give Anthony5172 gold 10");

        Assert.That(result.Success, Is.True);
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

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error!.Code, Is.EqualTo(CommandParseErrorCode.MissingArgument));
    }

    [Test]
    public void ParseCommand_ExtraPositionalToken_Fails()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(GiveCommand());

        CommandParseResult result = console.ParseCommand("give Anthony5172 gold 10 extra");

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error!.Code, Is.EqualTo(CommandParseErrorCode.ExtraArgument));
    }

    [Test]
    public void ParseCommand_FlagsUseDefaultPrefix()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(RestartCommand());

        CommandParseResult result = console.ParseCommand("server.restart maintenance --ignore-players");

        Assert.That(result.Success, Is.True);
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

        Assert.That(result.Success, Is.True);
        Assert.That(result.Command!.Flags["ignore-players"], Is.True);
        Assert.That(result.Command.Options["delay"], Is.EqualTo(5));
    }

    [Test]
    public void ParseCommand_SpaceSeparatedOptions_AreAcceptedByDefault()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(RestartCommand());

        CommandParseResult result = console.ParseCommand("server.restart maintenance --delay 5");

        Assert.That(result.Success, Is.True);
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

        Assert.That(result.Success, Is.True);
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

        Assert.That(spaceResult.Success, Is.True);
        Assert.That(equalResult.Success, Is.True);
        Assert.That(spaceResult.Command!.Options["delay"], Is.EqualTo(5));
        Assert.That(equalResult.Command!.Options["delay"], Is.EqualTo(6));
    }

    [Test]
    public void ParseCommand_DisallowedOptionSyntax_Fails()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(RestartCommand());

        CommandParseResult result = console.ParseCommand("server.restart maintenance --delay=5");

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error!.Code, Is.EqualTo(CommandParseErrorCode.OptionValueSyntaxNotAllowed));
        Assert.That(result.Input, Is.EqualTo("server.restart maintenance --delay=5"));
    }

    [Test]
    public void ParseCommand_UnknownFlagOrOption_Fails()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(RestartCommand());

        CommandParseResult result = console.ParseCommand("server.restart maintenance --now");

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error!.Code, Is.EqualTo(CommandParseErrorCode.UnknownFlagOrOption));
    }

    [Test]
    public void ParseCommand_DuplicateFlagOrOption_Fails()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(RestartCommand());

        CommandParseResult result = console.ParseCommand("server.restart maintenance --delay 5 --d 6");

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error!.Code, Is.EqualTo(CommandParseErrorCode.DuplicateFlagOrOption));
    }

    [Test]
    public void ParseCommand_MissingOptions_BindConfiguredDefaultAndTypeDefault()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("player.teleport")
            .Argument<TeleportState>(x => x.Player, "player")
            .Option<TeleportState>(x => x.Zone, "zone")
            .Default("spawn")
            .Option<TeleportState>(x => x.Height, "height")
            .Execute<TeleportState>((ctx, state) => new CommandResult())
            .Build());

        CommandParseResult result = console.ParseCommand("player.teleport Anthony5172");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Command!.Options["zone"], Is.EqualTo("spawn"));
        Assert.That(result.Command.Options["height"], Is.EqualTo(0));
        Assert.That(result.Command.GetState<TeleportState>()!.Zone, Is.EqualTo("spawn"));
    }

    [Test]
    public void ParseCommand_ConvertsBoolNumericEnumAndNullableValues()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("configure")
            .Option<ConfigureState>(x => x.Enabled, "enabled")
            .Option<ConfigureState>(x => x.Count, "count")
            .Option<ConfigureState>(x => x.Mode, "mode")
            .Option<ConfigureState>(x => x.OptionalAmount, "amount")
            .Execute<ConfigureState>((ctx, state) => new CommandResult())
            .Build());

        CommandParseResult result = console.ParseCommand("configure --enabled TRUE --count 12 --mode Fast --amount 3.5");

        Assert.That(result.Success, Is.True);
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
            .Option<ConfigureState>(x => x.Enabled, "enabled")
            .Option<ConfigureState>(x => x.Count, "count")
            .Option<ConfigureState>(x => x.Mode, "mode")
            .Option<ConfigureState>(x => x.OptionalAmount, "amount")
            .Execute<ConfigureState>((ctx, state) => new CommandResult())
            .Build());

        CommandParseResult result = console.ParseCommand("configure --enabled ON --count 12 --mode Fast --amount 3.5");

        Assert.That(result.Success, Is.True);
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
            .Option<ConfigureState>(x => x.Enabled, "enabled")
            .Option<ConfigureState>(x => x.Count, "count")
            .Option<ConfigureState>(x => x.Mode, "mode")
            .Option<ConfigureState>(x => x.OptionalAmount, "amount")
            .Execute<ConfigureState>((ctx, state) => new CommandResult())
            .Build());

        CommandParseResult result = console.ParseCommand("configure --enabled true --count 12 --mode Fast --amount 3.5");

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error!.Code, Is.EqualTo(CommandParseErrorCode.InvalidValue));
    }

    [Test]
    public void ParseCommand_QuotedStringsSupportSingleDoubleAndEscapes()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("say")
            .Argument<SayState>(x => x.Message, "message")
            .Option<SayState>(x => x.Target, "target")
            .Execute<SayState>((ctx, state) => new CommandResult())
            .Build());

        CommandParseResult result = console.ParseCommand("say \"hello \\\"world\\\"\" --target '--admin'");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Command!.GetState<SayState>()!.Message, Is.EqualTo("hello \"world\""));
        Assert.That(result.Command.GetState<SayState>()!.Target, Is.EqualTo("--admin"));
    }

    [Test]
    public void ParseCommand_UnclosedQuote_Fails()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(SimpleCommand("say"));

        CommandParseResult result = console.ParseCommand("say \"hello");

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error!.Code, Is.EqualTo(CommandParseErrorCode.UnclosedQuote));
    }

    [Test]
    public void ParseCommand_InvalidConversion_Fails()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(GiveCommand());

        CommandParseResult result = console.ParseCommand("give Anthony5172 gold ten");

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error!.Code, Is.EqualTo(CommandParseErrorCode.InvalidValue));
    }

    [Test]
    public void RegisterCommand_DuplicateFlagOrOptionAliasesUseConfiguredCaseSensitivity()
    {
        var console = new ConsoleManager();
        CommandDefinition command = new CommandBuilder("server.restart")
            .Flag<RestartState>(x => x.IgnorePlayers, "ignore-players", "i")
            .Option<RestartState>(x => x.DelaySeconds, "delay", "I")
            .Execute<RestartState>((ctx, state) => new CommandResult())
            .Build();

        Assert.Throws<InvalidOperationException>(() => console.RegisterCommand(command));
    }

    private static CommandDefinition SimpleCommand(string path)
    {
        return new CommandBuilder(path)
            .Execute(ctx => new CommandResult())
            .Build();
    }

    private static CommandDefinition GiveCommand()
    {
        return new CommandBuilder("give")
            .Argument<GiveState>(x => x.Player, "player")
            .Argument<GiveState>(x => x.Item, "item")
            .Argument<GiveState>(x => x.Amount, "amount")
            .Execute<GiveState>((ctx, state) => new CommandResult())
            .Build();
    }

    private static CommandDefinition RestartCommand()
    {
        return new CommandBuilder("server.restart")
            .Argument<RestartState>(x => x.Reason, "reason")
            .Flag<RestartState>(x => x.IgnorePlayers, "ignore-players", "i")
            .Option<RestartState>(x => x.DelaySeconds, "delay", "d")
            .Default(10)
            .Execute<RestartState>((ctx, state) => new CommandResult())
            .Build();
    }

    private sealed record GiveState(string Player, string Item, int Amount);

    private sealed record RestartState(string Reason, bool IgnorePlayers, int DelaySeconds);

    private sealed record TeleportState(string Player, string Zone, int Height);

    private sealed record ConfigureState(bool Enabled, int Count, ParseMode Mode, decimal? OptionalAmount);

    private sealed record SayState(string Message, string Target);

    private enum ParseMode
    {
        Slow,
        Fast
    }
}
