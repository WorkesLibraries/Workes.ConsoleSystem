using System;
using System.Collections.Generic;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Provides context for command value autocomplete providers.
/// </summary>
public sealed class CommandAutocompleteContext
{
    internal CommandAutocompleteContext(
        string input,
        int cursorIndex,
        string partialValue,
        CommandDefinition command,
        CommandMemberDefinition member,
        IReadOnlyList<string> tokensBeforeCursor)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        PartialValue = partialValue ?? throw new ArgumentNullException(nameof(partialValue));
        Command = command ?? throw new ArgumentNullException(nameof(command));
        Member = member ?? throw new ArgumentNullException(nameof(member));
        TokensBeforeCursor = tokensBeforeCursor ?? throw new ArgumentNullException(nameof(tokensBeforeCursor));
        CursorIndex = cursorIndex;
    }

    /// <summary>
    /// Gets the full command input.
    /// </summary>
    public string Input { get; }

    /// <summary>
    /// Gets the cursor index used for autocomplete.
    /// </summary>
    public int CursorIndex { get; }

    /// <summary>
    /// Gets the partial value being completed.
    /// </summary>
    public string PartialValue { get; }

    /// <summary>
    /// Gets the matched command definition.
    /// </summary>
    public CommandDefinition Command { get; }

    /// <summary>
    /// Gets the active argument or option member.
    /// </summary>
    public CommandMemberDefinition Member { get; }

    /// <summary>
    /// Gets raw token text before the cursor.
    /// </summary>
    public IReadOnlyList<string> TokensBeforeCursor { get; }
}
