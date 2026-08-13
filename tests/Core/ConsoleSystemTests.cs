using Workes.ConsoleSystem.Core;

namespace Workes.ConsoleSystem.Tests.Core;

public sealed class ConsoleSystemTests
{
    [Test]
    public void Constructor_CreatesCoreComponents()
    {
        var console = new ConsoleManager();

        Assert.That(console.History, Is.Not.Null);
        Assert.That(console.CommandHistory, Is.Not.Null);
        Assert.That(console.Log, Is.Not.Null);
        Assert.That(console.Commands, Is.Not.Null);
    }

    [Test]
    public void Constructor_InitialHistoriesAreEmpty()
    {
        var console = new ConsoleManager();

        Assert.That(console.History.Entries, Is.Empty);
        Assert.That(console.CommandHistory.Entries, Is.Empty);
    }
}
