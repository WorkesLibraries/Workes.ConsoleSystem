using System;
using Workes.ConsoleSystem.Presentation;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Builds semantic command output.
/// </summary>
public sealed class CommandOutputBuilder
{
    private readonly CommandOutputKind _kind;
    private readonly ConsoleTextBuilder _textBuilder;

    internal CommandOutputBuilder(CommandOutputKind kind, string? defaultStyle)
    {
        if (defaultStyle is not null && string.IsNullOrWhiteSpace(defaultStyle))
        {
            throw new ArgumentException("Default style identifiers cannot be empty.", nameof(defaultStyle));
        }

        _kind = kind;
        _textBuilder = ConsoleText.Build(defaultStyle);
    }

    /// <summary>
    /// Appends plain text without an explicit segment style.
    /// </summary>
    /// <param name="text">The text to append.</param>
    /// <returns>The same builder.</returns>
    public CommandOutputBuilder Text(string text)
    {
        _textBuilder.Text(text);
        return this;
    }

    /// <summary>
    /// Appends a semantic value segment.
    /// </summary>
    /// <param name="text">The rendered text for the value.</param>
    /// <param name="style">The optional semantic style identifier.</param>
    /// <param name="data">The optional structured value represented by the segment.</param>
    /// <returns>The same builder.</returns>
    public CommandOutputBuilder Value(string text, string? style = null, object? data = null)
    {
        _textBuilder.Value(text, style, data);
        return this;
    }

    /// <summary>
    /// Creates the immutable command output.
    /// </summary>
    /// <returns>The created command output.</returns>
    public CommandOutput Build()
    {
        return new CommandOutput(_kind, _textBuilder.Build());
    }
}
