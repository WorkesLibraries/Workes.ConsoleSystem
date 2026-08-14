using System;
using System.Text;

namespace Workes.ConsoleSystem.Presentation;

internal static class ConsoleTextFormatting
{
    public static ConsoleStyle ResolveStyle(ConsoleText text, ConsoleTextSegment segment, ConsoleFormattingContext context)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        if (segment is null)
        {
            throw new ArgumentNullException(nameof(segment));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        ConsoleStyle resolvedStyle = new ConsoleStyle();
        string? styleId = text.ResolveStyleId(segment);
        if (styleId is not null && context.Theme.TryGetStyle(styleId, out ConsoleStyle? themeStyle) && themeStyle is not null)
        {
            resolvedStyle = themeStyle;
        }

        ConsoleStyle style = resolvedStyle.Merge(segment.InlineStyle);
        style.ValidateAgainst(context.Model);
        return style;
    }

    public static string Format(
        ConsoleText text,
        ConsoleFormattingContext context,
        Func<string, string> escape,
        Func<ConsoleStyle, string> open,
        Func<ConsoleStyle, string> close)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        var result = new StringBuilder();
        foreach (ConsoleTextSegment segment in text.Segments)
        {
            ConsoleStyle style = ResolveStyle(text, segment, context);
            result.Append(open(style));
            result.Append(escape(segment.Text));
            result.Append(close(style));
        }

        return result.ToString();
    }

    public static string EscapeXmlLike(string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    public static string EscapeBbCode(string value)
    {
        return value
            .Replace("[", "\\[")
            .Replace("]", "\\]");
    }

}
