using System;
using System.Collections.Generic;
using Workes.ConsoleSystem.Presentation;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Represents semantic text output produced by a command.
/// </summary>
public sealed class CommandOutput
{
    internal CommandOutput(CommandOutputKind kind, ConsoleText content)
    {
        Kind = kind;
        Content = content ?? throw new ArgumentNullException(nameof(content));
        DefaultStyleId = content.DefaultStyleId;
        PlainText = content.PlainText;
        Segments = content.Segments;
    }

    private CommandOutput(CommandOutputKind kind, string text, string? defaultStyle)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        if (defaultStyle is not null && string.IsNullOrWhiteSpace(defaultStyle))
        {
            throw new ArgumentException("Default style identifiers cannot be empty.", nameof(defaultStyle));
        }

        Kind = kind;
        Text = text;
        DefaultStyleId = defaultStyle;
        PlainText = ConsoleTextMarkupParser.ToPlainText(text);
        Segments = Array.Empty<ConsoleTextSegment>();
    }

    /// <summary>
    /// Gets the structural output kind.
    /// </summary>
    public CommandOutputKind Kind { get; }

    /// <summary>
    /// Gets the semantic console text content.
    /// </summary>
    public ConsoleText? Content { get; }

    /// <summary>
    /// Gets unresolved text when the output was authored from a string.
    /// </summary>
    public string? Text { get; }

    /// <summary>
    /// Gets a value indicating whether this output was authored from a string.
    /// </summary>
    public bool IsText => Text is not null;

    /// <summary>
    /// Gets the optional semantic style identifier used by unstylized segments.
    /// </summary>
    public string? DefaultStyleId { get; }

    /// <summary>
    /// Gets the semantic output segments.
    /// </summary>
    public IReadOnlyList<ConsoleTextSegment> Segments { get; }

    /// <summary>
    /// Gets the derived plain text for the output.
    /// </summary>
    public string PlainText { get; }

    /// <summary>
    /// Starts building inline command output.
    /// </summary>
    /// <param name="defaultStyle">The optional default semantic style identifier.</param>
    /// <returns>The output builder.</returns>
    public static CommandOutputBuilder BuildInline(string? defaultStyle = null)
    {
        return new CommandOutputBuilder(CommandOutputKind.Inline, defaultStyle);
    }

    /// <summary>
    /// Starts building block command output.
    /// </summary>
    /// <param name="defaultStyle">The optional default semantic style identifier.</param>
    /// <returns>The output builder.</returns>
    public static CommandOutputBuilder BuildBlock(string? defaultStyle = null)
    {
        return new CommandOutputBuilder(CommandOutputKind.Block, defaultStyle);
    }

    /// <summary>
    /// Creates simple inline command output from formatting-aware text.
    /// </summary>
    /// <param name="text">The output text.</param>
    /// <param name="defaultStyle">The optional default semantic style identifier.</param>
    /// <returns>The created command output.</returns>
    public static CommandOutput Inline(string text, string? defaultStyle = null)
    {
        return new CommandOutput(CommandOutputKind.Inline, text, defaultStyle);
    }

    /// <summary>
    /// Creates simple block command output from formatting-aware text.
    /// </summary>
    /// <param name="text">The output text.</param>
    /// <param name="defaultStyle">The optional default semantic style identifier.</param>
    /// <returns>The created command output.</returns>
    public static CommandOutput Block(string text, string? defaultStyle = null)
    {
        return new CommandOutput(CommandOutputKind.Block, text, defaultStyle);
    }

    /// <summary>
    /// Creates inline command output from console text.
    /// </summary>
    public static CommandOutput Inline(ConsoleText content)
    {
        return new CommandOutput(CommandOutputKind.Inline, content);
    }

    /// <summary>
    /// Creates block command output from console text.
    /// </summary>
    public static CommandOutput Block(ConsoleText content)
    {
        return new CommandOutput(CommandOutputKind.Block, content);
    }

    /// <summary>
    /// Resolves a segment style against this output's default style.
    /// </summary>
    /// <param name="segment">The segment to inspect.</param>
    /// <returns>The resolved semantic style identifier.</returns>
    public string? ResolveStyleId(ConsoleTextSegment segment)
    {
        if (Content is null)
        {
            throw new InvalidOperationException("String-authored command output must be resolved by a ConsoleManager before segment styles can be inspected.");
        }

        return Content.ResolveStyleId(segment);
    }
}
