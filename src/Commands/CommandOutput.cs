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
        DefaultSemanticId = content.DefaultSemanticId;
        PlainText = content.PlainText;
        Segments = content.Segments;
    }

    private CommandOutput(CommandOutputKind kind, string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        Kind = kind;
        Content = ConsoleText.Plain(text);
        DefaultSemanticId = Content.DefaultSemanticId;
        PlainText = Content.PlainText;
        Segments = Content.Segments;
    }

    /// <summary>
    /// Gets the structural output kind.
    /// </summary>
    public CommandOutputKind Kind { get; }

    /// <summary>
    /// Gets the semantic console text content.
    /// </summary>
    public ConsoleText Content { get; }

    /// <summary>
    /// Gets the optional semantic identifier used by unclassified segments.
    /// </summary>
    public string? DefaultSemanticId { get; }

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
    /// <param name="defaultSemanticId">The optional default semantic identifier.</param>
    /// <returns>The output builder.</returns>
    public static CommandOutputBuilder BuildInline(string? defaultSemanticId = null)
    {
        return new CommandOutputBuilder(CommandOutputKind.Inline, defaultSemanticId);
    }

    /// <summary>
    /// Starts building block command output.
    /// </summary>
    /// <param name="defaultSemanticId">The optional default semantic identifier.</param>
    /// <returns>The output builder.</returns>
    public static CommandOutputBuilder BuildBlock(string? defaultSemanticId = null)
    {
        return new CommandOutputBuilder(CommandOutputKind.Block, defaultSemanticId);
    }

    /// <summary>
    /// Creates simple inline command output from literal plain text.
    /// </summary>
    /// <param name="text">The output text.</param>
    /// <returns>The created command output.</returns>
    public static CommandOutput Inline(string text)
    {
        return new CommandOutput(CommandOutputKind.Inline, text);
    }

    /// <summary>
    /// Creates simple block command output from literal plain text.
    /// </summary>
    /// <param name="text">The output text.</param>
    /// <returns>The created command output.</returns>
    public static CommandOutput Block(string text)
    {
        return new CommandOutput(CommandOutputKind.Block, text);
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
    /// Resolves a segment semantic identifier against this output's default semantic identifier.
    /// </summary>
    /// <param name="segment">The segment to inspect.</param>
    /// <returns>The resolved semantic identifier.</returns>
    public string? ResolveSemanticId(ConsoleTextSegment segment)
    {
        return Content.ResolveSemanticId(segment);
    }
}
