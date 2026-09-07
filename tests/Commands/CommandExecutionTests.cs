using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Entries;

namespace Workes.ConsoleSystem.Tests.Commands;

public sealed class CommandExecutionTests
{
    [Test]
    public void ExecuteCommand_String_ParsesValidatesInvokesSimpleHandlerAndReturnsResult()
    {
        bool invoked = false;
        var console = new ConsoleManager();
        var expected = CommandResult.Success(CommandOutput.Inline("Noclip enabled.", "Success"));
        console.RegisterCommand(new CommandBuilder("noclip")
            .Execute(ctx =>
            {
                invoked = true;
                return expected;
            })
            .Build());

        CommandResult result = console.ExecuteCommand("noclip");

        Assert.That(invoked, Is.True);
        Assert.That(result, Is.SameAs(expected));
        Assert.That(console.CommandHistory.Entries, Is.EqualTo(new[] { "noclip" }));
        Assert.That(console.History.Entries[0], Is.TypeOf<CommandInputEntry>());
        Assert.That(console.History.Entries[1], Is.TypeOf<CommandOutputEntry>());
        Assert.That(((CommandOutputEntry)console.History.Entries[1]).PlainText, Is.EqualTo("Noclip enabled."));
    }

    [Test]
    public void ExecuteCommand_String_InvokesTypedHandlerWithBoundState()
    {
        RestartState? captured = null;
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("server.restart")
            .Argument<RestartState>(x => x.Reason, "reason")
            .Flag<RestartState>(x => x.IgnorePlayers, "ignore-players")
            .Option<RestartState>(x => x.DelaySeconds, "delay")
            .Execute<RestartState>((ctx, state) =>
            {
                captured = state;
                return new CommandResult();
            })
            .Build());

