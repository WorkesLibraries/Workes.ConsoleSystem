using System.Reflection;
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Core;

namespace Workes.ConsoleSystem.Tests.Commands;

public sealed class CommandResultTests
{
    [Test]
    public void Success_CreatesSuccessfulEmptyResult()
    {
        var result = CommandResult.Success();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Failure, Is.Null);
        Assert.That(result.Outputs, Is.Empty);
    }

    [Test]
    public void PublicConstruction_UsesFactoriesOnly()
    {
        Assert.That(typeof(CommandResult).GetConstructors(BindingFlags.Public | BindingFlags.Instance), Is.Empty);
        Assert.That(typeof(CommandResult).GetMethod(nameof(CommandResult.Success), BindingFlags.Public | BindingFlags.Static), Is.Not.Null);
        Assert.That(typeof(CommandResult).GetMethod(nameof(CommandResult.Failed), BindingFlags.Public | BindingFlags.Static), Is.Not.Null);
    }

    [Test]
    public void Success_PreservesOutputsInOrder()
    {
        var first = CommandOutput.Inline("one");
        var second = CommandOutput.Block("two");

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
        var output = CommandOutput.Inline("Could not run command.");

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
