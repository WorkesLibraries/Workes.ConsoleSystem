using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Presentation;
using ConsoleColor = Workes.ConsoleSystem.Presentation.ConsoleColor;

namespace Workes.ConsoleSystem.Tests.Presentation;

public sealed class ConsoleTextTests
{
    [Test]
    public void Plain_DoesNotParseMarkup()
    {
        var text = ConsoleText.Plain("<b>literal</b>", "Information");

        Assert.That(text.PlainText, Is.EqualTo("<b>literal</b>"));
        Assert.That(text.DefaultStyleId, Is.EqualTo("Information"));
        Assert.That(text.Segments, Has.Count.EqualTo(1));
        Assert.That(text.ResolveStyleId(text.Segments[0]), Is.EqualTo("Information"));
    }

    [Test]
    public void CreateText_WithFormattingEnabledParsesNestedTags()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            Formatting = ConsoleFormattingOptions.UnityRichText()
        });
        var text = console.CreateText("Gave <style=Amount><color=#4ade80><b>7</b></color></style> wood");

        Assert.That(text.PlainText, Is.EqualTo("Gave 7 wood"));
        Assert.That(text.Segments, Has.Count.EqualTo(3));
        Assert.That(text.Segments[1].Text, Is.EqualTo("7"));
        Assert.That(text.Segments[1].StyleId, Is.EqualTo("Amount"));
        Assert.That(text.Segments[1].InlineStyle!.GetValue<ConsoleColor>(ConsoleStandardFormatAttributes.ForegroundColor), Is.EqualTo(ConsoleColor.FromHex("#4ade80")));
        Assert.That(text.Segments[1].InlineStyle!.HasFlag(ConsoleStandardFormatAttributes.Bold), Is.True);
    }

    [Test]
    public void CreateText_WithFormattingEnabledDecodesEntities()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            Formatting = ConsoleFormattingOptions.UnityRichText()
        });
        var text = console.CreateText("&lt;b&gt; &amp;");

        Assert.That(text.PlainText, Is.EqualTo("<b> &"));
    }

    [Test]
    public void CreateText_WithFormattingDisabledKeepsMarkupLiteral()
    {
        var console = new ConsoleManager();

        var text = console.CreateText("<b>literal</b>", "Information");

        Assert.That(text.PlainText, Is.EqualTo("<b>literal</b>"));
        Assert.That(text.DefaultStyleId, Is.EqualTo("Information"));
    }

    [TestCase("Use <something> here")]
    [TestCase("value < 10")]
    [TestCase("List<string>")]
    [TestCase("a < b > c")]
    public void CreateText_UnknownTagsAndOrdinaryAnglesStayLiteral(string textValue)
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            Formatting = ConsoleFormattingOptions.UnityRichText()
        });

        var text = console.CreateText(textValue);

        Assert.That(text.PlainText, Is.EqualTo(textValue));
    }

    [Test]
    public void CreateText_UnknownTagsAroundKnownMarkupStayLiteral()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            Formatting = ConsoleFormattingOptions.UnityRichText()
        });

        var text = console.CreateText("Use <something><b>this</b></something> here");

        Assert.That(text.PlainText, Is.EqualTo("Use <something>this</something> here"));
    }

    [TestCase("<b>missing")]
    [TestCase("</b>")]
    [TestCase("<b><i>x</b></i>")]
    [TestCase("<style=>x</style>")]
    [TestCase("<color=red>x</color>")]
    public void CreateText_KnownInvalidMarkupThrows(string markup)
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            Formatting = ConsoleFormattingOptions.UnityRichText()
        });

        Assert.Throws<FormatException>(() => console.CreateText(markup));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void BlankStyleIdsThrow(string style)
    {
        Assert.Throws<ArgumentException>(() => ConsoleText.Plain("x", style));
        Assert.Throws<ArgumentException>(() => ConsoleText.Build(style));
        Assert.Throws<ArgumentException>(() => new ConsoleTextSegment("x", style));
    }
}