        CommandResult result = console.ExecuteCommand("server.restart maintenance --ignore-players --delay 5");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(captured, Is.EqualTo(new RestartState("maintenance", true, 5)));
    }

    [Test]
    public void ExecuteCommand_BoundCommand_ValidatesAndInvokesWithoutReparsing()
    {
        int invocations = 0;
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("counter.add")
            .Argument<CountState>(x => x.Amount, "amount")
            .Constraint<CountState>("positive", "Amount must be positive.", state => state.Amount > 0)
            .Execute<CountState>((ctx, state) =>
            {
                invocations++;
                return CommandResult.Success(CommandOutput.Inline(state.Amount.ToString()));
            })
            .Build());
        BoundCommand bound = console.ParseCommand("counter.add 3").Command!;

        CommandResult result = console.ExecuteCommand(bound);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(invocations, Is.EqualTo(1));
        Assert.That(console.CommandHistory.Entries, Is.EqualTo(new[] { "counter.add 3" }));
    }

    [Test]
    public void ExecuteCommand_ParseFailure_ReturnsFailureAndAppendsFailureEntry()
    {
        var console = new ConsoleManager();

        ConsoleOperationException ex = Assert.Throws<ConsoleOperationException>(() => console.ExecuteCommand("missing"))!;

        Assert.That(ex.Failure.Code, Is.EqualTo(ConsoleFailureCodes.CommandUnknown));
        Assert.That(console.History.Entries[0], Is.TypeOf<CommandInputEntry>());
        Assert.That(console.History.Entries[1], Is.TypeOf<CommandFailureEntry>());
        Assert.That(((CommandFailureEntry)console.History.Entries[1]).Failure, Is.SameAs(ex.Failure));
    }

    [Test]
    public void ExecuteCommand_ValidationFailure_ReturnsFailureAndDoesNotInvokeHandler()
    {
        bool invoked = false;
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("counter.add")
            .Argument<CountState>(x => x.Amount, "amount")
            .Constraint<CountState>("positive", "Amount must be positive.", state => state.Amount > 0)
            .Execute<CountState>((ctx, state) =>
            {
                invoked = true;
                return new CommandResult();
            })
            .Build());

        ConsoleOperationException ex = Assert.Throws<ConsoleOperationException>(() => console.ExecuteCommand("counter.add 0"))!;

        Assert.That(invoked, Is.False);
        Assert.That(ex.Failure.Code, Is.EqualTo(ConsoleFailureCodes.CommandConstraintRejected));
        Assert.That(console.History.Entries.Last(), Is.TypeOf<CommandFailureEntry>());
    }

    [Test]
    public void ExecuteCommand_HandlerReturnedFailure_ReturnsFailureAndAppendsFailureEntry()
    {
        var failure = ConsoleFailure.Create(
            ConsoleFailureKind.CommandExecution,
            ConsoleFailureCodes.CommandExecutionRejected,
            "Player not found.");
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("player.heal")
            .Execute(ctx => CommandResult.Failed(failure))
            .Build());

        ConsoleOperationException ex = Assert.Throws<ConsoleOperationException>(() => console.ExecuteCommand("player.heal"))!;

        Assert.That(ex.Failure, Is.SameAs(failure));
        Assert.That(console.History.Entries.Last(), Is.TypeOf<CommandFailureEntry>());
    }

    [Test]
    public void ExecuteCommand_HandlerException_ReturnsExecutionFailureWithCause()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("explode")
            .Execute(ctx => throw new InvalidOperationException("Boom."))
            .Build());

        ConsoleOperationException ex = Assert.Throws<ConsoleOperationException>(() => console.ExecuteCommand("explode"))!;

        Assert.That(ex.Failure.Kind, Is.EqualTo(ConsoleFailureKind.CommandExecution));
        Assert.That(ex.Failure.Code, Is.EqualTo(ConsoleFailureCodes.CommandExecutionRejected));
        Assert.That(ex.Failure.Cause, Is.Not.Null);
        Assert.That(ex.Failure.Cause!.Message, Is.EqualTo("Boom."));
        Assert.That(console.History.Entries.Last(), Is.TypeOf<CommandFailureEntry>());
    }

    [Test]
    public void ExecuteCommand_NullHandlerResult_ReturnsExecutionFailure()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("null.result")
            .Execute(ctx => null!)
            .Build());

        ConsoleOperationException ex = Assert.Throws<ConsoleOperationException>(() => console.ExecuteCommand("null.result"))!;

        Assert.That(ex.Failure.Kind, Is.EqualTo(ConsoleFailureKind.CommandExecution));
        Assert.That(console.History.Entries.Last(), Is.TypeOf<CommandFailureEntry>());
    }

    [Test]
    public void TryExecuteCommand_ReturnsTrueForSuccessAndFalseForFailure()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("noclip")
            .Execute(ctx => new CommandResult())
            .Build());

        bool success = console.TryExecuteCommand("noclip", out CommandResult successResult);
        bool failure = console.TryExecuteCommand("missing", out CommandResult failureResult);

        Assert.That(success, Is.True);
        Assert.That(successResult.IsSuccess, Is.True);
        Assert.That(failure, Is.False);
        Assert.That(failureResult.IsSuccess, Is.False);
    }

    [Test]
    public void TryExecuteCommand_BoundCommand_ReturnsTrueForSuccessAndFalseForFailure()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("counter.add")
            .Argument<CountState>(x => x.Amount, "amount")
            .Constraint<CountState>("positive", "Amount must be positive.", state => state.Amount > 0)
            .Execute<CountState>((ctx, state) => new CommandResult())
            .Build());
        BoundCommand valid = console.ParseCommand("counter.add 1").Command!;
        BoundCommand invalid = console.ParseCommand("counter.add 0").Command!;

        bool success = console.TryExecuteCommand(valid, out CommandResult successResult);
        bool failure = console.TryExecuteCommand(invalid, out CommandResult failureResult);

        Assert.That(success, Is.True);
        Assert.That(successResult.IsSuccess, Is.True);
        Assert.That(failure, Is.False);
        Assert.That(failureResult.IsSuccess, Is.False);
    }

    [Test]
    public void ExecuteCommand_ReturnsSuccessAndThrowsOperationExceptionOnFailure()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("noclip")
            .Execute(ctx => new CommandResult())
            .Build());

        CommandResult success = console.ExecuteCommand("noclip");
        ConsoleOperationException ex = Assert.Throws<ConsoleOperationException>(() => console.ExecuteCommand("missing"))!;

        Assert.That(success.IsSuccess, Is.True);
        Assert.That(ex.Failure.Code, Is.EqualTo(ConsoleFailureCodes.CommandUnknown));
    }

    [Test]
    public void Echo_DefaultsAndCommandOverrides_AreApplied()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            Execution = new CommandExecutionOptions
            {
                EchoInputDefaultStyle = "CommandInput"
            }
        });
        console.RegisterCommand(new CommandBuilder("default.echo")
            .Execute(ctx => new CommandResult())
            .Build());
        console.RegisterCommand(new CommandBuilder("styled.echo")
            .EchoInput("SpecialInput")
            .Execute(ctx => new CommandResult())
            .Build());
        console.RegisterCommand(new CommandBuilder("quiet")
            .DoNotEchoInput()
            .Execute(ctx => new CommandResult())
            .Build());

        console.ExecuteCommand("default.echo");
        console.ExecuteCommand("styled.echo");
        console.ExecuteCommand("quiet");

        var inputEntries = console.History.Entries.OfType<CommandInputEntry>().ToArray();
        Assert.That(inputEntries, Has.Length.EqualTo(2));
        Assert.That(inputEntries[0].Content.DefaultStyleId, Is.EqualTo("CommandInput"));
        Assert.That(inputEntries[1].Content.DefaultStyleId, Is.EqualTo("SpecialInput"));
    }

    [Test]
    public void ExecuteCommand_WhenManagerEchoDisabled_DoesNotEchoInputUnlessCommandOverrides()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            Execution = new CommandExecutionOptions
            {
                EchoInput = false
            }
        });
        console.RegisterCommand(new CommandBuilder("quiet.default")
            .Execute(ctx => new CommandResult())
            .Build());
        console.RegisterCommand(new CommandBuilder("loud.override")
            .EchoInput()
            .Execute(ctx => new CommandResult())
            .Build());

        console.ExecuteCommand("quiet.default");
        console.ExecuteCommand("loud.override");

        Assert.That(console.History.Entries.OfType<CommandInputEntry>().Select(x => x.Input), Is.EqualTo(new[] { "loud.override" }));
    }

    [Test]
    public void ExecuteCommand_SuccessOutputs_AreDeclaredThenDynamic()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("save")
            .SuccessOutputInline("Declared")
            .Execute(ctx => CommandResult.Success(CommandOutput.Inline("Dynamic")))
            .Build());

        console.ExecuteCommand("save");

        Assert.That(
            console.History.Entries.OfType<CommandOutputEntry>().Select(x => x.PlainText),
            Is.EqualTo(new[] { "Declared", "Dynamic" }));
    }

    [Test]
    public void ExecuteCommand_RespectsBoundedHistoryCapacity()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            History = new HistoryOptions
            {
                ConsoleHistoryCapacity = 2
            }
        });
        console.RegisterCommand(new CommandBuilder("one")
            .Execute(ctx => CommandResult.Success(CommandOutput.Inline("One")))
            .Build());
        console.RegisterCommand(new CommandBuilder("two")
            .Execute(ctx => CommandResult.Success(CommandOutput.Inline("Two")))
            .Build());

        console.ExecuteCommand("one");
        console.ExecuteCommand("two");

        Assert.That(console.History.Entries, Has.Count.EqualTo(2));
        Assert.That(console.History.Entries[0], Is.TypeOf<CommandInputEntry>());
        Assert.That(((CommandInputEntry)console.History.Entries[0]).Input, Is.EqualTo("two"));
        Assert.That(((CommandOutputEntry)console.History.Entries[1]).PlainText, Is.EqualTo("Two"));
    }

    [Test]
    public void CommandContext_ExposesExecutionMetadata()
    {
        CommandContext? captured = null;
        var console = new ConsoleManager();
        CommandDefinition command = new CommandBuilder("noclip")
            .Execute(ctx =>
            {
                captured = ctx;
                return new CommandResult();
            })
            .Build();
        console.RegisterCommand(command);

        console.ExecuteCommand("noclip");

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Input, Is.EqualTo("noclip"));
        Assert.That(captured.Definition, Is.SameAs(command));
        Assert.That(captured.Command.Definition, Is.SameAs(command));
        Assert.That(captured.Command.Input, Is.EqualTo("noclip"));
        Assert.That(captured.Timestamp, Is.Not.EqualTo(default(DateTimeOffset)));
    }

    [Test]
    public void ExecuteCommand_NullArgumentsThrow()
    {
        var console = new ConsoleManager();

        Assert.Throws<ArgumentNullException>(() => console.ExecuteCommand((string)null!));
        Assert.Throws<ArgumentNullException>(() => console.ExecuteCommand((BoundCommand)null!));
        Assert.Throws<ArgumentNullException>(() => console.TryExecuteCommand((string)null!, out _));
        Assert.Throws<ArgumentNullException>(() => console.TryExecuteCommand((BoundCommand)null!, out _));
    }

    private sealed record RestartState(string Reason, bool IgnorePlayers, int DelaySeconds);

    private sealed record CountState(int Amount);
}
