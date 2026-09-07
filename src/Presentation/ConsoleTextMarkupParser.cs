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
                if (!TryReadRawTag(markup, index, profile, out RawTag rawTag))
                {
                    text.Append(current);
                    index++;
                    continue;
                }

                if (!rawTag.IsKnown)
                {
                    text.Append(rawTag.RawText);
                    index += rawTag.RawText.Length;
                    continue;
                }

                FlushText(text, segments, stack);
                if (rawTag.IsClosing)
                {
                    CloseTag(stack, rawTag.Name);
                }
                else
                {
                    stack.Push(rawTag.Scope!);
                }

                index += rawTag.RawText.Length;
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
        return ToPlainText(markup, ConsoleMarkupProfile.Standard);
    }

    public static string ToPlainText(string markup, ConsoleMarkupProfile profile)
    {
        if (markup is null)
        {
            throw new ArgumentNullException(nameof(markup));
        }

        if (profile is null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        var text = new StringBuilder();
        var stack = new Stack<string>();

        for (int index = 0; index < markup.Length;)
        {
            char current = markup[index];
            if (current == '<')
            {
                if (!TryReadRawTag(markup, index, profile, out RawTag rawTag))
                {
                    text.Append(current);
                    index++;
                    continue;
                }

                if (!rawTag.IsKnown)
                {
                    text.Append(rawTag.RawText);
                    index += rawTag.RawText.Length;
                    continue;
                }

                if (rawTag.IsClosing)
                {
                    if (stack.Count == 0)
                    {
                        throw new FormatException($"Closing tag '{rawTag.Name}' has no matching opening tag.");
                    }

                    string openingName = stack.Pop();
                    if (!string.Equals(openingName, rawTag.Name, StringComparison.Ordinal))
                    {
                        throw new FormatException($"Closing tag '{rawTag.Name}' does not match opening tag '{openingName}'.");
                    }
                }
                else
                {
                    stack.Push(rawTag.Name);
                }

                index += rawTag.RawText.Length;
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

    private static bool TryReadRawTag(
        string markup,
        int index,
        ConsoleMarkupProfile profile,
        out RawTag rawTag)
    {
        rawTag = default;
        int closeIndex = markup.IndexOf('>', index + 1);
        if (closeIndex < 0)
        {
            if (LooksLikeKnownTagStart(markup, index, profile))
            {
                throw new FormatException("Markup tag is not closed.");
            }

            return false;
        }

        string rawText = markup.Substring(index, closeIndex - index + 1);
        string tag = markup.Substring(index + 1, closeIndex - index - 1);
        if (!string.Equals(tag, tag.Trim(), StringComparison.Ordinal))
        {
            rawTag = RawTag.Unknown(rawText);
            return true;
        }

        if (tag.Length == 0)
        {
            rawTag = RawTag.Unknown(rawText);
            return true;
        }

        if (tag[0] == '/')
        {
            string closingName = tag.Substring(1);
            if (!profile.IsKnownTagName(closingName))
            {
                rawTag = RawTag.Unknown(rawText);
                return true;
            }

            rawTag = RawTag.Closing(rawText, closingName);
            return true;
        }

        ConsoleMarkupProfile.MarkupTagParseResult result = profile.TryParseOpeningTag(tag, out ConsoleMarkupProfile.MarkupScope? scope);
        if (result == ConsoleMarkupProfile.MarkupTagParseResult.Unknown)
        {
            rawTag = RawTag.Unknown(rawText);
            return true;
        }

        if (result == ConsoleMarkupProfile.MarkupTagParseResult.InvalidKnown)
        {
            throw new FormatException($"Invalid markup tag '{tag}'.");
        }

        rawTag = RawTag.Opening(rawText, scope!);
        return true;
    }

    private static bool LooksLikeKnownTagStart(string markup, int index, ConsoleMarkupProfile profile)
    {
        if (index + 1 >= markup.Length)
        {
            return false;
        }

        int nameStart = markup[index + 1] == '/' ? index + 2 : index + 1;
        if (nameStart >= markup.Length)
        {
            return false;
        }

        int nameEnd = nameStart;
        while (nameEnd < markup.Length)
        {
            char character = markup[nameEnd];
            if (character == '=' || character == '>' || char.IsWhiteSpace(character))
            {
                break;
            }

            nameEnd++;
        }

        if (nameEnd == nameStart)
        {
            return false;
        }

        string name = markup.Substring(nameStart, nameEnd - nameStart);
        return profile.IsKnownTagName(name);
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

    private struct RawTag
    {
        private RawTag(string rawText, bool isKnown, bool isClosing, string name, ConsoleMarkupProfile.MarkupScope? scope)
        {
            RawText = rawText;
            IsKnown = isKnown;
            IsClosing = isClosing;
            Name = name;
            Scope = scope;
        }

        public string RawText { get; }

        public bool IsKnown { get; }

        public bool IsClosing { get; }

        public string Name { get; }

        public ConsoleMarkupProfile.MarkupScope? Scope { get; }

        public static RawTag Unknown(string rawText)
        {
            return new RawTag(rawText, false, false, string.Empty, null);
        }

        public static RawTag Opening(string rawText, ConsoleMarkupProfile.MarkupScope scope)
        {
            return new RawTag(rawText, true, false, scope.Name, scope);
        }

        public static RawTag Closing(string rawText, string name)
        {
            return new RawTag(rawText, true, true, name, null);
        }
    }
}
