using System;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Describes why a command input failed to parse.
/// </summary>
public sealed class CommandParseError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandParseError"/> class.
    /// </summary>
    /// <param name="code">The stable parse error code.</param>
    /// <param name="message">The human-readable parse error message.</param>
    public CommandParseError(CommandParseErrorCode code, string message)
    {
        Code = code;
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }

    /// <summary>
    /// Gets the stable parse error code.
    /// </summary>
    public CommandParseErrorCode Code { get; }

    /// <summary>
    /// Gets the human-readable parse error message.
    /// </summary>
    public string Message { get; }
}
