using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Core;

namespace Workes.ConsoleSystem.Tests.Commands;

public sealed class CommandValidationTests
{
    [Test]
    public void ValidateCommand_WithoutConstraints_Succeeds()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(SimpleCommand("noclip"));

        CommandParseResult parse = console.ParseCommand("noclip");
        CommandValidationResult result = console.ValidateCommand(parse.Command!);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Command, Is.SameAs(parse.Command));
        Assert.That(result.Failure, Is.Null);
    }

    [Test]
    public void ValidateCommand_TypedConstraintFailure_ReturnsStructuredFailure()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("server.restart")
            .Argument<RestartState>(x => x.Reason, "reason")
            .Flag<RestartState>(x => x.IgnorePlayers, "ignore-players")
            .Option<RestartState>(x => x.DelaySeconds, "delay")
            .Constraint<RestartState>(
                "delay-ignore-players",
                "Delay cannot be combined with ignore players.",
                state => !state.IgnorePlayers || state.DelaySeconds == 0)
            .Execute<RestartState>((ctx, state) => new CommandResult())
            .Build());

        CommandParseResult parse = console.ParseCommand("server.restart maintenance --ignore-players --delay 5");
        CommandValidationResult result = console.ValidateCommand(parse.Command!);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Command, Is.SameAs(parse.Command));
        Assert.That(result.Failure!.Kind, Is.EqualTo(ConsoleFailureKind.CommandConstraint));
        Assert.That(result.Failure.Code, Is.EqualTo(ConsoleFailureCodes.CommandConstraintRejected));
        Assert.That(result.Failure.Message, Is.EqualTo("Delay cannot be combined with ignore players."));
        Assert.That(result.Failure.Source, Is.EqualTo("delay-ignore-players"));
    }

    [Test]
    public void ValidateCommand_TypedConstraintPass_Succeeds()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("server.restart")
            .Argument<RestartState>(x => x.Reason, "reason")
            .Flag<RestartState>(x => x.IgnorePlayers, "ignore-players")
            .Option<RestartState>(x => x.DelaySeconds, "delay")
            .Constraint<RestartState>(
                "delay-ignore-players",
                "Delay cannot be combined with ignore players.",
                state => !state.IgnorePlayers || state.DelaySeconds == 0)
            .Execute<RestartState>((ctx, state) => new CommandResult())
            .Build());

        CommandParseResult parse = console.ParseCommand("server.restart maintenance --ignore-players --delay 0");
        CommandValidationResult result = console.ValidateCommand(parse.Command!);

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void ValidateCommand_MultipleConstraints_ReturnsFirstFailure()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("server.restart")
            .Argument<RestartReasonState>(x => x.Reason, "reason")
            .Constraint<RestartReasonState>("first", "First failed.", state => false)
            .Constraint<RestartReasonState>("second", "Second failed.", state => false)
            .Execute<RestartReasonState>((ctx, state) => new CommandResult())
            .Build());

        CommandParseResult parse = console.ParseCommand("server.restart maintenance");
        CommandValidationResult result = console.ValidateCommand(parse.Command!);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Failure!.Message, Is.EqualTo("First failed."));
        Assert.That(result.Failure.Source, Is.EqualTo("first"));
    }

    [Test]
    public void ValidateCommand_WhenConstraintThrows_ReturnsFailureWithCause()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("server.restart")
            .Argument<RestartReasonState>(x => x.Reason, "reason")
            .Constraint<RestartReasonState>("throws", "Should not surface directly.", state => throw new InvalidOperationException("Broken predicate."))
            .Execute<RestartReasonState>((ctx, state) => new CommandResult())
            .Build());

        CommandParseResult parse = console.ParseCommand("server.restart maintenance");
        CommandValidationResult result = console.ValidateCommand(parse.Command!);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Failure!.Kind, Is.EqualTo(ConsoleFailureKind.CommandConstraint));
        Assert.That(result.Failure.Code, Is.EqualTo(ConsoleFailureCodes.CommandConstraintRejected));
        Assert.That(result.Failure.Source, Is.EqualTo("throws"));
        Assert.That(result.Failure.Cause, Is.Not.Null);
        Assert.That(result.Failure.Cause!.Message, Is.EqualTo("Broken predicate."));
    }

    [Test]
    public void ValidateCommand_Range_IsInclusive()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("server.restart")
            .Option<RangeState>(x => x.DelaySeconds, "delay")
            .Range(0, 10)
            .Execute<RangeState>((ctx, state) => new CommandResult())
            .Build());

        CommandValidationResult minimum = console.ValidateCommand(console.ParseCommand("server.restart --delay 0").Command!);
        CommandValidationResult maximum = console.ValidateCommand(console.ParseCommand("server.restart --delay 10").Command!);
        CommandValidationResult tooHigh = console.ValidateCommand(console.ParseCommand("server.restart --delay 11").Command!);

        Assert.That(minimum.Success, Is.True);
        Assert.That(maximum.Success, Is.True);
        Assert.That(tooHigh.Success, Is.False);
        Assert.That(tooHigh.Failure!.Source, Is.EqualTo("delay"));
    }

    [Test]
    public void ValidateCommand_AllowedValues_EnforcesMembership()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("mode.set")
            .Option<ModeState>(x => x.Mode, "mode")
            .AllowedValues(Mode.Fast, Mode.Slow)
            .Execute<ModeState>((ctx, state) => new CommandResult())
            .Build());

        CommandValidationResult allowed = console.ValidateCommand(console.ParseCommand("mode.set --mode Fast").Command!);
        CommandValidationResult rejected = console.ValidateCommand(console.ParseCommand("mode.set --mode Hidden").Command!);

        Assert.That(allowed.Success, Is.True);
        Assert.That(rejected.Success, Is.False);
        Assert.That(rejected.Failure!.Source, Is.EqualTo("mode"));
    }

    [Test]
    public void ValidateCommand_RangeAndAllowedValues_BothMustPass()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("server.restart")
            .Option<RangeState>(x => x.DelaySeconds, "delay")
            .Range(0, 10)
            .AllowedValues(0, 5, 10)
            .Execute<RangeState>((ctx, state) => new CommandResult())
            .Build());

        CommandValidationResult allowed = console.ValidateCommand(console.ParseCommand("server.restart --delay 5").Command!);
        CommandValidationResult outsideAllowedValues = console.ValidateCommand(console.ParseCommand("server.restart --delay 6").Command!);
        CommandValidationResult outsideRange = console.ValidateCommand(console.ParseCommand("server.restart --delay 11").Command!);

        Assert.That(allowed.Success, Is.True);
        Assert.That(outsideAllowedValues.Success, Is.False);
        Assert.That(outsideRange.Success, Is.False);
    }

    [Test]
    public void Build_InvalidRangeMetadataThrows()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new CommandBuilder("server.restart")
                .Option<RangeState>(x => x.DelaySeconds, "delay")
                .Range(10, 0)
                .Execute<RangeState>((ctx, state) => new CommandResult())
                .Build());
    }

    [Test]
    public void Build_InvalidAllowedValueMetadataThrows()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new CommandBuilder("server.restart")
                .Option<RangeState>(x => x.DelaySeconds, "delay")
                .AllowedValues("not-a-number")
                .Execute<RangeState>((ctx, state) => new CommandResult())
                .Build());
    }

    [Test]
    public void Constraint_WhenPredicateIsNullThrows()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CommandBuilder("server.restart")
                .Argument<RestartState>(x => x.Reason, "reason")
                .Constraint<RestartState>("constraint", "message", null!));
    }

    [Test]
    public void ValidateCommand_NullCommandThrows()
    {
        var console = new ConsoleManager();

        Assert.Throws<ArgumentNullException>(() => console.ValidateCommand(null!));
    }

    [Test]
    public void ValidateCommand_HasNoHistorySideEffects()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(SimpleCommand("noclip"));

        console.ValidateCommand(console.ParseCommand("noclip").Command!);

        Assert.That(console.History.Entries, Is.Empty);
        Assert.That(console.CommandHistory.Entries, Is.Empty);
    }

    private static CommandDefinition SimpleCommand(string path)
    {
        return new CommandBuilder(path)
            .Execute(ctx => new CommandResult())
            .Build();
    }

    private sealed record RestartState(string Reason, bool IgnorePlayers, int DelaySeconds);

    private sealed record RestartReasonState(string Reason);

    private sealed record RangeState(int DelaySeconds);

    private sealed record ModeState(Mode Mode);

    private enum Mode
    {
        Slow,
        Fast,
        Hidden
    }
}
