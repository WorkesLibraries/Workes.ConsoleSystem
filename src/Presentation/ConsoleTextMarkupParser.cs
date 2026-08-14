using System;
using System.Collections.Generic;
using System.Text;

namespace Workes.ConsoleSystem.Presentation;

internal static class ConsoleTextMarkupParser
{
    public static ConsoleText Parse(string markup, string? defaultStyle, ConsoleMarkupProfile profile)
    {
        if (markup is null)
        {
            throw new ArgumentNullException(nameof(markup));
        }

        if (profile is null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        var segments = new List<ConsoleTextSegment>();
        var stack = new Stack<ConsoleMarkupProfile.MarkupScope>();
        var text = new StringBuilder();

        for (int index = 0; index < markup.Length;)
        {
            char current = markup[index];
            if (current == '<')
            {
                FlushText(text, segments, stack);
                int closeIndex = markup.IndexOf('>', index + 1);
                if (closeIndex < 0)
                {
                    throw new FormatException("Markup tag is not closed.");
                }

                string tag = markup.Substring(index + 1, closeIndex - index - 1).Trim();
                if (tag.Length == 0)
                {
                    throw new FormatException("Markup tags cannot be empty.");
                }

                if (tag[0] == '/')
                {
                    CloseTag(stack, tag.Substring(1).Trim());
                }
                else
                {
                    stack.Push(profile.ParseOpeningTag(tag));
                }

                index = closeIndex + 1;
                continue;
            }

            if (current == '&')
            {
                if (TryReadEntity(markup, index, out char decoded, out int consumed))
                {
                    text.Append(decoded);
                    index += consumed;
                    continue;
                }
            }

            text.Append(current);
            index++;
        }

        FlushText(text, segments, stack);
        if (stack.Count != 0)
        {
            throw new FormatException($"Markup tag '{stack.Peek().Name}' is not closed.");
        }

        return new ConsoleText(defaultStyle, segments);
    }

    public static string ToPlainText(string markup)
    {
        if (markup is null)
        {
            throw new ArgumentNullException(nameof(markup));
        }

        var text = new StringBuilder();
        var stack = new Stack<string>();

        for (int index = 0; index < markup.Length;)
        {
            char current = markup[index];
            if (current == '<')
            {
                int closeIndex = markup.IndexOf('>', index + 1);
                if (closeIndex < 0)
                {
                    throw new FormatException("Markup tag is not closed.");
                }

                string tag = markup.Substring(index + 1, closeIndex - index - 1).Trim();
                if (tag.Length == 0)
                {
                    throw new FormatException("Markup tags cannot be empty.");
                }

                if (tag[0] == '/')
                {
                    string closingName = tag.Substring(1).Trim();
                    if (stack.Count == 0)
                    {
                        throw new FormatException($"Closing tag '{closingName}' has no matching opening tag.");
                    }

                    string openingName = stack.Pop();
                    if (!string.Equals(openingName, closingName, StringComparison.Ordinal))
                    {
                        throw new FormatException($"Closing tag '{closingName}' does not match opening tag '{openingName}'.");
                    }
                }
                else
                {
                    stack.Push(ReadTagName(tag));
                }

                index = closeIndex + 1;
                continue;
            }

            if (current == '&' && TryReadEntity(markup, index, out char decoded, out int consumed))
            {
                text.Append(decoded);
                index += consumed;
                continue;
            }

            text.Append(current);
            index++;
        }

        if (stack.Count != 0)
        {
            throw new FormatException($"Markup tag '{stack.Peek()}' is not closed.");
        }

        return text.ToString();
    }

    private static void FlushText(StringBuilder text, List<ConsoleTextSegment> segments, Stack<ConsoleMarkupProfile.MarkupScope> stack)
    {
        if (text.Length == 0)
        {
            return;
        }

        string? styleId = null;
        ConsoleStyle inlineStyle = new ConsoleStyle();
        ConsoleMarkupProfile.MarkupScope[] scopes = stack.ToArray();
        for (int index = scopes.Length - 1; index >= 0; index--)
        {
            ConsoleMarkupProfile.MarkupScope scope = scopes[index];
            if (scope.StyleId is not null)
            {
                styleId = scope.StyleId;
            }

            inlineStyle = inlineStyle.Merge(scope.InlineStyle);
        }

        segments.Add(new ConsoleTextSegment(
            text.ToString(),
            styleId,
            null,
            inlineStyle.IsEmpty ? null : inlineStyle));
        text.Clear();
    }

    private static void CloseTag(Stack<ConsoleMarkupProfile.MarkupScope> stack, string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
        {
            throw new FormatException("Closing markup tags cannot be empty.");
        }

        if (stack.Count == 0)
        {
            throw new FormatException($"Closing tag '{tagName}' has no matching opening tag.");
        }

        ConsoleMarkupProfile.MarkupScope scope = stack.Pop();
        if (!string.Equals(scope.Name, tagName, StringComparison.Ordinal))
        {
            throw new FormatException($"Closing tag '{tagName}' does not match opening tag '{scope.Name}'.");
        }
    }

    private static bool TryReadEntity(string markup, int index, out char decoded, out int consumed)
    {
        if (StartsWith(markup, index, "&lt;"))
        {
            decoded = '<';
            consumed = 4;
            return true;
        }

        if (StartsWith(markup, index, "&gt;"))
        {
            decoded = '>';
            consumed = 4;
            return true;
        }

        if (StartsWith(markup, index, "&amp;"))
        {
            decoded = '&';
            consumed = 5;
            return true;
        }

        decoded = '\0';
        consumed = 0;
        return false;
    }

    private static bool StartsWith(string value, int index, string expected)
    {
        if (index + expected.Length > value.Length)
        {
            return false;
        }

        for (int offset = 0; offset < expected.Length; offset++)
        {
            if (value[index + offset] != expected[offset])
            {
                return false;
            }
        }

        return true;
    }

    private static string ReadTagName(string tag)
    {
        int equalsIndex = tag.IndexOf('=');
        string name = equalsIndex < 0 ? tag : tag.Substring(0, equalsIndex).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new FormatException("Markup tags cannot be empty.");
        }

        return name;
    }
}
