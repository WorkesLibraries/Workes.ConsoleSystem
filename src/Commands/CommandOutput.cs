using System;
using System.Collections.Generic;
using System.Text;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Represents semantic text output produced by a command.
/// </summary>
public sealed class CommandOutput
{
    private readonly IReadOnlyList<CommandOutputSegment> _segments;

    internal CommandOutput(CommandOutputKind kind, string? defaultStyleId, IEnumerable<CommandOutputSegment> segments)
    {
        if (defaultStyleId is not null && string.IsNullOrWhiteSpace(defaultStyleId))
        {
            throw new ArgumentException("Default style identifiers cannot be empty.", nameof(defaultStyleId));
        }

        if (segments is null)
        {
            throw new ArgumentNullException(nameof(segments));
        }

        var segmentList = new List<CommandOutputSegment>();
        var plainText = new StringBuilder();
        foreach (CommandOutputSegment segment in segments)
        {
            if (segment is null)
            {
                throw new ArgumentException("Command output segments cannot contain null values.", nameof(segments));
            }

            segmentList.Add(segment);
            plainText.Append(segment.Text);
        }

        Kind = kind;
        DefaultStyleId = defaultStyleId;
        _segments = segmentList.AsReadOnly();
        PlainText = plainText.ToString();
    }

    /// <summary>
    /// Gets the structural output kind.
    /// </summary>
    public CommandOutputKind Kind { get; }

    /// <summary>
    /// Gets the optional semantic style identifier used by unstylized segments.
    /// </summary>
    public string? DefaultStyleId { get; }

    /// <summary>
    /// Gets the semantic output segments.
    /// </summary>
    public IReadOnlyList<CommandOutputSegment> Segments => _segments;

    /// <summary>
    /// Gets the derived plain text for the output.
    /// </summary>
    public string PlainText { get; }

    /// <summary>
    /// Starts building inline command output.
    /// </summary>
    /// <param name="defaultStyle">The optional default semantic style identifier.</param>
    /// <returns>The output builder.</returns>
    public static CommandOutputBuilder Inline(string? defaultStyle = null)
    {
        return new CommandOutputBuilder(CommandOutputKind.Inline, defaultStyle);
    }

    /// <summary>
    /// Starts building block command output.
    /// </summary>
    /// <param name="defaultStyle">The optional default semantic style identifier.</param>
    /// <returns>The output builder.</returns>
    public static CommandOutputBuilder Block(string? defaultStyle = null)
    {
        return new CommandOutputBuilder(CommandOutputKind.Block, defaultStyle);
    }

    /// <summary>
    /// Creates simple inline command output from plain text.
    /// </summary>
    /// <param name="text">The output text.</param>
    /// <param name="defaultStyle">The optional default semantic style identifier.</param>
    /// <returns>The created command output.</returns>
    public static CommandOutput InlineText(string text, string? defaultStyle = null)
    {
        return Inline(defaultStyle).Text(text).Build();
    }

    /// <summary>
    /// Creates simple block command output from plain text.
    /// </summary>
    /// <param name="text">The output text.</param>
    /// <param name="defaultStyle">The optional default semantic style identifier.</param>
    /// <returns>The created command output.</returns>
    public static CommandOutput BlockText(string text, string? defaultStyle = null)
    {
        return Block(defaultStyle).Text(text).Build();
    }

    /// <summary>
    /// Resolves a segment style against this output's default style.
    /// </summary>
    /// <param name="segment">The segment to inspect.</param>
    /// <returns>The resolved semantic style identifier.</returns>
    public string? ResolveStyleId(CommandOutputSegment segment)
    {
        if (segment is null)
        {
            throw new ArgumentNullException(nameof(segment));
        }

        return segment.ResolveStyleId(DefaultStyleId);
    }
}
