using System;
using System.Collections.Generic;
using System.Reflection;
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;

namespace Workes.ConsoleSystem.Tests.Core;

public sealed class CommandInputHistoryTests
{
    [Test]
    public void Manager_ConstructsBoundedCommandHistoryWithOldDefaults()
    {
        var console = new ConsoleManager();

        Assert.That(console.CommandHistory.Capacity, Is.EqualTo(100));
        Assert.That(console.CommandHistory.Entries, Is.Empty);
        Assert.That(console.Options.Execution.EchoInput, Is.True);
        Assert.That(console.Options.History.CommandHistoryDuplicatePolicy,
            Is.EqualTo(CommandHistoryDuplicatePolicy.RejectConsecutive));
    }

    [Test]
    public void RecordCommandInput_PreservesOriginalNonblankInput()
    {
        var console = new ConsoleManager();
        const string input = "  Player.Heal  ";

        bool retained = console.RecordCommandInput(input);

        Assert.That(retained, Is.True);
        Assert.That(console.CommandHistory.Entries, Is.EqualTo(new[] { input }));
        Assert.That(console.History.Entries, Is.Empty);
    }

    [Test]
    public void RecordCommandInput_RejectsBlankAndNullInput()
    {
        var console = new ConsoleManager();

        Assert.That(console.RecordCommandInput(""), Is.False);
        Assert.That(console.RecordCommandInput("  \t"), Is.False);
        Assert.Throws<ArgumentNullException>(() => console.RecordCommandInput(null!));
        Assert.That(console.CommandHistory.Entries, Is.Empty);
    }

    [Test]
    public void RecordCommandInput_RejectsOnlyConsecutiveTrimmedCaseInsensitiveDuplicatesByDefault()
    {
        var console = new ConsoleManager();

        Assert.That(console.RecordCommandInput("  status "), Is.True);
        Assert.That(console.RecordCommandInput("STATUS"), Is.False);
        Assert.That(console.RecordCommandInput("help"), Is.True);
        Assert.That(console.RecordCommandInput("Status"), Is.True);
        Assert.That(console.CommandHistory.Entries, Is.EqualTo(new[] { "  status ", "help", "Status" }));
    }

    [Test]
    public void RecordCommandInput_AllowsConsecutiveDuplicatesWhenConfigured()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            History = new HistoryOptions
            {
                CommandHistoryDuplicatePolicy = CommandHistoryDuplicatePolicy.Allow
            }
        });

        Assert.That(console.RecordCommandInput("status"), Is.True);
        Assert.That(console.RecordCommandInput(" STATUS "), Is.True);
        Assert.That(console.CommandHistory.Entries, Is.EqualTo(new[] { "status", " STATUS " }));
    }

    [Test]
    public void RecordCommandInput_WhenAtCapacityDropsOldestInput()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            History = new HistoryOptions { CommandHistoryCapacity = 2 }
        });

        console.RecordCommandInput("first");
        console.RecordCommandInput("second");
        console.RecordCommandInput("third");

        Assert.That(console.CommandHistory.Capacity, Is.EqualTo(2));
        Assert.That(console.CommandHistory.Entries, Is.EqualTo(new[] { "second", "third" }));
    }

    [Test]
    public void Entries_ReturnsDetachedReadOnlySnapshot_AndClearRemovesInputs()
    {
        var console = new ConsoleManager();
        console.RecordCommandInput("first");
        IReadOnlyList<string> snapshot = console.CommandHistory.Entries;

        console.CommandHistory.Clear();

        Assert.That(snapshot, Is.EqualTo(new[] { "first" }));
        Assert.That(console.CommandHistory.Entries, Is.Empty);
        Assert.Throws<NotSupportedException>(() => ((IList<string>)snapshot).Clear());
        Assert.DoesNotThrow(() => console.CommandHistory.Clear());
    }

    [Test]
    public void ExecutionOptions_EchoInputIsSnapshotted()
    {
        var options = new ConsoleManagerOptions
        {
            Execution = new CommandExecutionOptions { EchoInput = false }
        };
        var console = new ConsoleManager(options);
        options.Execution.EchoInput = true;

        Assert.That(console.Options.Execution.EchoInput, Is.False);
    }

    [TestCase(true)]
    [TestCase(false)]
    [TestCase(null)]
    public void CommandEchoInputDefinition_StoresNullableOverride(bool? echoInput)
    {
        ConstructorInfo constructor = typeof(CommandEchoInputDefinition).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(bool?) },
            modifiers: null)!;
        var definition = (CommandEchoInputDefinition)constructor.Invoke(new object?[] { echoInput });

        Assert.That(definition.EchoInput, Is.EqualTo(echoInput));
        Assert.That(typeof(CommandEchoInputDefinition).GetConstructors(), Is.Empty);
    }
}
