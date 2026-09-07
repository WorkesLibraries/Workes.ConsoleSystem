using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Presentation;

namespace Workes.ConsoleSystem.Tests.Commands;

public sealed class CommandOutputTests
{
    [Test]
    public void Inline_CreatesStringAuthoredInlineOutput()
    {
        var output = CommandOutput.Inline("Console ready.", "Information");

        Assert.That(output.Kind, Is.EqualTo(CommandOutputKind.Inline));
        Assert.That(output.IsText, Is.True);
        Assert.That(output.Text, Is.EqualTo("Console ready."));
        Assert.That(output.DefaultStyleId, Is.EqualTo("Information"));
        Assert.That(output.PlainText, Is.EqualTo("Console ready."));
        Assert.That(output.Segments, Is.Empty);
    }

    [Test]
    public void Block_CreatesStringAuthoredBlockOutput()
    {
        var output = CommandOutput.Block("help\nnoclip", "Information");

        Assert.That(output.Kind, Is.EqualTo(CommandOutputKind.Block));
        Assert.That(output.DefaultStyleId, Is.EqualTo("Information"));
        Assert.That(output.PlainText, Is.EqualTo("help\nnoclip"));
    }

    [Test]
    public void Builder_PreservesSegmentsStylesDataAndPlainText()
    {
        var amount = 10;

        var output = CommandOutput.BuildInline(defaultStyle: "Success")
            .Text("Gave ")
            .Value(amount.ToString(), style: "Amount", data: amount)
            .Text(" gold")
            .Build();

        Assert.That(output.Kind, Is.EqualTo(CommandOutputKind.Inline));
        Assert.That(output.DefaultStyleId, Is.EqualTo("Success"));
        Assert.That(output.PlainText, Is.EqualTo("Gave 10 gold"));
        Assert.That(output.Segments, Has.Count.EqualTo(3));
        Assert.That(output.Segments[1].Text, Is.EqualTo("10"));
        Assert.That(output.Segments[1].StyleId, Is.EqualTo("Amount"));
        Assert.That(output.Segments[1].Data, Is.EqualTo(10));
        Assert.That(output.ResolveStyleId(output.Segments[0]), Is.EqualTo("Success"));
        Assert.That(output.ResolveStyleId(output.Segments[1]), Is.EqualTo("Amount"));
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
        Assert.Throws<ArgumentNullException>(() => CommandOutput.Inline(null!));
        Assert.Throws<ArgumentNullException>(() => CommandOutput.BuildInline().Text(null!));
        Assert.Throws<ArgumentNullException>(() => CommandOutput.BuildInline().Value(null!));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void BlankStyleIdsThrow(string styleId)
    {
        Assert.Throws<ArgumentException>(() => CommandOutput.BuildInline(styleId));
        Assert.Throws<ArgumentException>(() => new ConsoleTextSegment("value", styleId));
    }

    [Test]
    public void Inline_FromConsoleTextPreservesStyledContent()
    {
        var content = ConsoleText.Build("Success")
            .Text("Gave ")
            .Value("10", "Amount")
            .Text(" gold.")
            .Build();

        var output = CommandOutput.Inline(content);

        Assert.That(output.Kind, Is.EqualTo(CommandOutputKind.Inline));
        Assert.That(output.IsText, Is.False);
        Assert.That(output.DefaultStyleId, Is.EqualTo("Success"));
        Assert.That(output.PlainText, Is.EqualTo("Gave 10 gold."));
        Assert.That(output.Segments[1].StyleId, Is.EqualTo("Amount"));
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

    [Test]
    public void Inline_StoresFormattingAwareTextAndDerivesPlainText()
    {
        var output = CommandOutput.Inline("Gave <style=Amount><b>10</b></style> gold.", "Success");

        Assert.That(output.Kind, Is.EqualTo(CommandOutputKind.Inline));
        Assert.That(output.IsText, Is.True);
        Assert.That(output.Text, Is.EqualTo("Gave <style=Amount><b>10</b></style> gold."));
        Assert.That(output.DefaultStyleId, Is.EqualTo("Success"));
        Assert.That(output.PlainText, Is.EqualTo("Gave 10 gold."));
        Assert.That(output.Content, Is.Null);
        Assert.That(output.Segments, Is.Empty);
    }

    [Test]
    public void Block_StoresFormattingAwareTextAndDerivesPlainText()
    {
        var output = CommandOutput.Block("help\n<style=Command>noclip</style>");

        Assert.That(output.Kind, Is.EqualTo(CommandOutputKind.Block));
        Assert.That(output.PlainText, Is.EqualTo("help\nnoclip"));
    }

    [TestCase("<b>missing")]
    [TestCase("</b>")]
    [TestCase("<b><i>x</b></i>")]
    [TestCase("<style=>x</style>")]
    [TestCase("<color=red>x</color>")]
    public void Inline_KnownInvalidMarkupShapeThrows(string markup)
    {
        Assert.Throws<FormatException>(() => CommandOutput.Inline(markup));
    }
}
