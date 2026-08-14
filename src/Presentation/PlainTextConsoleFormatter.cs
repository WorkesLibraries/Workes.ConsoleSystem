using System;

namespace Workes.ConsoleSystem.Presentation;

/// <summary>
/// Formats console text as plain text.
/// </summary>
public sealed class PlainTextConsoleFormatter : IConsoleTextFormatter
{
    /// <inheritdoc />
    public string Format(ConsoleText text, ConsoleFormattingContext context)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        return text.PlainText;
    }
}
