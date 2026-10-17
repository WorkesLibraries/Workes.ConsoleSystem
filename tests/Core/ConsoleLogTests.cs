using System;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Entries;
using Workes.ConsoleSystem.Logging;
using Workes.ConsoleSystem.Presentation;

namespace Workes.ConsoleSystem.Tests.Core;

public sealed class ConsoleLogTests
{
    [Test]
    public void ManagerLoggingMethods_RecordAllSixLevelsInCallOrder()
    {
        var console = new ConsoleManager();

        console.LogTrace("Trace");
        console.LogDebug("Debug");
        console.LogInformation("Information");
        console.LogWarning("Warning");
        console.LogError("Error");
        console.LogCritical("Critical");

        Assert.That(console.History.Entries, Has.Count.EqualTo(6));
        Assert.That(((LogEntry)console.History.Entries[0]).Level, Is.EqualTo(LogLevel.Trace));
        Assert.That(((LogEntry)console.History.Entries[1]).Level, Is.EqualTo(LogLevel.Debug));
        Assert.That(((LogEntry)console.History.Entries[2]).Level, Is.EqualTo(LogLevel.Information));
        Assert.That(((LogEntry)console.History.Entries[3]).Level, Is.EqualTo(LogLevel.Warning));
        Assert.That(((LogEntry)console.History.Entries[4]).Level, Is.EqualTo(LogLevel.Error));
        Assert.That(((LogEntry)console.History.Entries[5]).Level, Is.EqualTo(LogLevel.Critical));
    }

    [Test]
    public void SemanticTextOverload_PreservesContentAndRecordsUtcTimestamp()
    {
        var console = new ConsoleManager();
        ConsoleText content = ConsoleText.Plain("Health restored", "success");

        console.LogInformation(content);

        var entry = (LogEntry)console.History.Entries[0];
        Assert.That(entry.Content, Is.SameAs(content));
        Assert.That(entry.Message, Is.EqualTo("Health restored"));
        Assert.That(entry.Timestamp.Offset, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void LoggingMethods_RejectNullMessageOrContent()
    {
        var console = new ConsoleManager();

        Assert.Throws<ArgumentNullException>(() => console.LogInformation((string)null!));
        Assert.Throws<ArgumentNullException>(() => console.LogInformation((ConsoleText)null!));
        Assert.That(console.History.Entries, Is.Empty);
    }
}
