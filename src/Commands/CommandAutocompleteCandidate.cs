using System;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Represents one autocomplete candidate.
/// </summary>
public sealed class CommandAutocompleteCandidate
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandAutocompleteCandidate"/> class.
    /// </summary>
    public CommandAutocompleteCandidate(
        string text,
        CommandAutocompleteCandidateKind kind,
        string? displayText = null,
        string? description = null)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        if (displayText is not null && string.IsNullOrWhiteSpace(displayText))
        {
            throw new ArgumentException("Display text cannot be blank.", nameof(displayText));
        }

        Text = text;
        Kind = kind;
        DisplayText = displayText ?? text;
        Description = description;
    }

    /// <summary>
    /// Gets the exact text that should replace the result replacement range.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the UI-facing display text.
    /// </summary>
    public string DisplayText { get; }

    /// <summary>
    /// Gets the candidate kind.
    /// </summary>
    public CommandAutocompleteCandidateKind Kind { get; }

    /// <summary>
    /// Gets the optional candidate description.
    /// </summary>
    public string? Description { get; }
}
