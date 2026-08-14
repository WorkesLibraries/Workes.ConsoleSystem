using System;

namespace Workes.ConsoleSystem.Presentation;

/// <summary>
/// Represents one semantic segment of console text.
/// </summary>
public sealed class ConsoleTextSegment
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleTextSegment"/> class.
    /// </summary>
    public ConsoleTextSegment(string text, string? styleId = null, object? data = null, ConsoleStyle? inlineStyle = null)
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
        InlineStyle = inlineStyle;
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
    /// Gets direct inline style overrides.
    /// </summary>
    public ConsoleStyle? InlineStyle { get; }

    /// <summary>
    /// Resolves the segment style identifier against a default style.
    /// </summary>
    public string? ResolveStyleId(string? defaultStyleId)
    {
        return StyleId ?? defaultStyleId;
    }
}
