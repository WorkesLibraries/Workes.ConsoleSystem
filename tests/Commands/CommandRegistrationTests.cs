using System.Collections;
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;

namespace Workes.ConsoleSystem.Tests.Commands;

public sealed class CommandRegistrationTests
{
    [Test]
    public void Build_SimpleCommand_Succeeds()
    {
        CommandDefinition command = new CommandBuilder("noclip")
            .Description("Toggle noclip.")
            .Execute(ctx => new CommandResult())
            .Build();

        Assert.That(command.Path, Is.EqualTo("noclip"));
        Assert.That(command.Description, Is.EqualTo("Toggle noclip."));
        Assert.That(command.StateType, Is.Null);
        Assert.That(command.HasHandler, Is.True);
        Assert.That(command.EchoInput.EchoInput, Is.Null);
        Assert.That(command.SuccessOutputs, Is.Empty);
    }

    [Test]
    public void Build_TypedCommandWithSchemaMetadata_Succeeds()
    {
        CommandDefinition command = new CommandBuilder("server.restart")
            .Description("Restart the server.")
            .Argument<RestartCommandState>(x => x.Reason, "reason")
            .Flag<RestartCommandState>(x => x.IgnorePlayers, "ignore-players", "i")
            .Option<RestartCommandState>(x => x.DelaySeconds, "delay", "d")
            .Default(10)
            .Range(0, 60)
            .AllowedValues(0, 10, 30, 60)
            .Constraint("delay-ignore-players", "Delay cannot be combined with ignore players.")
            .Execute<RestartCommandState>((ctx, state) => new CommandResult())
            .Build();

        Assert.That(command.StateType, Is.EqualTo(typeof(RestartCommandState)));
        Assert.That(command.Arguments, Has.Count.EqualTo(1));
        Assert.That(command.Arguments[0].Name, Is.EqualTo("reason"));
        Assert.That(command.Flags, Has.Count.EqualTo(1));
        Assert.That(command.Flags[0].Name, Is.EqualTo("ignore-players"));
        Assert.That(command.Flags[0].Aliases, Is.EqualTo(new[] { "i" }));
        Assert.That(command.Options, Has.Count.EqualTo(1));
        Assert.That(command.Options[0].Name, Is.EqualTo("delay"));
        Assert.That(command.Options[0].DefaultValue, Is.EqualTo(10));
        Assert.That(command.Options[0].RangeMinimum, Is.EqualTo(0));
        Assert.That(command.Options[0].RangeMaximum, Is.EqualTo(60));
        Assert.That(command.Options[0].AllowedValues, Is.EqualTo(new object[] { 0, 10, 30, 60 }));
        Assert.That(command.Constraints, Has.Count.EqualTo(1));
        Assert.That(command.Constraints[0].Message, Is.EqualTo("Delay cannot be combined with ignore players."));
    }

    [Test]
    public void Build_CommandEchoInputAndSuccessOutputMetadata_Succeeds()
    {
        CommandDefinition command = new CommandBuilder("noclip")
            .EchoInput("CommandInput")
            .SuccessOutputInline("Noclip enabled.", "Success")
            .SuccessOutputBlock("Line 1\nLine 2", "Success")
            .SuccessOutputInlineMarkup("<style=Success>Noclip enabled.</style>")
            .Execute(ctx => new CommandResult())
            .Build();

        Assert.That(command.EchoInput.EchoInput, Is.True);
        Assert.That(command.EchoInput.DefaultStyleId, Is.EqualTo("CommandInput"));
        Assert.That(command.SuccessOutputs, Has.Count.EqualTo(3));
        Assert.That(command.SuccessOutputs[0].Output!.Kind, Is.EqualTo(CommandOutputKind.Inline));
        Assert.That(command.SuccessOutputs[0].Output!.PlainText, Is.EqualTo("Noclip enabled."));
        Assert.That(command.SuccessOutputs[1].Output!.Kind, Is.EqualTo(CommandOutputKind.Block));
        Assert.That(command.SuccessOutputs[1].Output!.PlainText, Is.EqualTo("Line 1\nLine 2"));
        Assert.That(command.SuccessOutputs[2].Output.IsMarkup, Is.True);
        Assert.That(command.SuccessOutputs[2].Output.Markup, Is.EqualTo("<style=Success>Noclip enabled.</style>"));
    }

    [Test]
    public void Build_DoNotEchoInputStoresOverride()
    {
        CommandDefinition command = new CommandBuilder("quiet")
            .DoNotEchoInput()
            .Execute(ctx => new CommandResult())
            .Build();

        Assert.That(command.EchoInput.EchoInput, Is.False);
        Assert.That(command.EchoInput.DefaultStyleId, Is.Null);
    }

