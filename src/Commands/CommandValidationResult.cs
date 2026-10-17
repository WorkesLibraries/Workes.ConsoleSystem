using System;
using Workes.ConsoleSystem.Core;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Represents the result of validating a bound command before execution.
/// </summary>
public sealed class CommandValidationResult
{
    private CommandValidationResult(BoundCommand command, ConsoleFailure? failure)
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
        Failure = failure;
    }

    /// <summary>
    /// Gets a value indicating whether validation succeeded.
    /// </summary>
    public bool IsSuccess => Failure is null;

    /// <summary>
    /// Gets the bound command that was validated.
    /// </summary>
    public BoundCommand Command { get; }

    /// <summary>
    /// Gets the structured failure when validation fails.
    /// </summary>
    public ConsoleFailure? Failure { get; }

    internal static CommandValidationResult Succeeded(BoundCommand command)
    {
        return new CommandValidationResult(command, null);
    }

    internal static CommandValidationResult Failed(BoundCommand command, ConsoleFailure failure)
    {
        return new CommandValidationResult(
            command,
            failure ?? throw new ArgumentNullException(nameof(failure)));
    }
}
