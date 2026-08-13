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
        var history = new CommandHistory();

        bool added = history.Add("noclip");

        Assert.That(added, Is.True);
        Assert.That(history.Entries, Is.EqualTo(new[] { "noclip" }));
    }

    [Test]
    public void Add_NullInputThrows()
    {
        var history = new CommandHistory();

        Assert.Throws<ArgumentNullException>(() => history.Add(null!));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Add_BlankInputIsIgnored(string input)
    {
        var history = new CommandHistory();

        bool added = history.Add(input);

        Assert.That(added, Is.False);
        Assert.That(history.Entries, Is.Empty);
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

        console.CommandHistory.Add("first");
        console.CommandHistory.Add("second");
        console.CommandHistory.Add("third");

        Assert.That(console.CommandHistory.Entries, Is.EqualTo(new[] { "second", "third" }));
    }

    [Test]
    public void Add_ByDefaultRejectsConsecutiveDuplicateUsingTrimmedCaseInsensitiveComparison()
    {
        var history = new CommandHistory();

        bool firstAdded = history.Add("  noclip  ");
        bool duplicateAdded = history.Add("NOCLIP");

        Assert.That(firstAdded, Is.True);
        Assert.That(duplicateAdded, Is.False);
        Assert.That(history.Entries, Is.EqualTo(new[] { "  noclip  " }));
    }

    [Test]
    public void Add_DefaultDuplicateRejectionAllowsDistinctCommands()
    {
        var history = new CommandHistory();

        history.Add("noclip");
        bool added = history.Add("noclip --invisible");

        Assert.That(added, Is.True);
        Assert.That(history.Entries, Is.EqualTo(new[] { "noclip", "noclip --invisible" }));
    }

    [Test]
    public void Add_DefaultDuplicateRejectionAllowsNonConsecutiveDuplicates()
    {
        var history = new CommandHistory();

        history.Add("noclip");
        history.Add("help");
        bool added = history.Add("Noclip");

        Assert.That(added, Is.True);
        Assert.That(history.Entries, Is.EqualTo(new[] { "noclip", "help", "Noclip" }));
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

        console.CommandHistory.Add("noclip");
        bool duplicateAdded = console.CommandHistory.Add("NOCLIP");

        Assert.That(duplicateAdded, Is.True);
        Assert.That(console.CommandHistory.Entries, Is.EqualTo(new[] { "noclip", "NOCLIP" }));
    }

    [Test]
    public void Add_PreservesOriginalInput()
    {
        var history = new CommandHistory();

        history.Add("  NoClip  ");

        Assert.That(history.Entries, Is.EqualTo(new[] { "  NoClip  " }));
    }

    [Test]
    public void Clear_RemovesAllInputs()
    {
        var history = new CommandHistory();

        history.Add("first");
        history.Add("second");

        history.Clear();

        Assert.That(history.Entries, Is.Empty);
    }
}
