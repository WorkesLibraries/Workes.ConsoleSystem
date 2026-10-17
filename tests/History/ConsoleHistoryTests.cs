using System;
using System.Collections.Generic;
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Entries;
using Workes.ConsoleSystem.Logging;

namespace Workes.ConsoleSystem.Tests.History;

public sealed class ConsoleHistoryTests
{
    [Test]
    public void Manager_HistoryExposesConfiguredCapacity()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            History = new HistoryOptions { ConsoleHistoryCapacity = 2 }
        });

        Assert.That(console.History.Capacity, Is.EqualTo(2));
        Assert.That(console.History.Entries, Is.Empty);
    }

    [Test]
    public void Entries_ReturnsOldestFirstSnapshotAndRetentionDropsOldestEntry()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            History = new HistoryOptions { ConsoleHistoryCapacity = 2 }
        });
        var first = new LogEntry(DateTimeOffset.UnixEpoch, LogLevel.Information, "First");
        var second = new LogEntry(DateTimeOffset.UnixEpoch, LogLevel.Warning, "Second");
        var third = new LogEntry(DateTimeOffset.UnixEpoch, LogLevel.Error, "Third");

        console.RecordEntry(first);
        console.RecordEntry(second);
        IReadOnlyList<IConsoleEntry> earlierSnapshot = console.History.Entries;
        console.RecordEntry(third);

        Assert.That(earlierSnapshot, Has.Count.EqualTo(2));
        Assert.That(earlierSnapshot[0], Is.SameAs(first));
        Assert.That(earlierSnapshot[1], Is.SameAs(second));

        IReadOnlyList<IConsoleEntry> currentEntries = console.History.Entries;
        Assert.That(currentEntries, Has.Count.EqualTo(2));
        Assert.That(currentEntries[0], Is.SameAs(second));
        Assert.That(currentEntries[1], Is.SameAs(third));
    }

    [Test]
    public void Entries_ReturnsCustomConsoleEntries()
    {
        var console = new ConsoleManager();
        var customEntry = new CustomConsoleEntry(DateTimeOffset.UnixEpoch);

        console.RecordEntry(customEntry);

        Assert.That(console.History.Entries, Has.Count.EqualTo(1));
        Assert.That(console.History.Entries[0], Is.SameAs(customEntry));
    }

    [Test]
    public void Clear_RemovesAllRetainedEntriesAndCanBeCalledWhenAlreadyEmpty()
    {
        var console = new ConsoleManager();
        console.RecordEntry(new LogEntry(DateTimeOffset.UnixEpoch, LogLevel.Information, "First"));
        console.RecordEntry(new LogEntry(DateTimeOffset.UnixEpoch, LogLevel.Warning, "Second"));

        console.History.Clear();

        Assert.That(console.History.Entries, Is.Empty);
        Assert.DoesNotThrow(() => console.History.Clear());
    }

    [Test]
    public void EntriesSnapshot_IsReadOnlyAndDetachedFromHistory()
    {
        var console = new ConsoleManager();
        var first = new LogEntry(DateTimeOffset.UnixEpoch, LogLevel.Information, "First");
        console.RecordEntry(first);

        IReadOnlyList<IConsoleEntry> snapshot = console.History.Entries;
        console.History.Clear();

        Assert.That(snapshot, Has.Count.EqualTo(1));
        Assert.That(snapshot[0], Is.SameAs(first));
        Assert.That(console.History.Entries, Is.Empty);
        Assert.Throws<NotSupportedException>(() => ((IList<IConsoleEntry>)snapshot).Clear());
    }

    [Test]
    public void RecordEntry_RejectsNullEntry()
    {
        var console = new ConsoleManager();

        Assert.Throws<ArgumentNullException>(() => console.RecordEntry(null!));
    }

    private sealed class CustomConsoleEntry : IConsoleEntry
    {
        internal CustomConsoleEntry(DateTimeOffset timestamp)
        {
            Timestamp = timestamp;
        }

        public DateTimeOffset Timestamp { get; }
    }
}
