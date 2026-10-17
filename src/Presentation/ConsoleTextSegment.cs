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
    public ConsoleTextSegment(string text, string? semanticId = null, object? data = null)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        if (semanticId is not null && string.IsNullOrWhiteSpace(semanticId))
        {
            throw new ArgumentException("Semantic identifiers cannot be empty.", nameof(semanticId));
        }

        Text = text;
        SemanticId = semanticId;
        Data = data;
    }

    /// <summary>
    /// Gets the plain text for the segment.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the optional semantic identifier.
    /// </summary>
    public string? SemanticId { get; }

    /// <summary>
    /// Gets the optional structured value represented by the segment.
    /// </summary>
    public object? Data { get; }

    /// <summary>
    /// Resolves the segment semantic identifier against a default semantic identifier.
    /// </summary>
    public string? ResolveSemanticId(string? defaultSemanticId)
    {
        return SemanticId ?? defaultSemanticId;
    }
}
