using System;
using System.Collections.Generic;

namespace Workes.ConsoleSystem.Presentation;

/// <summary>
/// Defines how console markup tags map to formatting model attributes.
/// </summary>
public sealed class ConsoleMarkupProfile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleMarkupProfile"/> class.
    /// </summary>
    public ConsoleMarkupProfile(
        string styleTagName = "style",
        string colorTagName = "color",
        string boldTagName = "b",
        string italicTagName = "i",
        string underlineTagName = "u",
        string foregroundColorAttribute = ConsoleStandardFormatAttributes.ForegroundColor,
        string boldAttribute = ConsoleStandardFormatAttributes.Bold,
        string italicAttribute = ConsoleStandardFormatAttributes.Italic,
        string underlineAttribute = ConsoleStandardFormatAttributes.Underline)
    {
        StyleTagName = ValidateTagName(styleTagName, nameof(styleTagName));
        ColorTagName = ValidateTagName(colorTagName, nameof(colorTagName));
        BoldTagName = ValidateTagName(boldTagName, nameof(boldTagName));
        ItalicTagName = ValidateTagName(italicTagName, nameof(italicTagName));
        UnderlineTagName = ValidateTagName(underlineTagName, nameof(underlineTagName));
        ForegroundColorAttribute = ValidateAttribute(foregroundColorAttribute, nameof(foregroundColorAttribute));
        BoldAttribute = ValidateAttribute(boldAttribute, nameof(boldAttribute));
        ItalicAttribute = ValidateAttribute(italicAttribute, nameof(italicAttribute));
        UnderlineAttribute = ValidateAttribute(underlineAttribute, nameof(underlineAttribute));
    }

    /// <summary>
    /// Gets the standard markup profile.
    /// </summary>
    public static ConsoleMarkupProfile Standard { get; } = new ConsoleMarkupProfile();

    /// <summary>
    /// Gets the style lookup tag name.
    /// </summary>
    public string StyleTagName { get; }

    /// <summary>
    /// Gets the foreground color tag name.
    /// </summary>
    public string ColorTagName { get; }

    /// <summary>
    /// Gets the bold tag name.
    /// </summary>
    public string BoldTagName { get; }

    /// <summary>
    /// Gets the italic tag name.
    /// </summary>
    public string ItalicTagName { get; }

    /// <summary>
    /// Gets the underline tag name.
    /// </summary>
    public string UnderlineTagName { get; }

    /// <summary>
    /// Gets the foreground color attribute identifier.
    /// </summary>
    public string ForegroundColorAttribute { get; }

    /// <summary>
    /// Gets the bold attribute identifier.
    /// </summary>
    public string BoldAttribute { get; }

    /// <summary>
    /// Gets the italic attribute identifier.
    /// </summary>
    public string ItalicAttribute { get; }

    /// <summary>
    /// Gets the underline attribute identifier.
    /// </summary>
    public string UnderlineAttribute { get; }

    internal MarkupTagParseResult TryParseOpeningTag(string tag, out MarkupScope? scope)
    {
        if (string.Equals(tag, BoldTagName, StringComparison.Ordinal))
        {
            scope = new MarkupScope(BoldTagName, null, new ConsoleStyle(new Dictionary<string, object?>
            {
                [BoldAttribute] = true
            }));
            return MarkupTagParseResult.Known;
        }

        if (string.Equals(tag, ItalicTagName, StringComparison.Ordinal))
        {
            scope = new MarkupScope(ItalicTagName, null, new ConsoleStyle(new Dictionary<string, object?>
            {
                [ItalicAttribute] = true
            }));
            return MarkupTagParseResult.Known;
        }

        if (string.Equals(tag, UnderlineTagName, StringComparison.Ordinal))
        {
            scope = new MarkupScope(UnderlineTagName, null, new ConsoleStyle(new Dictionary<string, object?>
            {
                [UnderlineAttribute] = true
            }));
            return MarkupTagParseResult.Known;
        }

        string stylePrefix = StyleTagName + "=";
        if (tag.StartsWith(stylePrefix, StringComparison.Ordinal))
        {
            string styleId = tag.Substring(stylePrefix.Length).Trim();
            if (string.IsNullOrWhiteSpace(styleId))
            {
                throw new FormatException("Style tags require a non-empty style identifier.");
            }

            scope = new MarkupScope(StyleTagName, styleId, null);
            return MarkupTagParseResult.Known;
        }

        string colorPrefix = ColorTagName + "=";
        if (tag.StartsWith(colorPrefix, StringComparison.Ordinal))
        {
            string color = tag.Substring(colorPrefix.Length).Trim();
            scope = new MarkupScope(ColorTagName, null, new ConsoleStyle(new Dictionary<string, object?>
            {
                [ForegroundColorAttribute] = ConsoleColor.FromHex(color)
            }));
            return MarkupTagParseResult.Known;
        }

        scope = null;
        return IsKnownTagName(ReadTagName(tag))
            ? MarkupTagParseResult.InvalidKnown
            : MarkupTagParseResult.Unknown;
    }

    /// <summary>
    /// Validates this profile against a format model.
    /// </summary>
    public void ValidateAgainst(ConsoleFormatModel model)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        ValidateAttribute(model, ForegroundColorAttribute, ConsoleFormatAttributeKind.Value, typeof(ConsoleColor));
        ValidateAttribute(model, BoldAttribute, ConsoleFormatAttributeKind.Flag, null);
        ValidateAttribute(model, ItalicAttribute, ConsoleFormatAttributeKind.Flag, null);
        ValidateAttribute(model, UnderlineAttribute, ConsoleFormatAttributeKind.Flag, null);
    }

    private static string ValidateTagName(string value, string parameterName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Markup tag names cannot be empty.", parameterName);
        }

        if (value.IndexOfAny(new[] { '<', '>', '/', '=', ' ' }) >= 0)
        {
            throw new ArgumentException("Markup tag names cannot contain markup delimiter characters or spaces.", parameterName);
        }

        return value;
    }

    private static string ValidateAttribute(string value, string parameterName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Formatting attribute identifiers cannot be empty.", parameterName);
        }

        return value;
    }

    private static void ValidateAttribute(
        ConsoleFormatModel model,
        string id,
        ConsoleFormatAttributeKind expectedKind,
        Type? expectedValueType)
    {
        ConsoleFormatAttributeDefinition attribute = model.GetAttribute(id);
        if (attribute.Kind != expectedKind)
        {
            throw new ArgumentException($"Markup profile attribute '{id}' must target a {expectedKind} formatting attribute.");
        }

        if (expectedValueType is not null && attribute.ValueType != expectedValueType)
        {
            throw new ArgumentException($"Markup profile attribute '{id}' must target values of type '{expectedValueType.Name}'.");
        }
    }

    internal bool IsKnownTagName(string tagName)
    {
        return string.Equals(tagName, StyleTagName, StringComparison.Ordinal) ||
            string.Equals(tagName, ColorTagName, StringComparison.Ordinal) ||
            string.Equals(tagName, BoldTagName, StringComparison.Ordinal) ||
            string.Equals(tagName, ItalicTagName, StringComparison.Ordinal) ||
            string.Equals(tagName, UnderlineTagName, StringComparison.Ordinal);
    }

    private static string ReadTagName(string tag)
    {
        int equalsIndex = tag.IndexOf('=');
        string name = equalsIndex < 0 ? tag : tag.Substring(0, equalsIndex).Trim();
        return name;
    }

    internal enum MarkupTagParseResult
    {
        Unknown,
        Known,
        InvalidKnown
    }

    internal sealed class MarkupScope
    {
        public MarkupScope(string name, string? styleId, ConsoleStyle? inlineStyle)
        {
            Name = name;
            StyleId = styleId;
            InlineStyle = inlineStyle;
        }

        public string Name { get; }

        public string? StyleId { get; }

        public ConsoleStyle? InlineStyle { get; }
    }
}
