using System;
using System.Collections.Generic;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Represents autocomplete candidates for command input at a cursor position.
/// </summary>
public sealed class CommandAutocompleteResult
{
    internal CommandAutocompleteResult(
        string input,
        int cursorIndex,
        int replacementStart,
        int replacementLength,
        IReadOnlyList<CommandAutocompleteCandidate> candidates)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        CursorIndex = cursorIndex;
        ReplacementStart = replacementStart;
        ReplacementLength = replacementLength;
        Candidates = candidates ?? throw new ArgumentNullException(nameof(candidates));
    }

    /// <summary>
    /// Gets the original input.
    /// </summary>
    public string Input { get; }

    /// <summary>
    /// Gets the cursor index used for autocomplete.
    /// </summary>
    public int CursorIndex { get; }

    /// <summary>
    /// Gets the start index of the text that should be replaced.
    /// </summary>
    public int ReplacementStart { get; }

    /// <summary>
    /// Gets the number of characters that should be replaced.
    /// </summary>
    public int ReplacementLength { get; }

    /// <summary>
    /// Gets autocomplete candidates in schema order.
    /// </summary>
    public IReadOnlyList<CommandAutocompleteCandidate> Candidates { get; }

    /// <summary>
    /// Applies a candidate to the original input using this result's replacement range.
    /// </summary>
    /// <param name="candidate">The candidate to apply.</param>
    /// <returns>The completed input.</returns>
    public string Apply(CommandAutocompleteCandidate candidate)
    {
        if (candidate is null)
        {
            throw new ArgumentNullException(nameof(candidate));
        }

        return Input.Remove(ReplacementStart, ReplacementLength).Insert(ReplacementStart, candidate.Text);
    }

    /// <summary>
    /// Applies a candidate by index to the original input using this result's replacement range.
    /// </summary>
    /// <param name="candidateIndex">The candidate index.</param>
    /// <returns>The completed input.</returns>
    public string Apply(int candidateIndex)
    {
        if (candidateIndex < 0 || candidateIndex >= Candidates.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(candidateIndex), "Candidate index must refer to an available autocomplete candidate.");
        }

        return Apply(Candidates[candidateIndex]);
    }
}
