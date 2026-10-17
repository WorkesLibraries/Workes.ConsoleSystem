using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Presentation;

namespace Workes.ConsoleSystem.Tests.Commands;

public sealed class CommandOutputTests
{
    [Test]
    public void Inline_CreatesLiteralPlainInlineOutput()
    {
        var output = CommandOutput.Inline("<b>Console ready.</b>");

        Assert.That(output.Kind, Is.EqualTo(CommandOutputKind.Inline));
        Assert.That(output.Content.PlainText, Is.EqualTo("<b>Console ready.</b>"));
        Assert.That(output.DefaultSemanticId, Is.Null);
        Assert.That(output.PlainText, Is.EqualTo("<b>Console ready.</b>"));
        Assert.That(output.Segments, Has.Count.EqualTo(1));
    }

    [Test]
    public void Block_CreatesLiteralPlainBlockOutput()
    {
        var output = CommandOutput.Block("help\n<style=Command>noclip</style>");

        Assert.That(output.Kind, Is.EqualTo(CommandOutputKind.Block));
        Assert.That(output.PlainText, Is.EqualTo("help\n<style=Command>noclip</style>"));
    }

    [Test]
    public void Builder_PreservesSegmentsSemanticIdsDataAndPlainText()
    {
        var amount = 10;

        var output = CommandOutput.BuildInline(defaultSemanticId: "Success")
            .Text("Gave ")
            .Value(amount.ToString(), semanticId: "Amount", data: amount)
            .Text(" gold")
            .Build();

        Assert.That(output.Kind, Is.EqualTo(CommandOutputKind.Inline));
        Assert.That(output.DefaultSemanticId, Is.EqualTo("Success"));
        Assert.That(output.PlainText, Is.EqualTo("Gave 10 gold"));
        Assert.That(output.Segments, Has.Count.EqualTo(3));
        Assert.That(output.Segments[1].Text, Is.EqualTo("10"));
        Assert.That(output.Segments[1].SemanticId, Is.EqualTo("Amount"));
        Assert.That(output.Segments[1].Data, Is.EqualTo(10));
        Assert.That(output.ResolveSemanticId(output.Segments[0]), Is.EqualTo("Success"));
        Assert.That(output.ResolveSemanticId(output.Segments[1]), Is.EqualTo("Amount"));
    }

    [Test]
    public void Builder_CanCreateEmptyOutput()
    {
        var output = CommandOutput.BuildInline().Build();

        Assert.That(output.PlainText, Is.Empty);
        Assert.That(output.Segments, Is.Empty);
    }

    [Test]
    public void NullTextThrows()
    {
        Assert.Throws<ArgumentNullException>(() => CommandOutput.Inline((string)null!));
        Assert.Throws<ArgumentNullException>(() => CommandOutput.Inline((ConsoleText)null!));
        Assert.Throws<ArgumentNullException>(() => CommandOutput.BuildInline().Text(null!));
        Assert.Throws<ArgumentNullException>(() => CommandOutput.BuildInline().Value(null!));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void BlankSemanticIdsThrow(string semanticId)
    {
        Assert.Throws<ArgumentException>(() => CommandOutput.BuildInline(semanticId));
        Assert.Throws<ArgumentException>(() => new ConsoleTextSegment("value", semanticId));
    }

    [Test]
    public void Inline_FromConsoleTextPreservesSemanticContent()
    {
        var content = ConsoleText.Build("Success")
            .Text("Gave ")
            .Value("10", "Amount")
            .Text(" gold.")
            .Build();

        var output = CommandOutput.Inline(content);

        Assert.That(output.Kind, Is.EqualTo(CommandOutputKind.Inline));
        Assert.That(output.Content, Is.SameAs(content));
        Assert.That(output.DefaultSemanticId, Is.EqualTo("Success"));
        Assert.That(output.PlainText, Is.EqualTo("Gave 10 gold."));
        Assert.That(output.Segments[1].SemanticId, Is.EqualTo("Amount"));
    }

    [Test]
    public void Block_FromConsoleTextCreatesBlockOutput()
    {
        var content = ConsoleText.Build()
            .Text("help\n")
            .Value("noclip", "Command")
            .Build();

        var output = CommandOutput.Block(content);

        Assert.That(output.Kind, Is.EqualTo(CommandOutputKind.Block));
        Assert.That(output.PlainText, Is.EqualTo("help\nnoclip"));
    }
}
