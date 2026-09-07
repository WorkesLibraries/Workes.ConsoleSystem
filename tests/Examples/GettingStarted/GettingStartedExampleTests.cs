using System.Text;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Entries;

namespace Workes.ConsoleSystem.Tests.Examples.GettingStarted;

[Category("Example")]
public sealed class GettingStartedExampleTests
{
    [Test]
    public void LoggingAndHistory_WritesReadableExample()
    {
        var console = new ConsoleManager();

        console.LogInformation("Console ready.");
        console.LogWarning("Low memory warning.");

        string output = RenderHistory(console);
        string path = WriteExample("GettingStarted", "LoggingAndHistoryExample.txt", output);

        Assert.That(output, Does.Contain("[Information] Console ready."));
        Assert.That(output, Does.Contain("[Warning] Low memory warning."));
        Assert.That(File.Exists(path), Is.True);
    }

    private static string RenderHistory(ConsoleManager console)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Getting Started Example");
        builder.AppendLine();

        foreach (IConsoleEntry entry in console.History.Entries)
        {
            if (entry is LogEntry log)
            {
                builder.AppendLine($"[{log.Level}] {log.Message}");
            }
        }

        return builder.ToString();
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
