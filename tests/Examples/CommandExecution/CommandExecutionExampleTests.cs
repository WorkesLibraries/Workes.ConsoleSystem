using System.Text;
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Entries;

namespace Workes.ConsoleSystem.Tests.Examples.CommandExecution;

[Category("Example")]
public sealed class CommandExecutionExampleTests
{
    [Test]
    public void PlayerGiveCommand_WritesReadableExample()
    {
        var console = new ConsoleManager();

        console.RegisterCommand(new CommandBuilder("player.give")
            .Argument<GiveState>(x => x.Player, "player")
            .Argument<GiveState>(x => x.Item, "item")
            .Argument<GiveState>(x => x.Amount, "amount")
            .Constraint<GiveState>("positive-amount", "Amount must be greater than zero.", state => state.Amount > 0)
            .Execute<GiveState>((ctx, state) => CommandResult.Success(CommandOutput.Inline(
                $"Gave {state.Player} {state.Amount} {state.Item}.",
                defaultStyle: "Success")))
            .Build());

        bool success = console.TryExecuteCommand("player.give @me wood 7", out CommandResult successResult);
        bool failure = console.TryExecuteCommand("player.give @me wood 0", out CommandResult failureResult);

        string output = RenderHistory(console);
        string path = WriteExample("CommandExecution", "PlayerGiveCommandExample.txt", output);

        Assert.That(success, Is.True);
        Assert.That(successResult.IsSuccess, Is.True);
        Assert.That(failure, Is.False);
        Assert.That(failureResult.Failure!.Code, Is.EqualTo(ConsoleFailureCodes.CommandConstraintRejected));
        Assert.That(output, Does.Contain("Input: player.give @me wood 7"));
        Assert.That(output, Does.Contain("Output: Gave @me 7 wood."));
        Assert.That(output, Does.Contain("Failure: Amount must be greater than zero."));
        Assert.That(File.Exists(path), Is.True);
    }

    private static string RenderHistory(ConsoleManager console)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Command Execution Example");
        builder.AppendLine();

        foreach (IConsoleEntry entry in console.History.Entries)
        {
            switch (entry)
            {
                case CommandInputEntry input:
                    builder.AppendLine($"Input: {input.Input}");
                    break;
                case CommandOutputEntry output:
                    builder.AppendLine($"Output: {output.PlainText}");
                    break;
                case CommandFailureEntry failure:
                    builder.AppendLine($"Failure: {failure.Message}");
                    break;
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

    private sealed record GiveState(string Player, string Item, int Amount);
}
