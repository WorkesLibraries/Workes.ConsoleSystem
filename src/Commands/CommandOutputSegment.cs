using System;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Represents one semantic segment of command output.
/// </summary>
public sealed class CommandOutputSegment
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandOutputSegment"/> class.
    /// </summary>
    /// <param name="text">The plain text for the segment.</param>
    /// <param name="styleId">The optional semantic style identifier.</param>
    /// <param name="data">The optional structured value represented by the segment.</param>
    public CommandOutputSegment(string text, string? styleId = null, object? data = null)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        if (styleId is not null && string.IsNullOrWhiteSpace(styleId))
        {
            throw new ArgumentException("Style identifiers cannot be empty.", nameof(styleId));
        }

        Text = text;
        StyleId = styleId;
        Data = data;
    }

    /// <summary>
    /// Gets the plain text for the segment.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the optional semantic style identifier.
    /// </summary>
    public string? StyleId { get; }

    /// <summary>
    /// Gets the optional structured value represented by the segment.
    /// </summary>
    public object? Data { get; }

    /// <summary>
    /// Resolves the segment style against an output default style.
    /// </summary>
    /// <param name="defaultStyleId">The output default style identifier.</param>
    /// <returns>The segment style identifier, or the default style identifier when the segment has no explicit style.</returns>
    public string? ResolveStyleId(string? defaultStyleId)
    {
        return StyleId ?? defaultStyleId;
    }
}
