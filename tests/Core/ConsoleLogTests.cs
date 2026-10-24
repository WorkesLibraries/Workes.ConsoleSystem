using System;
using System.Reflection;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Entries;
using Workes.ConsoleSystem.Logging;
using Workes.ConsoleSystem.Presentation;

namespace Workes.ConsoleSystem.Tests.Core;

public sealed class ConsoleLogTests
{
    [Test]
    public void PublicLoggingSurface_IsOnConsoleManager()
    {
        Assert.That(typeof(ConsoleManager).GetProperty("Log", BindingFlags.Instance | BindingFlags.Public), Is.Null);
        Assert.That(typeof(ConsoleLog).IsPublic, Is.False);
        Assert.That(typeof(ConsoleManager).GetMethod(nameof(ConsoleManager.LogTrace), new[] { typeof(string) }), Is.Not.Null);
    }

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
    public void RecordEntry_PreservesLogMetadataInHistory()
    {
        var console = new ConsoleManager();
        var exception = new InvalidOperationException("Connection failed.");
        var entry = new LogEntry(
            DateTimeOffset.UnixEpoch,
            LogLevel.Error,
            "Could not connect.",
            source: "network.client",
            exception: exception);

        console.RecordEntry(entry);

        var recorded = (LogEntry)console.History.Entries[0];
        Assert.That(recorded.Source, Is.EqualTo("network.client"));
        Assert.That(recorded.Exception, Is.SameAs(exception));
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
