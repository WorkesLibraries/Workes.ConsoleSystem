using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Entries;
using Workes.ConsoleSystem.Logging;

namespace Workes.ConsoleSystem.Tests.Entries;

public sealed class ConsoleEntryTests
{
    [Test]
    public void LogEntry_PreservesConstructorValues()
    {
        var timestamp = new DateTimeOffset(2026, 6, 19, 12, 30, 0, TimeSpan.Zero);

        var entry = new LogEntry(timestamp, LogLevel.Warning, "Inventory is nearly full.");

        Assert.That(entry.Timestamp, Is.EqualTo(timestamp));
        Assert.That(entry.Level, Is.EqualTo(LogLevel.Warning));
        Assert.That(entry.Message, Is.EqualTo("Inventory is nearly full."));
    }

    [Test]
    public void CommandInputEntry_PreservesConstructorValues()
    {
        var timestamp = new DateTimeOffset(2026, 6, 19, 12, 31, 0, TimeSpan.Zero);

        var entry = new CommandInputEntry(timestamp, "player heal");

        Assert.That(entry.Timestamp, Is.EqualTo(timestamp));
        Assert.That(entry.Input, Is.EqualTo("player heal"));
    }

    [Test]
    public void CommandOutputEntry_PreservesConstructorValues()
    {
        var timestamp = new DateTimeOffset(2026, 6, 19, 12, 32, 0, TimeSpan.Zero);
        var output = CommandOutput.InlineText("Unknown command.", "Error");

        var entry = new CommandOutputEntry(timestamp, output);

        Assert.That(entry.Timestamp, Is.EqualTo(timestamp));
        Assert.That(entry.Output, Is.SameAs(output));
        Assert.That(entry.PlainText, Is.EqualTo("Unknown command."));
    }

    [Test]
    public void CommandOutputEntry_NullOutputThrows()
    {
        var timestamp = new DateTimeOffset(2026, 6, 19, 12, 32, 0, TimeSpan.Zero);

        Assert.Throws<ArgumentNullException>(() => new CommandOutputEntry(timestamp, null!));
    }
}
