using System;
using System.Collections.Generic;
using System.Text;

namespace Workes.ConsoleSystem.Presentation;

/// <summary>
/// Represents engine-neutral console text with optional semantic segments.
/// </summary>
public sealed class ConsoleText
{
    private readonly IReadOnlyList<ConsoleTextSegment> _segments;

    internal ConsoleText(string? defaultSemanticId, IEnumerable<ConsoleTextSegment> segments)
    {
        if (defaultSemanticId is not null && string.IsNullOrWhiteSpace(defaultSemanticId))
        {
            throw new ArgumentException("Default semantic identifiers cannot be empty.", nameof(defaultSemanticId));
        }

        if (segments is null)
        {
            throw new ArgumentNullException(nameof(segments));
        }

        var segmentList = new List<ConsoleTextSegment>();
        var plainText = new StringBuilder();
        foreach (ConsoleTextSegment segment in segments)
        {
            if (segment is null)
            {
                throw new ArgumentException("Console text segments cannot contain null values.", nameof(segments));
            }

            segmentList.Add(segment);
            plainText.Append(segment.Text);
        }

        DefaultSemanticId = defaultSemanticId;
        _segments = segmentList.AsReadOnly();
        PlainText = plainText.ToString();
    }

    /// <summary>
    /// Gets the optional semantic identifier used by unclassified segments.
    /// </summary>
    public string? DefaultSemanticId { get; }

    /// <summary>
    /// Gets the console text segments.
    /// </summary>
    public IReadOnlyList<ConsoleTextSegment> Segments => _segments;

    /// <summary>
    /// Gets the derived plain text.
    /// </summary>
    public string PlainText { get; }

    /// <summary>
    /// Creates literal plain console text.
    /// </summary>
    public static ConsoleText Plain(string text, string? defaultSemanticId = null)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        return new ConsoleText(defaultSemanticId, new[] { new ConsoleTextSegment(text) });
    }

    /// <summary>
    /// Starts building console text.
    /// </summary>
    public static ConsoleTextBuilder Build(string? defaultSemanticId = null)
    {
        return new ConsoleTextBuilder(defaultSemanticId);
    }

    /// <summary>
    /// Resolves a segment semantic identifier against this text's default semantic identifier.
    /// </summary>
    public string? ResolveSemanticId(ConsoleTextSegment segment)
    {
        if (segment is null)
        {
            throw new ArgumentNullException(nameof(segment));
        }

        return segment.ResolveSemanticId(DefaultSemanticId);
    }
}
