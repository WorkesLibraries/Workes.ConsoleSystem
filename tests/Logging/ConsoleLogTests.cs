using Workes.ConsoleSystem.Entries;
using Workes.ConsoleSystem.Logging;
using Workes.ConsoleSystem.Core;

namespace Workes.ConsoleSystem.Tests.Logging;

public sealed class ConsoleLogTests
{
    [Test]
    public void Information_AddsLogEntryToSharedHistory()
    {
        var console = new GameConsole();

        console.Log.Information("Game started.");

        Assert.That(console.History.Entries, Has.Count.EqualTo(1));

        var entry = (LogEntry)console.History.Entries[0];
        Assert.That(entry.Level, Is.EqualTo(LogLevel.Information));
        Assert.That(entry.Message, Is.EqualTo("Game started."));
    }
}
