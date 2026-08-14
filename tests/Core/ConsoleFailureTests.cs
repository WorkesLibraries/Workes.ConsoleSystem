using Workes.ConsoleSystem.Core;

namespace Workes.ConsoleSystem.Tests.Core;

public sealed class ConsoleFailureTests
{
    [Test]
    public void Constructor_StoresFailureDetails()
    {
        var cause = ConsoleFailure.Create(
            ConsoleFailureKind.CommandParsing,
            ConsoleFailureCodes.CommandUnknown,
            "Unknown command.");

        var failure = new ConsoleFailure(
            ConsoleFailureKind.CommandExecution,
            ConsoleFailureCodes.CommandExecutionRejected,
            "Command execution failed.",
            component: "CommandExecutor",
            source: "noclip",
            cause: cause);

        Assert.That(failure.Kind, Is.EqualTo(ConsoleFailureKind.CommandExecution));
        Assert.That(failure.Code, Is.EqualTo(ConsoleFailureCodes.CommandExecutionRejected));
        Assert.That(failure.Message, Is.EqualTo("Command execution failed."));
        Assert.That(failure.Component, Is.EqualTo("CommandExecutor"));
        Assert.That(failure.Source, Is.EqualTo("noclip"));
        Assert.That(failure.Cause, Is.SameAs(cause));
    }

    [TestCase(null, "message")]
    [TestCase("", "message")]
    [TestCase("   ", "message")]
    [TestCase("code", null)]
    [TestCase("code", "")]
    [TestCase("code", "   ")]
    public void Constructor_InvalidCodeOrMessageThrows(string? code, string? message)
    {
        Assert.Throws<ArgumentException>(() => new ConsoleFailure(
            ConsoleFailureKind.Unknown,
            code!,
            message!));
    }

    [Test]
    public void Create_CreatesFailure()
    {
        ConsoleFailure failure = ConsoleFailure.Create(
            ConsoleFailureKind.Validation,
            ConsoleFailureCodes.ValidationRejected,
            "Rejected.");

        Assert.That(failure.Kind, Is.EqualTo(ConsoleFailureKind.Validation));
        Assert.That(failure.Code, Is.EqualTo(ConsoleFailureCodes.ValidationRejected));
        Assert.That(failure.Message, Is.EqualTo("Rejected."));
    }

    [Test]
    public void Wrap_PreservesCauseAndAddsCauseMessage()
    {
        ConsoleFailure cause = ConsoleFailure.Create(
            ConsoleFailureKind.CommandParsing,
            ConsoleFailureCodes.CommandValueInvalid,
            "Invalid value.");

        ConsoleFailure wrapped = ConsoleFailure.Wrap(
            ConsoleFailureKind.CommandExecution,
            ConsoleFailureCodes.CommandExecutionRejected,
            "Execution rejected",
            cause,
            component: "CommandExecutor",
            source: "give");

        Assert.That(wrapped.Cause, Is.SameAs(cause));
        Assert.That(wrapped.Message, Is.EqualTo("Execution rejected: Invalid value."));
        Assert.That(wrapped.ToString(), Is.EqualTo("Execution rejected: Invalid value. Cause: Invalid value."));
    }

    [Test]
    public void FromException_WhenConsoleSystemException_ReturnsContainedFailure()
    {
        ConsoleFailure failure = ConsoleFailure.Create(
            ConsoleFailureKind.CommandExecution,
            ConsoleFailureCodes.CommandExecutionRejected,
            "Execution failed.");
        var exception = new ConsoleOperationException(failure);

        ConsoleFailure converted = ConsoleFailure.FromException(exception);

        Assert.That(converted, Is.SameAs(failure));
    }

    [Test]
    public void FromException_WhenStandardException_CreatesExtensionFailure()
    {
        ConsoleFailure failure = ConsoleFailure.FromException(new InvalidOperationException("Extension failed."));

        Assert.That(failure.Kind, Is.EqualTo(ConsoleFailureKind.Extension));
        Assert.That(failure.Code, Is.EqualTo(ConsoleFailureCodes.ExtensionRejected));
        Assert.That(failure.Message, Is.EqualTo("Extension failed."));
    }

    [Test]
    public void Equality_UsesAllFailureFields()
    {
        ConsoleFailure first = ConsoleFailure.Create(
            ConsoleFailureKind.CommandParsing,
            ConsoleFailureCodes.CommandUnknown,
            "Unknown command.",
            component: "CommandParser",
            source: "missing");
        ConsoleFailure second = ConsoleFailure.Create(
            ConsoleFailureKind.CommandParsing,
            ConsoleFailureCodes.CommandUnknown,
            "Unknown command.",
            component: "CommandParser",
            source: "missing");

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
    }

    [Test]
    public void ConsoleSystemException_ExposesFailure()
    {
        ConsoleFailure failure = ConsoleFailure.Create(
            ConsoleFailureKind.CommandExecution,
            ConsoleFailureCodes.CommandExecutionRejected,
            "Execution failed.");

        var exception = new ConsoleSystemException(failure);

        Assert.That(exception.Failure, Is.SameAs(failure));
        Assert.That(exception.Message, Is.EqualTo("Execution failed."));
    }

    [Test]
    public void ConsoleOperationException_ExposesFailure()
    {
        ConsoleFailure failure = ConsoleFailure.Create(
            ConsoleFailureKind.CommandExecution,
            ConsoleFailureCodes.CommandExecutionRejected,
            "Execution failed.");

        var exception = new ConsoleOperationException(failure);

        Assert.That(exception.Failure, Is.SameAs(failure));
    }
}
