using System;
using System.Collections.Generic;
using System.Text;

namespace Workes.ConsoleSystem.Presentation;

/// <summary>
/// Represents engine-neutral console text with semantic style information.
/// </summary>
public sealed class ConsoleText
{
    private readonly IReadOnlyList<ConsoleTextSegment> _segments;

    internal ConsoleText(string? defaultStyleId, IEnumerable<ConsoleTextSegment> segments)
    {
        if (defaultStyleId is not null && string.IsNullOrWhiteSpace(defaultStyleId))
        {
            throw new ArgumentException("Default style identifiers cannot be empty.", nameof(defaultStyleId));
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

        DefaultStyleId = defaultStyleId;
        _segments = segmentList.AsReadOnly();
        PlainText = plainText.ToString();
    }

    /// <summary>
    /// Gets the optional semantic style identifier used by unstylized segments.
    /// </summary>
    public string? DefaultStyleId { get; }

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
    public static ConsoleText Plain(string text, string? defaultStyle = null)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        return new ConsoleText(defaultStyle, new[] { new ConsoleTextSegment(text) });
    }

    /// <summary>
    /// Escapes text for use inside console markup.
    /// </summary>
    public static string EscapeMarkup(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    /// <summary>
    /// Starts building console text.
    /// </summary>
    public static ConsoleTextBuilder Build(string? defaultStyle = null)
    {
        return new ConsoleTextBuilder(defaultStyle);
    }

    internal static ConsoleText Markup(string markup, string? defaultStyle, ConsoleMarkupProfile profile)
    {
        return ConsoleTextMarkupParser.Parse(markup, defaultStyle, profile);
    }

    /// <summary>
    /// Resolves a segment style identifier against this text's default style.
    /// </summary>
    public string? ResolveStyleId(ConsoleTextSegment segment)
    {
        if (segment is null)
        {
            throw new ArgumentNullException(nameof(segment));
        }

        return segment.ResolveStyleId(DefaultStyleId);
    }
}
