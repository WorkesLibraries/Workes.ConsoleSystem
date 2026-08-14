using System;
using Workes.ConsoleSystem.Core;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Represents the result of parsing command input.
/// </summary>
public sealed class CommandParseResult
{
    private CommandParseResult(string input, BoundCommand? command, ConsoleFailure? failure)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        Command = command;
        Failure = failure;
    }

    /// <summary>
    /// Gets the original input that was parsed.
    /// </summary>
    public string Input { get; }

    /// <summary>
    /// Gets a value indicating whether parsing succeeded.
    /// </summary>
    public bool Success => Command is not null;

    /// <summary>
    /// Gets the bound command when parsing succeeds.
    /// </summary>
    public BoundCommand? Command { get; }

    /// <summary>
    /// Gets the structured failure when parsing fails.
    /// </summary>
    public ConsoleFailure? Failure { get; }

    internal static CommandParseResult Succeeded(string input, BoundCommand command)
    {
        return new CommandParseResult(input, command ?? throw new ArgumentNullException(nameof(command)), null);
    }

    internal static CommandParseResult Failed(string input, ConsoleFailure failure)
    {
        return new CommandParseResult(input, null, failure ?? throw new ArgumentNullException(nameof(failure)));
    }
}
