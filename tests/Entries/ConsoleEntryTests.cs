using System;
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Entries;
using Workes.ConsoleSystem.Logging;
using Workes.ConsoleSystem.Presentation;

namespace Workes.ConsoleSystem.Tests.Entries;

public sealed class ConsoleEntryTests
{
    [Test]
    public void LogEntry_PreservesConstructorValues()
    {
        DateTimeOffset timestamp = DateTimeOffset.UnixEpoch;
        var entry = new LogEntry(timestamp, LogLevel.Warning, "Inventory is nearly full.");

        Assert.That(entry.Timestamp, Is.EqualTo(timestamp));
        Assert.That(entry.Level, Is.EqualTo(LogLevel.Warning));
        Assert.That(entry.Message, Is.EqualTo("Inventory is nearly full."));
        Assert.That(entry.Content.PlainText, Is.EqualTo("Inventory is nearly full."));
    }

    [Test]
    public void LogEntry_PreservesSemanticContent()
    {
        var content = ConsoleText.Plain("Inventory is nearly full.", "Warning");
        var entry = new LogEntry(DateTimeOffset.UnixEpoch, LogLevel.Warning, content);

        Assert.That(entry.Content, Is.SameAs(content));
        Assert.That(entry.Message, Is.EqualTo("Inventory is nearly full."));
    }

    [Test]
    public void LogEntry_PreservesOptionalSourceAndExceptionMetadata()
    {
        var exception = new InvalidOperationException("Connection failed.");
        var entry = new LogEntry(
            DateTimeOffset.UnixEpoch,
            LogLevel.Error,
            "Could not connect.",
            source: "network.client",
            exception: exception);

        Assert.That(entry.Source, Is.EqualTo("network.client"));
        Assert.That(entry.Exception, Is.SameAs(exception));
    }

    [Test]
    public void LogEntry_MetadataDefaultsToNullForExistingConstructionPatterns()
    {
        var stringEntry = new LogEntry(DateTimeOffset.UnixEpoch, LogLevel.Information, "Ready.");
        var contentEntry = new LogEntry(DateTimeOffset.UnixEpoch, LogLevel.Information, ConsoleText.Plain("Ready."));

        Assert.That(stringEntry.Source, Is.Null);
        Assert.That(stringEntry.Exception, Is.Null);
        Assert.That(contentEntry.Source, Is.Null);
        Assert.That(contentEntry.Exception, Is.Null);
        Assert.That(
            typeof(LogEntry).GetConstructor(new[] { typeof(DateTimeOffset), typeof(LogLevel), typeof(string) }),
            Is.Not.Null);
        Assert.That(
            typeof(LogEntry).GetConstructor(new[] { typeof(DateTimeOffset), typeof(LogLevel), typeof(ConsoleText) }),
            Is.Not.Null);
    }

    [Test]
    public void LogEntry_SourceAndExceptionCanBeProvidedIndependently()
    {
        var exception = new InvalidOperationException("Connection failed.");
        var sourceOnly = new LogEntry(DateTimeOffset.UnixEpoch, LogLevel.Error, "Failed.", "network.client");
        var exceptionOnly = new LogEntry(
            DateTimeOffset.UnixEpoch,
            LogLevel.Error,
            "Failed.",
            source: null,
            exception: exception);

        Assert.That(sourceOnly.Source, Is.EqualTo("network.client"));
        Assert.That(sourceOnly.Exception, Is.Null);
        Assert.That(exceptionOnly.Source, Is.Null);
        Assert.That(exceptionOnly.Exception, Is.SameAs(exception));
    }

    [Test]
    public void LogEntry_NullMessageOrContentThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new LogEntry(DateTimeOffset.UnixEpoch, LogLevel.Warning, (string)null!));
        Assert.Throws<ArgumentNullException>(() => new LogEntry(DateTimeOffset.UnixEpoch, LogLevel.Warning, (ConsoleText)null!));
    }

    [Test]
    public void CommandInputEntry_PreservesConstructorValues()
    {
        var entry = new CommandInputEntry(DateTimeOffset.UnixEpoch, "player heal");

        Assert.That(entry.Timestamp, Is.EqualTo(DateTimeOffset.UnixEpoch));
        Assert.That(entry.Input, Is.EqualTo("player heal"));
        Assert.That(entry.Content.PlainText, Is.EqualTo("player heal"));
    }

    [Test]
    public void CommandInputEntry_PreservesSemanticContent()
    {
        var content = ConsoleText.Plain("player heal", "CommandInput");
        var entry = new CommandInputEntry(DateTimeOffset.UnixEpoch, content);

        Assert.That(entry.Content, Is.SameAs(content));
        Assert.That(entry.Input, Is.EqualTo("player heal"));
    }

    [Test]
    public void CommandInputEntry_NullInputOrContentThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new CommandInputEntry(DateTimeOffset.UnixEpoch, (string)null!));
        Assert.Throws<ArgumentNullException>(() => new CommandInputEntry(DateTimeOffset.UnixEpoch, (ConsoleText)null!));
    }

    [Test]
    public void CommandOutputEntry_PreservesConstructorValues()
    {
        CommandOutput output = CommandOutput.Inline("Unknown command.");
        var entry = new CommandOutputEntry(DateTimeOffset.UnixEpoch, output);

        Assert.That(entry.Timestamp, Is.EqualTo(DateTimeOffset.UnixEpoch));
        Assert.That(entry.Output, Is.SameAs(output));
        Assert.That(entry.PlainText, Is.EqualTo("Unknown command."));
    }

    [Test]
    public void CommandOutputEntry_NullOutputThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new CommandOutputEntry(DateTimeOffset.UnixEpoch, null!));
    }

    [Test]
    public void CommandFailureEntry_PreservesConstructorValues()
    {
        ConsoleFailure failure = ConsoleFailure.Create(
            ConsoleFailureKind.CommandExecution,
            ConsoleFailureCodes.CommandExecutionRejected,
            "Command failed.");
        var entry = new CommandFailureEntry(DateTimeOffset.UnixEpoch, failure);

        Assert.That(entry.Timestamp, Is.EqualTo(DateTimeOffset.UnixEpoch));
        Assert.That(entry.Failure, Is.SameAs(failure));
        Assert.That(entry.Message, Is.EqualTo("Command failed."));
        Assert.That(entry.Content.DefaultSemanticId, Is.Null);
    }

    [Test]
    public void CommandFailureEntry_PreservesSemanticContent()
    {
        ConsoleFailure failure = ConsoleFailure.Create(
            ConsoleFailureKind.CommandExecution,
            ConsoleFailureCodes.CommandExecutionRejected,
            "Command failed.");
        var content = ConsoleText.Plain("Visible failure.", "Warning");
        var entry = new CommandFailureEntry(DateTimeOffset.UnixEpoch, failure, content);

        Assert.That(entry.Failure, Is.SameAs(failure));
        Assert.That(entry.Content, Is.SameAs(content));
        Assert.That(entry.Message, Is.EqualTo("Visible failure."));
    }

    [Test]
    public void CommandFailureEntry_NullValuesThrow()
    {
        ConsoleFailure failure = ConsoleFailure.Create(
            ConsoleFailureKind.CommandExecution,
            ConsoleFailureCodes.CommandExecutionRejected,
            "Command failed.");

        Assert.Throws<ArgumentNullException>(() => new CommandFailureEntry(DateTimeOffset.UnixEpoch, null!));
        Assert.Throws<ArgumentNullException>(() => new CommandFailureEntry(DateTimeOffset.UnixEpoch, failure, null!));
    }
}
