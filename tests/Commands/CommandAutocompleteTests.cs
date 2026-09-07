using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;

namespace Workes.ConsoleSystem.Tests.Commands;

public sealed class CommandAutocompleteTests
{
    [Test]
    public void EmptyInput_SuggestsAllCommandPaths()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(SimpleCommand("noclip"));
        console.RegisterCommand(SimpleCommand("help"));

        var result = console.GetAutocomplete(string.Empty, 0);

        Assert.That(result.ReplacementStart, Is.EqualTo(0));
        Assert.That(result.ReplacementLength, Is.EqualTo(0));
        Assert.That(result.Candidates.Select(x => x.Text), Is.EqualTo(new[] { "noclip", "help" }));
        Assert.That(result.Candidates.All(x => x.Kind == CommandAutocompleteCandidateKind.CommandPath), Is.True);
    }

    [Test]
    public void PartialCommandPath_UsesConfiguredCaseSensitivity()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            CommandParsing = new CommandParsingOptions
            {
                IsCaseSensitive = true
            }
        });
        console.RegisterCommand(SimpleCommand("noclip"));
        console.RegisterCommand(SimpleCommand("NOCLIP"));

        var result = console.GetAutocomplete("NO", 2);

        Assert.That(result.Candidates.Select(x => x.Text), Is.EqualTo(new[] { "NOCLIP" }));
    }

    [Test]
    public void CursorInsideInput_ControlsReplacementRange()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(SimpleCommand("server.restart"));

        var result = console.GetAutocomplete("server.re", 6);

        Assert.That(result.ReplacementStart, Is.EqualTo(0));
        Assert.That(result.ReplacementLength, Is.EqualTo(9));
        Assert.That(result.Candidates.Select(x => x.Text), Is.EqualTo(new[] { "server.restart" }));
    }

    [Test]
    public void ApplyCandidate_ReplacesResultRange()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(SimpleCommand("server.restart"));

        var result = console.GetAutocomplete("server.re", "server.re".Length);

        Assert.That(result.Apply(result.Candidates[0]), Is.EqualTo("server.restart"));
        Assert.That(result.Apply(0), Is.EqualTo("server.restart"));
    }

    [Test]
    public void ApplyCandidate_InvalidInputsThrow()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(SimpleCommand("noclip"));
        var result = console.GetAutocomplete("no", 2);

        Assert.Throws<ArgumentNullException>(() => result.Apply((CommandAutocompleteCandidate)null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.Apply(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.Apply(result.Candidates.Count));
    }

    [Test]
    public void DotSegmentPathCompletion_SuggestsFirstPathSegments()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            Autocomplete = new CommandAutocompleteOptions
            {
                PathCompletionMode = CommandPathCompletionMode.DotSegment
            }
        });
        console.RegisterCommand(SimpleCommand("Player.AddItem"));
        console.RegisterCommand(SimpleCommand("Player.ModAv"));
        console.RegisterCommand(SimpleCommand("Platoon.Spawn"));

        var result = console.GetAutocomplete("Pl", 2);

        Assert.That(result.ReplacementStart, Is.EqualTo(0));
        Assert.That(result.ReplacementLength, Is.EqualTo(2));
        Assert.That(result.Candidates.Select(x => x.Text), Is.EqualTo(new[] { "Player", "Platoon" }));
        Assert.That(result.Candidates.Select(x => x.DisplayText), Is.EqualTo(new[] { "Player.", "Platoon." }));
        Assert.That(result.Apply(0), Is.EqualTo("Player"));
    }

    [Test]
    public void DotSegmentPathCompletion_SuggestsCurrentSegmentAfterDot()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            Autocomplete = new CommandAutocompleteOptions
            {
                PathCompletionMode = CommandPathCompletionMode.DotSegment
            }
        });
        console.RegisterCommand(SimpleCommand("Player.AddItem"));
        console.RegisterCommand(SimpleCommand("Player.ModAv"));
        console.RegisterCommand(SimpleCommand("Player.SetAv"));
        console.RegisterCommand(SimpleCommand("Platoon.Spawn"));

        var result = console.GetAutocomplete("Player.Add", "Player.Add".Length);

        Assert.That(result.ReplacementStart, Is.EqualTo("Player.".Length));
        Assert.That(result.ReplacementLength, Is.EqualTo("Add".Length));
        Assert.That(result.Candidates.Select(x => x.Text), Is.EqualTo(new[] { "AddItem" }));
        Assert.That(result.Candidates[0].DisplayText, Is.EqualTo("Player.AddItem"));
        Assert.That(result.Apply(0), Is.EqualTo("Player.AddItem"));
    }

    [Test]
    public void DotSegmentPathCompletion_SupportsMoreThanTwoPathSegments()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            Autocomplete = new CommandAutocompleteOptions
            {
                PathCompletionMode = CommandPathCompletionMode.DotSegment
            }
        });
        console.RegisterCommand(SimpleCommand("Player.Inventory.SetModifier"));
        console.RegisterCommand(SimpleCommand("Player.Inventory.Clear"));
        console.RegisterCommand(SimpleCommand("Player.Stats.SetModifier"));

        var secondSegment = console.GetAutocomplete("Player.In", "Player.In".Length);
        var thirdSegment = console.GetAutocomplete("Player.Inventory.Set", "Player.Inventory.Set".Length);

        Assert.That(secondSegment.ReplacementStart, Is.EqualTo("Player.".Length));
        Assert.That(secondSegment.ReplacementLength, Is.EqualTo("In".Length));
        Assert.That(secondSegment.Candidates.Select(x => x.Text), Is.EqualTo(new[] { "Inventory" }));
        Assert.That(secondSegment.Candidates[0].DisplayText, Is.EqualTo("Player.Inventory."));
        Assert.That(secondSegment.Apply(0), Is.EqualTo("Player.Inventory"));

        Assert.That(thirdSegment.ReplacementStart, Is.EqualTo("Player.Inventory.".Length));
        Assert.That(thirdSegment.ReplacementLength, Is.EqualTo("Set".Length));
        Assert.That(thirdSegment.Candidates.Select(x => x.Text), Is.EqualTo(new[] { "SetModifier" }));
        Assert.That(thirdSegment.Candidates[0].DisplayText, Is.EqualTo("Player.Inventory.SetModifier"));
        Assert.That(thirdSegment.Apply(0), Is.EqualTo("Player.Inventory.SetModifier"));
    }

    [Test]
    public void DotSegmentPathCompletion_DoesNotSuggestAlreadyCompleteSegment()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            Autocomplete = new CommandAutocompleteOptions
            {
                PathCompletionMode = CommandPathCompletionMode.DotSegment
            }
        });
        console.RegisterCommand(SimpleCommand("Player.AddItem"));

        var result = console.GetAutocomplete("Player.AddItem", "Player.AddItem".Length);

        Assert.That(result.Candidates, Is.Empty);
    }

    [Test]
    public void FullPathCompletion_RemainsDefault()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(SimpleCommand("Player.AddItem"));
        console.RegisterCommand(SimpleCommand("Player.ModAv"));

        var result = console.GetAutocomplete("Pl", 2);

        Assert.That(result.Candidates.Select(x => x.Text), Is.EqualTo(new[] { "Player.AddItem", "Player.ModAv" }));
    }

    [Test]
    public void AfterRequiredArguments_SuggestsUnusedFlagsOptionsAndAliasesWithPrefix()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(RestartCommand());

        var result = console.GetAutocomplete("server.restart maintenance --", "server.restart maintenance --".Length);

        Assert.That(result.Candidates.Select(x => x.Text), Is.EqualTo(new[] { "--ignore-players", "--i", "--delay", "--d" }));
        Assert.That(result.Candidates.Select(x => x.Kind), Is.EqualTo(new[]
        {
            CommandAutocompleteCandidateKind.Flag,
            CommandAutocompleteCandidateKind.Flag,
            CommandAutocompleteCandidateKind.Option,
            CommandAutocompleteCandidateKind.Option
        }));
    }

    [Test]
    public void UsedFlagsAndOptions_AreNotSuggestedAgain()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(RestartCommand());

        var result = console.GetAutocomplete("server.restart maintenance --ignore-players --", "server.restart maintenance --ignore-players --".Length);

        Assert.That(result.Candidates.Select(x => x.Text), Is.EqualTo(new[] { "--delay", "--d" }));
    }

    [Test]
    public void CustomPrefix_IsIncludedInInsertedMemberText()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            CommandParsing = new CommandParsingOptions
            {
                FlagAndOptionPrefix = "-"
            }
        });
        console.RegisterCommand(RestartCommand());

        var result = console.GetAutocomplete("server.restart maintenance -", "server.restart maintenance -".Length);

        Assert.That(result.Candidates.Select(x => x.Text), Does.Contain("-delay"));
    }

    [Test]
    public void PositionalArgumentValue_UsesProvider()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("player.give")
            .Argument<GiveState>(x => x.Player, "player")
                .ValueCandidates(ctx => new[] { "@me", "Anthony5172" })
            .Argument<GiveState>(x => x.Item, "item")
            .Execute<GiveState>((ctx, state) => new CommandResult())
            .Build());

        var result = console.GetAutocomplete("player.give @", "player.give @".Length);

        Assert.That(result.ReplacementStart, Is.EqualTo("player.give ".Length));
        Assert.That(result.ReplacementLength, Is.EqualTo(1));
        Assert.That(result.Candidates.Select(x => x.Text), Is.EqualTo(new[] { "@me" }));
        Assert.That(result.Candidates[0].Kind, Is.EqualTo(CommandAutocompleteCandidateKind.ArgumentValue));
    }

    [Test]
    public void OptionValue_UsesProviderForSpaceSeparatedOptions()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("server.restart")
            .Argument<RestartState>(x => x.Reason, "reason")
            .Option<RestartState>(x => x.DelaySeconds, "delay", "d")
                .ValueCandidates(ctx => new[] { "10", "30", "60" })
            .Execute<RestartState>((ctx, state) => new CommandResult())
            .Build());

        var result = console.GetAutocomplete("server.restart maintenance --delay 3", "server.restart maintenance --delay 3".Length);

        Assert.That(result.Candidates.Select(x => x.Text), Is.EqualTo(new[] { "30" }));
        Assert.That(result.Candidates[0].Kind, Is.EqualTo(CommandAutocompleteCandidateKind.OptionValue));
    }

    [Test]
    public void OptionValue_UsesProviderForEqualSeparatedOptions()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            CommandParsing = new CommandParsingOptions
            {
                OptionValueStyle = OptionValueStyle.EqualSeparated
            }
        });
        console.RegisterCommand(new CommandBuilder("server.restart")
            .Argument<RestartState>(x => x.Reason, "reason")
            .Option<RestartState>(x => x.DelaySeconds, "delay", "d")
                .ValueCandidates(ctx => new[] { "10", "30", "60" })
            .Execute<RestartState>((ctx, state) => new CommandResult())
            .Build());

        var result = console.GetAutocomplete("server.restart maintenance --delay=3", "server.restart maintenance --delay=3".Length);

        Assert.That(result.ReplacementStart, Is.EqualTo("server.restart maintenance --delay=".Length));
        Assert.That(result.ReplacementLength, Is.EqualTo(1));
        Assert.That(result.Candidates.Select(x => x.Text), Is.EqualTo(new[] { "30" }));
    }

    [Test]
    public void OptionValue_UsesProviderForAnySeparatedOptions()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            CommandParsing = new CommandParsingOptions
            {
                OptionValueStyle = OptionValueStyle.AnySeparated
            }
        });
        console.RegisterCommand(new CommandBuilder("server.restart")
            .Argument<RestartState>(x => x.Reason, "reason")
            .Option<RestartState>(x => x.DelaySeconds, "delay", "d")
                .ValueCandidates(ctx => new[] { "10", "30", "60" })
            .Execute<RestartState>((ctx, state) => new CommandResult())
            .Build());

        var result = console.GetAutocomplete("server.restart maintenance --delay=", "server.restart maintenance --delay=".Length);

        Assert.That(result.Candidates.Select(x => x.Text), Is.EqualTo(new[] { "10", "30", "60" }));
    }

    [Test]
    public void MissingValueProvider_ReturnsNoCandidatesForValuePosition()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("player.give")
            .Argument<GiveState>(x => x.Player, "player")
            .Argument<GiveState>(x => x.Item, "item")
            .Execute<GiveState>((ctx, state) => new CommandResult())
            .Build());

        var result = console.GetAutocomplete("player.give A", "player.give A".Length);

        Assert.That(result.Candidates, Is.Empty);
    }

    [Test]
    public void IncompleteQuotes_StillUseBestEffortCandidates()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("say")
            .Argument<SayState>(x => x.Message, "message")
                .ValueCandidates(ctx => new[] { "hello world", "hello player" })
            .Execute<SayState>((ctx, state) => new CommandResult())
            .Build());

        var result = console.GetAutocomplete("say \"hello", "say \"hello".Length);

        Assert.That(result.ReplacementStart, Is.EqualTo("say \"".Length));
        Assert.That(result.ReplacementLength, Is.EqualTo(5));
        Assert.That(result.Candidates.Select(x => x.Text), Is.EqualTo(new[] { "hello world", "hello player" }));
    }

    [TestCase(-1)]
    [TestCase(2)]
    public void InvalidCursorIndex_Throws(int cursorIndex)
    {
        var console = new ConsoleManager();

        Assert.Throws<ArgumentOutOfRangeException>(() => console.GetAutocomplete("x", cursorIndex));
    }

    [Test]
    public void Autocomplete_HasNoHistorySideEffects()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(SimpleCommand("noclip"));

        console.GetAutocomplete("no", 2);

        Assert.That(console.History.Entries, Is.Empty);
        Assert.That(console.CommandHistory.Entries, Is.Empty);
    }

    [Test]
    public void ValueCandidates_ReceivesContext()
    {
        CommandAutocompleteContext? capturedContext = null;
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("player.give")
            .Argument<GiveState>(x => x.Player, "player")
            .Argument<GiveState>(x => x.Item, "item")
                .ValueCandidates(ctx =>
                {
                    capturedContext = ctx;
                    return new[] { "wood" };
                })
            .Execute<GiveState>((ctx, state) => new CommandResult())
            .Build());

        console.GetAutocomplete("player.give @me w", "player.give @me w".Length);

        Assert.That(capturedContext, Is.Not.Null);
        Assert.That(capturedContext!.Input, Is.EqualTo("player.give @me w"));
        Assert.That(capturedContext.CursorIndex, Is.EqualTo("player.give @me w".Length));
        Assert.That(capturedContext.PartialValue, Is.EqualTo("w"));
        Assert.That(capturedContext.Command.Path, Is.EqualTo("player.give"));
        Assert.That(capturedContext.Member.Name, Is.EqualTo("item"));
        Assert.That(capturedContext.TokensBeforeCursor, Is.EqualTo(new[] { "player.give", "@me", "w" }));
    }

    [Test]
    public void ValueCandidates_WhenUsedBeforeValueMember_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new CommandBuilder("noclip").ValueCandidates(ctx => Array.Empty<string>()));
    }

    [Test]
    public void ValueCandidates_WhenUsedAfterFlag_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new CommandBuilder("server.restart")
                .Flag<RestartState>(x => x.IgnorePlayers, "ignore-players")
                .ValueCandidates(ctx => Array.Empty<string>()));
    }

    [Test]
    public void ValueCandidates_WhenProviderIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CommandBuilder("server.restart")
                .Argument<RestartState>(x => x.Reason, "reason")
                .ValueCandidates(null!));
    }

    [Test]
    public void ValueCandidates_WhenUsedAfterExecute_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new CommandBuilder("server.restart")
                .Argument<RestartState>(x => x.Reason, "reason")
                .Execute<RestartState>((ctx, state) => new CommandResult())
                .ValueCandidates(ctx => Array.Empty<string>()));
    }

    [Test]
    public void ValueCandidates_WhenUsedAfterDescription_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new CommandBuilder("server.restart")
                .Argument<RestartState>(x => x.Reason, "reason")
                .Description("Restart the server.")
                .ValueCandidates(ctx => Array.Empty<string>()));
    }

    private static CommandDefinition SimpleCommand(string path)
    {
        return new CommandBuilder(path)
            .Execute(ctx => new CommandResult())
            .Build();
    }

    private static CommandDefinition RestartCommand()
    {
        return new CommandBuilder("server.restart")
            .Argument<RestartState>(x => x.Reason, "reason")
            .Flag<RestartState>(x => x.IgnorePlayers, "ignore-players", "i")
            .Option<RestartState>(x => x.DelaySeconds, "delay", "d")
            .Execute<RestartState>((ctx, state) => new CommandResult())
            .Build();
    }

    private sealed record RestartState(string Reason, bool IgnorePlayers, int DelaySeconds);

    private sealed record GiveState(string Player, string Item);

    private sealed record SayState(string Message);
}
