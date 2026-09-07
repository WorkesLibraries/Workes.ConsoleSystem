using System.Text;
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;

namespace Workes.ConsoleSystem.Tests.Examples.Autocomplete;

[Category("Example")]
public sealed class AutocompleteExampleTests
{
    [Test]
    public void CommandAutocomplete_WritesReadableExample()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            Autocomplete = new CommandAutocompleteOptions
            {
                PathCompletionMode = CommandPathCompletionMode.DotSegment
            }
        });

        console.RegisterCommand(new CommandBuilder("Player.Inventory.SetModifier")
            .Argument<ModifierState>(x => x.Item, "item")
                .ValueCandidates(ctx => new[] { "wood", "stone", "gold" })
            .Option<ModifierState>(x => x.Amount, "amount", "a")
                .ValueCandidates(ctx => new[] { "1", "10", "100" })
            .Execute<ModifierState>((ctx, state) => new CommandResult())
            .Build());

        CommandAutocompleteResult path = console.GetAutocomplete("Pl", 2);
        string completedPath = path.Apply(0);

        CommandAutocompleteResult item = console.GetAutocomplete("Player.Inventory.SetModifier g", "Player.Inventory.SetModifier g".Length);
        string completedItem = item.Apply(0);

        CommandAutocompleteResult option = console.GetAutocomplete("Player.Inventory.SetModifier gold --a", "Player.Inventory.SetModifier gold --a".Length);
        string completedOption = option.Apply(0);

        var builder = new StringBuilder();
        builder.AppendLine("Autocomplete Example");
        builder.AppendLine();
        builder.AppendLine($"Path candidate: {path.Candidates[0].DisplayText}");
        builder.AppendLine($"Completed path: {completedPath}");
        builder.AppendLine($"Item candidate: {item.Candidates[0].DisplayText}");
        builder.AppendLine($"Completed item: {completedItem}");
        builder.AppendLine($"Option candidate: {option.Candidates[0].DisplayText}");
        builder.AppendLine($"Completed option: {completedOption}");

        string outputPath = WriteExample("Autocomplete", "AutocompleteExample.txt", builder.ToString());

        Assert.That(completedPath, Is.EqualTo("Player"));
        Assert.That(completedItem, Is.EqualTo("Player.Inventory.SetModifier gold"));
        Assert.That(completedOption, Is.EqualTo("Player.Inventory.SetModifier gold --amount"));
        Assert.That(File.Exists(outputPath), Is.True);
    }

    private static string WriteExample(string area, string fileName, string content)
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "ExampleOutputs", area);
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    private sealed record ModifierState(string Item, int Amount);
}
