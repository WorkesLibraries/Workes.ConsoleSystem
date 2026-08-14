using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Core;

namespace Workes.ConsoleSystem.Tests.Commands;

public sealed class CommandResultTests
{
    [Test]
    public void Constructor_CreatesSuccessfulEmptyResult()
    {
        var result = new CommandResult();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Failure, Is.Null);
        Assert.That(result.Outputs, Is.Empty);
    }

    [Test]
    public void Success_PreservesOutputsInOrder()
    {
        var first = CommandOutput.InlineText("one");
        var second = CommandOutput.BlockText("two");

        var result = CommandResult.Success(first, second);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Failure, Is.Null);
        Assert.That(result.Outputs, Is.EqualTo(new[] { first, second }));
    }

    [Test]
    public void Failure_PreservesFailureAndOptionalOutputs()
    {
        var failure = ConsoleFailure.Create(
            ConsoleFailureKind.CommandExecution,
            ConsoleFailureCodes.CommandExecutionRejected,
            "Command failed.");
        var output = CommandOutput.InlineText("Could not run command.", "Error");

        var result = CommandResult.Failed(failure, output);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure, Is.SameAs(failure));
        Assert.That(result.Outputs, Is.EqualTo(new[] { output }));
    }

    [Test]
    public void Failure_NullFailureThrows()
    {
        Assert.Throws<ArgumentNullException>(() => CommandResult.Failed(null!));
    }

    [Test]
    public void Success_NullOutputThrows()
    {
        Assert.Throws<ArgumentNullException>(() => CommandResult.Success(null!));
        Assert.Throws<ArgumentException>(() => CommandResult.Success(new CommandOutput[] { null! }));
    }
}
