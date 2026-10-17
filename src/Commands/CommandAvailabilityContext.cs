using System;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Provides command input information to an availability check.
/// </summary>
public sealed class CommandAvailabilityContext
{
    internal CommandAvailabilityContext(string input, int? cursorIndex, CommandDefinition definition, BoundCommand? command)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        CursorIndex = cursorIndex;
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Command = command;
    }

    /// <summary>Gets the input being executed or completed.</summary>
    public string Input { get; }

    /// <summary>Gets the cursor position for autocomplete, or null during execution.</summary>
    public int? CursorIndex { get; }

    /// <summary>Gets the command definition being checked.</summary>
    public CommandDefinition Definition { get; }

    /// <summary>Gets the parsed command during execution, or null during autocomplete.</summary>
    public BoundCommand? Command { get; }
}
