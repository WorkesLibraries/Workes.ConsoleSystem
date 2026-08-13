using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.History;

namespace Workes.ConsoleSystem.Tests.History;

public sealed class CommandHistoryTests
{
    [Test]
    public void DefaultConstructor_UsesDefaultCapacity()
    {
        var history = new CommandHistory();

        Assert.That(history.Capacity, Is.EqualTo(100));
    }

    [Test]
    public void Add_StoresValidInputAndReturnsTrue()
    {
        var console = new ConsoleManager();

        bool added = console.RecordCommandInput("noclip");

        Assert.That(added, Is.True);
        Assert.That(console.CommandHistory.Entries, Is.EqualTo(new[] { "noclip" }));
    }

    [Test]
    public void Add_NullInputThrows()
    {
        var console = new ConsoleManager();

        Assert.Throws<ArgumentNullException>(() => console.RecordCommandInput(null!));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Add_BlankInputIsIgnored(string input)
    {
        var console = new ConsoleManager();

        bool added = console.RecordCommandInput(input);

        Assert.That(added, Is.False);
        Assert.That(console.CommandHistory.Entries, Is.Empty);
    }

    [Test]
    public void Add_WhenCapacityIsReached_DropsOldestInput()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            History = new HistoryOptions
            {
                CommandHistoryCapacity = 2
            }
        });

        console.RecordCommandInput("first");
        console.RecordCommandInput("second");
        console.RecordCommandInput("third");

        Assert.That(console.CommandHistory.Entries, Is.EqualTo(new[] { "second", "third" }));
    }

    [Test]
    public void Add_ByDefaultRejectsConsecutiveDuplicateUsingTrimmedCaseInsensitiveComparison()
    {
        var console = new ConsoleManager();

        bool firstAdded = console.RecordCommandInput("  noclip  ");
        bool duplicateAdded = console.RecordCommandInput("NOCLIP");

        Assert.That(firstAdded, Is.True);
        Assert.That(duplicateAdded, Is.False);
        Assert.That(console.CommandHistory.Entries, Is.EqualTo(new[] { "  noclip  " }));
    }

    [Test]
    public void Add_DefaultDuplicateRejectionAllowsDistinctCommands()
    {
        var console = new ConsoleManager();

        console.RecordCommandInput("noclip");
        bool added = console.RecordCommandInput("noclip --invisible");

        Assert.That(added, Is.True);
        Assert.That(console.CommandHistory.Entries, Is.EqualTo(new[] { "noclip", "noclip --invisible" }));
    }

    [Test]
    public void Add_DefaultDuplicateRejectionAllowsNonConsecutiveDuplicates()
    {
        var console = new ConsoleManager();

        console.RecordCommandInput("noclip");
        console.RecordCommandInput("help");
        bool added = console.RecordCommandInput("Noclip");

        Assert.That(added, Is.True);
        Assert.That(console.CommandHistory.Entries, Is.EqualTo(new[] { "noclip", "help", "Noclip" }));
    }

    [Test]
    public void Add_WhenDuplicatePolicyAllows_StoresConsecutiveDuplicates()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            History = new HistoryOptions
            {
                CommandHistoryDuplicatePolicy = CommandHistoryDuplicatePolicy.Allow
            }
        });

        console.RecordCommandInput("noclip");
        bool duplicateAdded = console.RecordCommandInput("NOCLIP");

        Assert.That(duplicateAdded, Is.True);
        Assert.That(console.CommandHistory.Entries, Is.EqualTo(new[] { "noclip", "NOCLIP" }));
    }

    [Test]
    public void Add_PreservesOriginalInput()
    {
        var console = new ConsoleManager();

        console.RecordCommandInput("  NoClip  ");

        Assert.That(console.CommandHistory.Entries, Is.EqualTo(new[] { "  NoClip  " }));
    }

    [Test]
    public void Clear_RemovesAllInputs()
    {
        var console = new ConsoleManager();

        console.RecordCommandInput("first");
        console.RecordCommandInput("second");

        console.CommandHistory.Clear();

        Assert.That(console.CommandHistory.Entries, Is.Empty);
    }
}
