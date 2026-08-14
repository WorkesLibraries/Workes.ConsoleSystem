using Workes.ConsoleSystem.Presentation;
using ConsoleColor = Workes.ConsoleSystem.Presentation.ConsoleColor;

namespace Workes.ConsoleSystem.Tests.Presentation;

public sealed class ConsoleColorTests
{
    [Test]
    public void FromRgb_CreatesOpaqueColor()
    {
        var color = ConsoleColor.FromRgb(1, 2, 3);

        Assert.That(color.Red, Is.EqualTo(1));
        Assert.That(color.Green, Is.EqualTo(2));
        Assert.That(color.Blue, Is.EqualTo(3));
        Assert.That(color.Alpha, Is.EqualTo(255));
        Assert.That(color.ToString(), Is.EqualTo("#010203"));
    }

    [Test]
    public void FromRgba_CreatesTransparentColor()
    {
        var color = ConsoleColor.FromRgba(1, 2, 3, 4);

        Assert.That(color.ToString(), Is.EqualTo("#01020304"));
    }

    [TestCase("#abc", 170, 187, 204, 255)]
    [TestCase("#AABBCC", 170, 187, 204, 255)]
    [TestCase("#AABBCCDD", 170, 187, 204, 221)]
    public void FromHex_ParsesSupportedFormats(string hex, int red, int green, int blue, int alpha)
    {
        var color = ConsoleColor.FromHex(hex);

        Assert.That(color.Red, Is.EqualTo(red));
        Assert.That(color.Green, Is.EqualTo(green));
        Assert.That(color.Blue, Is.EqualTo(blue));
        Assert.That(color.Alpha, Is.EqualTo(alpha));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("AABBCC")]
    [TestCase("#12")]
    [TestCase("#GGGGGG")]
    public void FromHex_InvalidValueThrows(string? hex)
    {
        if (hex is null)
        {
            Assert.Throws<ArgumentNullException>(() => ConsoleColor.FromHex(hex!));
        }
        else
        {
            Assert.Throws<FormatException>(() => ConsoleColor.FromHex(hex));
        }
    }
}
