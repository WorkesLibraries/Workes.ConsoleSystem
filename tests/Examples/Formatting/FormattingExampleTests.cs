using System.Text;
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Presentation;
using ConsoleColor = Workes.ConsoleSystem.Presentation.ConsoleColor;

namespace Workes.ConsoleSystem.Tests.Examples.Formatting;

[Category("Example")]
public sealed class FormattingExampleTests
{
    [Test]
    public void UnityGodotAndPlainFormatting_WritesReadableExample()
    {
        var theme = new ConsoleTheme(new Dictionary<string, ConsoleStyle>
        {
            ["Success"] = ConsoleStyle.Standard(
                foregroundColor: ConsoleColor.FromHex("#4ade80"),
                bold: true)
        });

        string markup = "<style=Success>Saved game.</style>";
        var unityConsole = new ConsoleManager(new ConsoleManagerOptions
        {
            Formatting = ConsoleFormattingOptions.UnityRichText(theme)
        });
        var godotConsole = new ConsoleManager(new ConsoleManagerOptions
        {
            Formatting = ConsoleFormattingOptions.GodotBbCode(theme)
        });
        var plainConsole = new ConsoleManager();

        string unity = unityConsole.Format(unityConsole.CreateText(markup));
        string godot = godotConsole.Format(godotConsole.CreateText(markup));
        string plain = plainConsole.Format(plainConsole.CreateText(markup));

        var builder = new StringBuilder();
        builder.AppendLine("Formatting Example");
        builder.AppendLine();
        builder.AppendLine($"Unity: {unity}");
        builder.AppendLine($"Godot: {godot}");
        builder.AppendLine($"Plain: {plain}");

        string path = WriteExample("Formatting", "FormattingExample.txt", builder.ToString());

        Assert.That(unity, Is.EqualTo("<color=#4ADE80><b>Saved game.</b></color>"));
        Assert.That(godot, Is.EqualTo("[color=#4ADE80][b]Saved game.[/b][/color]"));
        Assert.That(plain, Is.EqualTo("<style=Success>Saved game.</style>"));
        Assert.That(File.Exists(path), Is.True);
    }

    private static string WriteExample(string area, string fileName, string content)
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "ExampleOutputs", area);
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, fileName);
        File.WriteAllText(path, content);
        return path;
    }
}
