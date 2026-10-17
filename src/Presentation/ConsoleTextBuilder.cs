using System;
using System.Collections.Generic;

namespace Workes.ConsoleSystem.Presentation;

/// <summary>
/// Builds semantic console text.
/// </summary>
public sealed class ConsoleTextBuilder
{
    private readonly string? _defaultSemanticId;
    private readonly List<ConsoleTextSegment> _segments = new List<ConsoleTextSegment>();

    internal ConsoleTextBuilder(string? defaultSemanticId)
    {
        if (defaultSemanticId is not null && string.IsNullOrWhiteSpace(defaultSemanticId))
        {
            throw new ArgumentException("Default semantic identifiers cannot be empty.", nameof(defaultSemanticId));
        }

        _defaultSemanticId = defaultSemanticId;
    }

    /// <summary>
    /// Appends plain text without an explicit semantic identifier.
    /// </summary>
    public ConsoleTextBuilder Text(string text)
    {
        _segments.Add(new ConsoleTextSegment(text));
        return this;
    }

    /// <summary>
    /// Appends a semantic value segment.
    /// </summary>
    public ConsoleTextBuilder Value(string text, string? semanticId = null, object? data = null)
    {
        _segments.Add(new ConsoleTextSegment(text, semanticId, data));
        return this;
    }

    /// <summary>
    /// Creates immutable console text.
    /// </summary>
    public ConsoleText Build()
    {
        return new ConsoleText(_defaultSemanticId, _segments);
    }
}
