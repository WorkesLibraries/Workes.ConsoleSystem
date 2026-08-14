using System.Collections.Generic;
using System.Text;

namespace Workes.ConsoleSystem.Presentation;

/// <summary>
/// Formats console text as Godot BBCode.
/// </summary>
public sealed class GodotBbCodeConsoleFormatter : IConsoleTextFormatter
{
    /// <inheritdoc />
    public string Format(ConsoleText text, ConsoleFormattingContext context)
    {
        return ConsoleTextFormatting.Format(text, context, ConsoleTextFormatting.EscapeBbCode, Open, Close);
    }

    private static string Open(ConsoleStyle style)
    {
        var builder = new StringBuilder();
        ConsoleColor? foregroundColor = style.GetValue<ConsoleColor>(ConsoleStandardFormatAttributes.ForegroundColor);
        if (foregroundColor is not null)
        {
            builder.Append("[color=");
            builder.Append(foregroundColor);
            builder.Append(']');
        }

        if (style.HasFlag(ConsoleStandardFormatAttributes.Bold))
        {
            builder.Append("[b]");
        }

        if (style.HasFlag(ConsoleStandardFormatAttributes.Italic))
        {
            builder.Append("[i]");
        }

        if (style.HasFlag(ConsoleStandardFormatAttributes.Underline))
        {
            builder.Append("[u]");
        }

        return builder.ToString();
    }

    private static string Close(ConsoleStyle style)
    {
        var tags = new List<string>();
        if (style.HasFlag(ConsoleStandardFormatAttributes.Underline))
        {
            tags.Add("u");
        }

        if (style.HasFlag(ConsoleStandardFormatAttributes.Italic))
        {
            tags.Add("i");
        }

        if (style.HasFlag(ConsoleStandardFormatAttributes.Bold))
        {
            tags.Add("b");
        }

        if (style.GetValue<ConsoleColor>(ConsoleStandardFormatAttributes.ForegroundColor) is not null)
        {
            tags.Add("color");
        }

        var builder = new StringBuilder();
        foreach (string tag in tags)
        {
            builder.Append("[/");
            builder.Append(tag);
            builder.Append(']');
        }

        return builder.ToString();
    }
}
