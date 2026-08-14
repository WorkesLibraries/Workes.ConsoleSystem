using System;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Represents the result of parsing command input.
/// </summary>
public sealed class CommandParseResult
{
    private CommandParseResult(string input, BoundCommand? command, CommandParseError? error)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        Command = command;
        Error = error;
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
    /// Gets the parse error when parsing fails.
    /// </summary>
    public CommandParseError? Error { get; }

    internal static CommandParseResult Succeeded(string input, BoundCommand command)
    {
        return new CommandParseResult(input, command ?? throw new ArgumentNullException(nameof(command)), null);
    }

    internal static CommandParseResult Failed(string input, CommandParseErrorCode code, string message)
    {
        return new CommandParseResult(input, null, new CommandParseError(code, message));
    }
}
