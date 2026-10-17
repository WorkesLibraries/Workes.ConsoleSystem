using System;
using System.Collections.Generic;
using NUnit.Framework;
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Entries;
using Workes.ConsoleSystem.History;

namespace Workes.ConsoleSystem.Tests.History;

[TestFixture]
public sealed class HistoryChangeHookTests
{
    [Test]
    public void ConsoleHistory_AddAtCapacity_ReportsAddedAndDroppedEntriesTogether()
    {
        var console = CreateManager(consoleCapacity: 1);
        ConsoleHistoryChangedEventArgs? change = null;
        object? sender = null;
        console.History.Changed += (eventSender, args) =>
        {
            sender = eventSender;
            change = args;
        };

        console.LogInformation("first");
        IConsoleEntry first = console.History.Entries[0];
        console.LogInformation("second");

        Assert.That(sender, Is.SameAs(console.History));
        Assert.That(change, Is.Not.Null);
        Assert.That(change!.ChangeKind, Is.EqualTo(HistoryChangeKind.Added));
        Assert.That(change.AddedEntries, Has.Count.EqualTo(1));
        Assert.That(change.DroppedEntries, Has.Count.EqualTo(1));
        Assert.That(change.DroppedEntries[0], Is.SameAs(first));
        Assert.That(change.ClearedEntries, Is.Empty);
        Assert.That(change.RetainedCount, Is.EqualTo(1));
        Assert.That(console.History.Entries[0], Is.SameAs(change.AddedEntries[0]));
        Assert.That(change.AddedEntries, Is.InstanceOf<System.Collections.ObjectModel.ReadOnlyCollection<IConsoleEntry>>());
    }

    [Test]
    public void ConsoleHistory_Clear_ReportsTheRemovedSnapshotOnce()
    {
        var console = CreateManager();
        console.LogInformation("first");
        console.LogInformation("second");
        IReadOnlyList<IConsoleEntry> beforeClear = console.History.Entries;
        ConsoleHistoryChangedEventArgs? change = null;
        int eventCount = 0;
        console.History.Changed += (_, args) =>
        {
            eventCount++;
            change = args;
        };

        console.History.Clear();
        console.History.Clear();

        Assert.That(eventCount, Is.EqualTo(1));
        Assert.That(change!.ChangeKind, Is.EqualTo(HistoryChangeKind.Cleared));
        Assert.That(change.AddedEntries, Is.Empty);
        Assert.That(change.DroppedEntries, Is.Empty);
        Assert.That(change.ClearedEntries, Is.EqualTo(beforeClear));
        Assert.That(change.RetainedCount, Is.Zero);
    }

    [Test]
    public void CommandHistory_AddAtCapacity_ReportsAddedAndDroppedInputsTogether()
    {
        var console = CreateManager(commandCapacity: 1);
        CommandHistoryChangedEventArgs? change = null;
        object? sender = null;
        console.CommandHistory.Changed += (eventSender, args) =>
        {
            sender = eventSender;
            change = args;
        };

        console.RecordCommandInput("first");
        console.RecordCommandInput("second");

        Assert.That(sender, Is.SameAs(console.CommandHistory));
        Assert.That(change!.ChangeKind, Is.EqualTo(HistoryChangeKind.Added));
        Assert.That(change.AddedInputs, Is.EqualTo(new[] { "second" }));
        Assert.That(change.DroppedInputs, Is.EqualTo(new[] { "first" }));
        Assert.That(change.ClearedInputs, Is.Empty);
        Assert.That(change.RetainedCount, Is.EqualTo(1));
    }

    [Test]
    public void CommandHistory_RejectedInputAndEmptyClear_DoNotRaiseEvents()
    {
        var console = CreateManager();
        int eventCount = 0;
        console.CommandHistory.Changed += (_, _) => eventCount++;

        Assert.That(console.RecordCommandInput("  "), Is.False);
        console.CommandHistory.Clear();

        Assert.That(console.RecordCommandInput(" status "), Is.True);
        Assert.That(console.RecordCommandInput("status"), Is.False);
        Assert.That(eventCount, Is.EqualTo(1));
    }

