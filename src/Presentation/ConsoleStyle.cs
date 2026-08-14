using System;
using System.Collections.Generic;

namespace Workes.ConsoleSystem.Presentation;

/// <summary>
/// Describes engine-neutral text formatting attributes.
/// </summary>
public sealed class ConsoleStyle
{
    private readonly IReadOnlyDictionary<string, object?> _attributes;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleStyle"/> class.
    /// </summary>
    public ConsoleStyle()
        : this(new Dictionary<string, object?>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleStyle"/> class.
    /// </summary>
    public ConsoleStyle(IDictionary<string, object?> attributes)
    {
        if (attributes is null)
        {
            throw new ArgumentNullException(nameof(attributes));
        }

        var copiedAttributes = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, object?> attribute in attributes)
        {
            if (string.IsNullOrWhiteSpace(attribute.Key))
            {
                throw new ArgumentException("Formatting attribute identifiers cannot be empty.", nameof(attributes));
            }

            copiedAttributes.Add(attribute.Key, attribute.Value);
        }

        _attributes = copiedAttributes;
    }

    /// <summary>
    /// Gets the style attributes.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Attributes => _attributes;

    /// <summary>
    /// Creates a style for the standard format model.
    /// </summary>
    public static ConsoleStyle Standard(
        ConsoleColor? foregroundColor = null,
        bool bold = false,
        bool italic = false,
        bool underline = false)
    {
        var attributes = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (foregroundColor is not null)
        {
            attributes.Add(ConsoleStandardFormatAttributes.ForegroundColor, foregroundColor);
        }

        if (bold)
        {
            attributes.Add(ConsoleStandardFormatAttributes.Bold, true);
        }

        if (italic)
        {
            attributes.Add(ConsoleStandardFormatAttributes.Italic, true);
        }

        if (underline)
        {
            attributes.Add(ConsoleStandardFormatAttributes.Underline, true);
        }

        return new ConsoleStyle(attributes);
    }

    /// <summary>
    /// Merges another style over this one.
    /// </summary>
    public ConsoleStyle Merge(ConsoleStyle? overrideStyle)
    {
        if (overrideStyle is null)
        {
            return this;
        }

        var mergedAttributes = new Dictionary<string, object?>(_attributes, StringComparer.Ordinal);
        foreach (KeyValuePair<string, object?> attribute in overrideStyle.Attributes)
        {
            mergedAttributes[attribute.Key] = attribute.Value;
        }

        return new ConsoleStyle(mergedAttributes);
    }

    /// <summary>
    /// Gets a value indicating whether this style contains the specified flag attribute.
    /// </summary>
    public bool HasFlag(string id)
    {
        if (id is null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        return _attributes.TryGetValue(id, out object? value) && value is true;
    }

    /// <summary>
    /// Gets the value for a formatting attribute.
    /// </summary>
    public T? GetValue<T>(string id)
        where T : class
    {
        if (id is null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        return _attributes.TryGetValue(id, out object? value) ? value as T : null;
    }

    /// <summary>
    /// Validates this style against a format model.
    /// </summary>
    public void ValidateAgainst(ConsoleFormatModel model)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        foreach (KeyValuePair<string, object?> attribute in _attributes)
        {
            ConsoleFormatAttributeDefinition definition = model.GetAttribute(attribute.Key);
            if (definition.Kind == ConsoleFormatAttributeKind.Flag)
            {
                if (attribute.Value is not true)
                {
                    throw new ArgumentException($"Flag formatting attribute '{attribute.Key}' must use the value true.");
                }

                continue;
            }

            Type valueType = definition.ValueType ?? throw new InvalidOperationException("Value attribute definitions must have a value type.");
            if (attribute.Value is null || !valueType.IsInstanceOfType(attribute.Value))
            {
                throw new ArgumentException($"Formatting attribute '{attribute.Key}' requires values of type '{valueType.Name}'.");
            }
        }
    }

    internal bool IsEmpty => _attributes.Count == 0;
}
