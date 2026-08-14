using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Presentation;

namespace Workes.ConsoleSystem.Tests.Commands;

public sealed class CommandOutputTests
{
    [Test]
    public void InlineText_CreatesSimpleInlineOutput()
    {
        var output = CommandOutput.InlineText("Console ready.", "Information");

        Assert.That(output.Kind, Is.EqualTo(CommandOutputKind.Inline));
        Assert.That(output.DefaultStyleId, Is.EqualTo("Information"));
        Assert.That(output.PlainText, Is.EqualTo("Console ready."));
        Assert.That(output.Segments, Has.Count.EqualTo(1));
        Assert.That(output.ResolveStyleId(output.Segments[0]), Is.EqualTo("Information"));
    }

    [Test]
    public void BlockText_CreatesSimpleBlockOutput()
    {
        var output = CommandOutput.BlockText("help\nnoclip", "Information");

        Assert.That(output.Kind, Is.EqualTo(CommandOutputKind.Block));
        Assert.That(output.DefaultStyleId, Is.EqualTo("Information"));
        Assert.That(output.PlainText, Is.EqualTo("help\nnoclip"));
    }

    [Test]
    public void Builder_PreservesSegmentsStylesDataAndPlainText()
    {
        var amount = 10;

        var output = CommandOutput.Inline(defaultStyle: "Success")
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
        var output = CommandOutput.Inline().Build();

        Assert.That(output.PlainText, Is.Empty);
        Assert.That(output.Segments, Is.Empty);
    }

    [Test]
    public void NullTextThrows()
    {
        Assert.Throws<ArgumentNullException>(() => CommandOutput.InlineText(null!));
        Assert.Throws<ArgumentNullException>(() => CommandOutput.Inline().Text(null!));
        Assert.Throws<ArgumentNullException>(() => CommandOutput.Inline().Value(null!));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void BlankStyleIdsThrow(string styleId)
    {
        Assert.Throws<ArgumentException>(() => CommandOutput.Inline(styleId));
        Assert.Throws<ArgumentException>(() => new ConsoleTextSegment("value", styleId));
    }

    [Test]
    public void InlineText_FromConsoleTextPreservesStyledContent()
    {
        var content = ConsoleText.Build("Success")
            .Text("Gave ")
            .Value("10", "Amount")
            .Text(" gold.")
            .Build();

        var output = CommandOutput.InlineText(content);

        Assert.That(output.Kind, Is.EqualTo(CommandOutputKind.Inline));
        Assert.That(output.DefaultStyleId, Is.EqualTo("Success"));
        Assert.That(output.PlainText, Is.EqualTo("Gave 10 gold."));
        Assert.That(output.Segments[1].StyleId, Is.EqualTo("Amount"));
    }

    [Test]
    public void BlockText_FromConsoleTextCreatesBlockOutput()
    {
        var content = ConsoleText.Build()
            .Text("help\n")
            .Value("noclip", "Command")
            .Build();

        var output = CommandOutput.BlockText(content);

        Assert.That(output.Kind, Is.EqualTo(CommandOutputKind.Block));
        Assert.That(output.PlainText, Is.EqualTo("help\nnoclip"));
    }

    [Test]
    public void InlineMarkup_StoresMarkupAndDerivesPlainText()
    {
        var output = CommandOutput.InlineMarkup("Gave <style=Amount><b>10</b></style> gold.", "Success");

        Assert.That(output.Kind, Is.EqualTo(CommandOutputKind.Inline));
        Assert.That(output.IsMarkup, Is.True);
        Assert.That(output.Markup, Is.EqualTo("Gave <style=Amount><b>10</b></style> gold."));
        Assert.That(output.DefaultStyleId, Is.EqualTo("Success"));
        Assert.That(output.PlainText, Is.EqualTo("Gave 10 gold."));
        Assert.That(output.Content, Is.Null);
        Assert.That(output.Segments, Is.Empty);
    }

    [Test]
    public void BlockMarkup_StoresMarkupAndDerivesPlainText()
    {
        var output = CommandOutput.BlockMarkup("help\n<style=Command>noclip</style>");

        Assert.That(output.Kind, Is.EqualTo(CommandOutputKind.Block));
        Assert.That(output.PlainText, Is.EqualTo("help\nnoclip"));
    }

    [TestCase("<b>missing")]
    [TestCase("</b>")]
    [TestCase("<b><i>x</b></i>")]
    public void Markup_InvalidMarkupShapeThrows(string markup)
    {
        Assert.Throws<FormatException>(() => CommandOutput.InlineMarkup(markup));
    }
}