    [Test]
    public void CommandHistory_Clear_ReportsClearedInputsAndNoOpClearIsSilent()
    {
        var console = CreateManager();
        console.RecordCommandInput("first");
        console.RecordCommandInput("second");
        CommandHistoryChangedEventArgs? change = null;
        int eventCount = 0;
        console.CommandHistory.Changed += (_, args) =>
        {
            eventCount++;
            change = args;
        };

        console.CommandHistory.Clear();
        console.CommandHistory.Clear();

        Assert.That(eventCount, Is.EqualTo(1));
        Assert.That(change!.ChangeKind, Is.EqualTo(HistoryChangeKind.Cleared));
        Assert.That(change.AddedInputs, Is.Empty);
        Assert.That(change.DroppedInputs, Is.Empty);
        Assert.That(change.ClearedInputs, Is.EqualTo(new[] { "first", "second" }));
        Assert.That(change.RetainedCount, Is.Zero);
    }

    [Test]
    public void ManagerEvents_ForwardHistoryEventsAndCanBeUnsubscribed()
    {
        var console = CreateManager();
        int consoleChanges = 0;
        int commandChanges = 0;
        object? consoleSender = null;
        object? commandSender = null;
        EventHandler<ConsoleHistoryChangedEventArgs> onConsoleChanged = (sender, _) =>
        {
            consoleSender = sender;
            consoleChanges++;
        };
        EventHandler<CommandHistoryChangedEventArgs> onCommandChanged = (sender, _) =>
        {
            commandSender = sender;
            commandChanges++;
        };
        console.ConsoleHistoryChanged += onConsoleChanged;
        console.CommandHistoryChanged += onCommandChanged;

        console.LogInformation("visible");
        console.RecordCommandInput("command");

        console.ConsoleHistoryChanged -= onConsoleChanged;
        console.CommandHistoryChanged -= onCommandChanged;
        console.LogInformation("another visible entry");
        console.RecordCommandInput("another command");

        Assert.That(consoleChanges, Is.EqualTo(1));
        Assert.That(commandChanges, Is.EqualTo(1));
        Assert.That(consoleSender, Is.SameAs(console.History));
        Assert.That(commandSender, Is.SameAs(console.CommandHistory));
    }

    [Test]
    public void ReadOnlyOperations_DoNotRaiseHistoryEvents()
    {
        var console = CreateManager();
        console.RegisterCommand(new CommandBuilder("status")
            .Execute(context => CommandResult.Success())
            .Build());
        int consoleChanges = 0;
        int commandChanges = 0;
        console.History.Changed += (_, _) => consoleChanges++;
        console.CommandHistory.Changed += (_, _) => commandChanges++;

        console.ParseCommand("status");
        var parse = console.ParseCommand("status");
        console.ValidateCommand(parse.Command!);
        console.GetAutocomplete("sta", 3);

        Assert.That(consoleChanges, Is.Zero);
        Assert.That(commandChanges, Is.Zero);
    }

    [Test]
    public void CommandExecution_RaisesHistoryEventsInWriteOrder()
    {
        var console = CreateManager();
        console.RegisterCommand(new CommandBuilder("say")
            .SuccessOutputInline("declared")
            .Execute(context => CommandResult.Success(CommandOutput.Inline("handler")))
            .Build());
        var eventOrder = new List<string>();
        console.CommandHistory.Changed += (_, _) => eventOrder.Add("command-history");
        console.History.Changed += (_, change) =>
        {
            if (change.AddedEntries[0] is CommandInputEntry)
            {
                eventOrder.Add("echo");
            }
            else if (change.AddedEntries[0] is CommandOutputEntry output)
            {
                eventOrder.Add(output.PlainText);
            }
        };

        console.ExecuteCommand("say");

        Assert.That(eventOrder, Is.EqualTo(new[] { "command-history", "echo", "declared", "handler" }));
    }

    [Test]
    public void SubscriberExceptionPropagatesAfterMutationIsCommitted()
    {
        var console = CreateManager();
        console.History.Changed += (_, _) => throw new InvalidOperationException("subscriber failed");

        Assert.Throws<InvalidOperationException>(() => console.LogInformation("committed"));
        Assert.That(console.History.Entries, Has.Count.EqualTo(1));
    }

    private static ConsoleManager CreateManager(int consoleCapacity = 2, int commandCapacity = 2)
    {
        return new ConsoleManager(new ConsoleManagerOptions
        {
            History = new HistoryOptions
            {
                ConsoleHistoryCapacity = consoleCapacity,
                CommandHistoryCapacity = commandCapacity
            }
        });
    }
}
