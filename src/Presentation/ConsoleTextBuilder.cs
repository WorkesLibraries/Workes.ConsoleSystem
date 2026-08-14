using System;
using System.Collections.Generic;

namespace Workes.ConsoleSystem.Presentation;

/// <summary>
/// Builds semantic console text.
/// </summary>
public sealed class ConsoleTextBuilder
{
    private readonly string? _defaultStyle;
    private readonly List<ConsoleTextSegment> _segments = new List<ConsoleTextSegment>();

    internal ConsoleTextBuilder(string? defaultStyle)
    {
        if (defaultStyle is not null && string.IsNullOrWhiteSpace(defaultStyle))
        {
            throw new ArgumentException("Default style identifiers cannot be empty.", nameof(defaultStyle));
        }

        _defaultStyle = defaultStyle;
    }

    /// <summary>
    /// Appends plain text without an explicit segment style.
    /// </summary>
    public ConsoleTextBuilder Text(string text)
    {
        _segments.Add(new ConsoleTextSegment(text));
        return this;
    }

    /// <summary>
    /// Appends a semantic value segment.
    /// </summary>
    public ConsoleTextBuilder Value(string text, string? style = null, object? data = null)
    {
        _segments.Add(new ConsoleTextSegment(text, style, data));
        return this;
    }

    /// <summary>
    /// Creates immutable console text.
    /// </summary>
    public ConsoleText Build()
    {
        return new ConsoleText(_defaultStyle, _segments);
    }
}
