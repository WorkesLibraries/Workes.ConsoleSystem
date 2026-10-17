using System;
using Workes.ConsoleSystem.Core;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Represents the result of parsing command input.
/// </summary>
public sealed class CommandParseResult
{
    private CommandParseResult(string input, BoundCommand? command, ConsoleFailure? failure, int? sourceOffset, int? sourceLength)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        Command = command;
        Failure = failure;
        SourceOffset = sourceOffset;
        SourceLength = sourceLength;
    }

    /// <summary>
    /// Gets the original input that was parsed.
    /// </summary>
    public string Input { get; }

    /// <summary>
    /// Gets a value indicating whether parsing succeeded.
    /// </summary>
    public bool IsSuccess => Command is not null;

    /// <summary>
    /// Gets the bound command when parsing succeeds.
    /// </summary>
    public BoundCommand? Command { get; }

    /// <summary>
    /// Gets the structured failure when parsing fails.
    /// </summary>
    public ConsoleFailure? Failure { get; }

    /// <summary>
    /// Gets the zero-based offset in <see cref="Input"/> of the input associated with a parse failure.
    /// </summary>
    /// <remarks>Both source location properties are null when parsing succeeds or the failure has no specific input location.</remarks>
    public int? SourceOffset { get; }

    /// <summary>
    /// Gets the length in <see cref="Input"/> of the input associated with a parse failure.
    /// </summary>
    public int? SourceLength { get; }

    internal static CommandParseResult Succeeded(string input, BoundCommand command)
    {
        return new CommandParseResult(input, command ?? throw new ArgumentNullException(nameof(command)), null, null, null);
    }

    internal static CommandParseResult Failed(string input, ConsoleFailure failure, int? sourceOffset = null, int? sourceLength = null)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        if (failure is null)
        {
            throw new ArgumentNullException(nameof(failure));
        }

        if (sourceOffset.HasValue != sourceLength.HasValue)
        {
            throw new ArgumentException("Source offset and length must be provided together.");
        }

        if (sourceOffset.HasValue && (sourceOffset.Value < 0 || sourceOffset.Value > input.Length))
        {
            throw new ArgumentOutOfRangeException(nameof(sourceOffset));
        }

        if (sourceLength.HasValue && (sourceLength.Value < 0 || sourceLength.Value > input.Length - sourceOffset!.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(sourceLength));
        }

        return new CommandParseResult(input, null, failure, sourceOffset, sourceLength);
    }
}