    [Test]
    public void Build_WithoutHandlerThrows()
    {
        var builder = new CommandBuilder("noclip");

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("server restart")]
    [TestCase(".restart")]
    [TestCase("server.")]
    [TestCase("server..restart")]
    public void Build_InvalidPathThrows(string? path)
    {
        var builder = new CommandBuilder(path!)
            .Execute(ctx => new CommandResult());

        Assert.That(() => builder.Build(), Throws.InstanceOf<Exception>());
    }

    [Test]
    public void Build_DuplicateMemberNameOrAliasThrows()
    {
        var builder = new CommandBuilder("server.restart")
            .Flag<RestartCommandState>(x => x.IgnorePlayers, "ignore-players", "i")
            .Option<RestartCommandState>(x => x.DelaySeconds, "delay", "i")
            .Execute<RestartCommandState>((ctx, state) => new CommandResult());

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Test]
    public void Flag_WhenBoundToNonBoolPropertyThrows()
    {
        Assert.Throws<ArgumentException>(() =>
            new CommandBuilder("server.restart")
                .Flag<RestartCommandState>(x => x.DelaySeconds, "delay"));
    }

    [Test]
    public void TypedCommand_WhenStateTypeIsNotReferenceTypeThrows()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new CommandBuilder("value.command")
                .Execute<int>((ctx, state) => new CommandResult()));
    }

    [Test]
    public void RegisterCommand_StoresDefinitionsInRegistrationOrder()
    {
        var console = new ConsoleManager();
        CommandDefinition first = SimpleCommand("first");
        CommandDefinition second = SimpleCommand("second");

        CommandDefinition registered = console.RegisterCommand(first);
        console.RegisterCommand(second);

        Assert.That(registered, Is.SameAs(first));
        Assert.That(console.Commands.Definitions, Is.EqualTo(new[] { first, second }));
    }

    [Test]
    public void RegisterCommand_DuplicatePathUsesConfiguredCaseSensitivity()
    {
        var console = new ConsoleManager();

        console.RegisterCommand(SimpleCommand("noclip"));

        Assert.Throws<InvalidOperationException>(() => console.RegisterCommand(SimpleCommand("NOCLIP")));
    }

    [Test]
    public void RegisterCommand_WhenCaseSensitive_AllowsPathWithDifferentCase()
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

        Assert.That(console.Commands.Definitions, Has.Count.EqualTo(2));
    }

    [Test]
    public void RegisterCommands_WhenBatchContainsDuplicatePath_RegistersNoCommands()
    {
        var console = new ConsoleManager();

        Assert.Throws<InvalidOperationException>(() => console.RegisterCommands(new[]
        {
            SimpleCommand("noclip"),
            SimpleCommand("NOCLIP")
        }));

        Assert.That(console.Commands.Definitions, Is.Empty);
    }

    [Test]
    public void RegisterCommand_WhenFlagOrOptionIncludesConfiguredPrefixThrows()
    {
        var console = new ConsoleManager();
        CommandDefinition command = new CommandBuilder("server.restart")
            .Flag<RestartCommandState>(x => x.IgnorePlayers, "--ignore-players")
            .Execute<RestartCommandState>((ctx, state) => new CommandResult())
            .Build();

        Assert.Throws<InvalidOperationException>(() => console.RegisterCommand(command));
    }

    [Test]
    public void RegisterCommand_UsesConfiguredFlagAndOptionPrefixValidation()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            CommandParsing = new CommandParsingOptions
            {
                FlagAndOptionPrefix = "-"
            }
        });

        CommandDefinition command = new CommandBuilder("server.restart")
            .Flag<RestartCommandState>(x => x.IgnorePlayers, "ignore-players", "i")
            .Option<RestartCommandState>(x => x.DelaySeconds, "delay", "d")
            .Execute<RestartCommandState>((ctx, state) => new CommandResult())
            .Build();

        Assert.DoesNotThrow(() => console.RegisterCommand(command));
    }

    [Test]
    public void Definitions_AreReadOnly()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(SimpleCommand("noclip"));

        var definitions = (IList)console.Commands.Definitions;

        Assert.Throws<NotSupportedException>(() => definitions.Add(SimpleCommand("help")));
    }

    [Test]
    public void ManagerLoggingMethods_WriteToConsoleHistory()
    {
        var console = new ConsoleManager();

        console.LogInformation("Ready.");

        Assert.That(console.History.Entries, Has.Count.EqualTo(1));
    }

    [Test]
    public void RecordCommandInput_WritesToCommandHistory()
    {
        var console = new ConsoleManager();

        bool added = console.RecordCommandInput("noclip");

        Assert.That(added, Is.True);
        Assert.That(console.CommandHistory.Entries, Is.EqualTo(new[] { "noclip" }));
    }

    private static CommandDefinition SimpleCommand(string path)
    {
        return new CommandBuilder(path)
            .Execute(ctx => new CommandResult())
            .Build();
    }

    private sealed record RestartCommandState(string Reason, bool IgnorePlayers, int DelaySeconds);
}
