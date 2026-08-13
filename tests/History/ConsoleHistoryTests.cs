using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Entries;

namespace Workes.ConsoleSystem.Tests.History;

public sealed class ConsoleHistoryTests
{
    [Test]
    public void DefaultConstructor_UsesDefaultCapacity()
    {
        var history = new Workes.ConsoleSystem.History.ConsoleHistory();

        Assert.That(history.Capacity, Is.EqualTo(200));
    }

    [Test]
    public void Add_WhenCapacityIsReached_DropsOldestEntry()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            History = new HistoryOptions
            {
                ConsoleHistoryCapacity = 2
            }
        });

        console.LogInformation("First");
        console.LogInformation("Second");
        console.LogInformation("Third");

        Assert.That(console.History.Entries, Has.Count.EqualTo(2));
        Assert.That(((LogEntry)console.History.Entries[0]).Message, Is.EqualTo("Second"));
        Assert.That(((LogEntry)console.History.Entries[1]).Message, Is.EqualTo("Third"));
    }

    [Test]
    public void Clear_RemovesAllEntries()
    {
        var console = new ConsoleManager();

        console.LogInformation("First");
        console.LogWarning("Second");

        console.History.Clear();

        Assert.That(console.History.Entries, Is.Empty);
    }
}
