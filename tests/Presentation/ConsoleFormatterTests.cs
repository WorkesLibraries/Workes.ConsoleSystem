using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Presentation;
using ConsoleColor = Workes.ConsoleSystem.Presentation.ConsoleColor;

namespace Workes.ConsoleSystem.Tests.Presentation;

public sealed class ConsoleFormatterTests
{
    [Test]
    public void PlainTextFormatter_ReturnsPlainText()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            Formatting = ConsoleFormattingOptions.Standard(new PlainTextConsoleFormatter())
        });
        var text = console.CreateText("<style=Success><b>Saved</b></style>");

        string formatted = console.Format(text);

        Assert.That(formatted, Is.EqualTo("Saved"));
    }

    [Test]
    public void UnityFormatter_FormatsThemeAndInlineStyles()
    {
        var theme = new ConsoleTheme(new Dictionary<string, ConsoleStyle>
        {
            ["Success"] = ConsoleStyle.Standard(foregroundColor: ConsoleColor.FromHex("#4ade80"))
        });
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            Formatting = ConsoleFormattingOptions.UnityRichText(theme)
        });
        var text = console.CreateText("<style=Success><b>Saved</b></style>");

        string formatted = console.Format(text);

        Assert.That(formatted, Is.EqualTo("<color=#4ADE80><b>Saved</b></color>"));
    }

    [Test]
    public void UnityFormatter_FormatsUnderline()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            Formatting = ConsoleFormattingOptions.UnityRichText()
        });
        var text = console.CreateText("<u>Saved</u>");

        string formatted = console.Format(text);

        Assert.That(formatted, Is.EqualTo("<u>Saved</u>"));
    }

    [Test]
    public void GodotFormatter_FormatsThemeAndInlineStyles()
    {
        var theme = new ConsoleTheme(new Dictionary<string, ConsoleStyle>
        {
            ["Success"] = ConsoleStyle.Standard(foregroundColor: ConsoleColor.FromHex("#4ade80"))
        });
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            Formatting = ConsoleFormattingOptions.GodotBbCode(theme)
        });
        var text = console.CreateText("<style=Success><u>Saved</u></style>");

        string formatted = console.Format(text);

        Assert.That(formatted, Is.EqualTo("[color=#4ADE80][u]Saved[/u][/color]"));
    }

    [Test]
    public void InlineColorOverridesThemeColor()
    {
        var theme = new ConsoleTheme(new Dictionary<string, ConsoleStyle>
        {
            ["Success"] = ConsoleStyle.Standard(foregroundColor: ConsoleColor.FromHex("#4ade80"))
        });
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            Formatting = ConsoleFormattingOptions.UnityRichText(theme)
        });
        var text = console.CreateText("<style=Success><color=#ff0000>Saved</color></style>");

        string formatted = console.Format(text);

        Assert.That(formatted, Is.EqualTo("<color=#FF0000>Saved</color>"));
    }

    [Test]
    public void Theme_InvalidStylesThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new ConsoleTheme(null!));
        Assert.Throws<ArgumentException>(() => new ConsoleTheme(new Dictionary<string, ConsoleStyle>
        {
            [""] = new ConsoleStyle()
        }));
        Assert.Throws<ArgumentException>(() => new ConsoleTheme(new Dictionary<string, ConsoleStyle>
        {
            ["Success"] = null!
        }));
        Assert.Throws<ArgumentException>(() => new ConsoleTheme().TryGetStyle("", out _));
    }

    [Test]
    public void FormattingDisabled_FormattingAwareStringsStayLiteral()
    {
        var console = new ConsoleManager();

        Assert.That(console.CreateText("<b>x</b>").PlainText, Is.EqualTo("<b>x</b>"));
        Assert.That(console.Format(ConsoleText.Plain("x")), Is.EqualTo("x"));
        Assert.That(console.Format(CommandOutput.Inline("<b>x</b>")), Is.EqualTo("<b>x</b>"));
    }

    [Test]
    public void Manager_FormatsStringAuthoredCommandOutput()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            Formatting = ConsoleFormattingOptions.UnityRichText()
        });
        var output = CommandOutput.Inline("<b>Saved</b>");

        string formatted = console.Format(output);

        Assert.That(formatted, Is.EqualTo("<b>Saved</b>"));
    }

    [Test]
    public void Manager_ResolvesStringAuthoredCommandOutput()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            Formatting = ConsoleFormattingOptions.UnityRichText()
        });
        var output = CommandOutput.Inline("<style=Success><b>Saved</b></style>");

        ConsoleText text = console.ResolveOutput(output);

        Assert.That(text.PlainText, Is.EqualTo("Saved"));
        Assert.That(text.Segments, Has.Count.EqualTo(1));
        Assert.That(text.Segments[0].StyleId, Is.EqualTo("Success"));
    }
}
