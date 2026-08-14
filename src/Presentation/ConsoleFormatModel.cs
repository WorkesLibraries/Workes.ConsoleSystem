using System;
using System.Collections.Generic;

namespace Workes.ConsoleSystem.Presentation;

/// <summary>
/// Defines the formatting attributes available to a console formatting setup.
/// </summary>
public sealed class ConsoleFormatModel
{
    private readonly IReadOnlyDictionary<string, ConsoleFormatAttributeDefinition> _attributes;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleFormatModel"/> class.
    /// </summary>
    public ConsoleFormatModel(IEnumerable<ConsoleFormatAttributeDefinition> attributes)
    {
        if (attributes is null)
        {
            throw new ArgumentNullException(nameof(attributes));
        }

        var copiedAttributes = new Dictionary<string, ConsoleFormatAttributeDefinition>(StringComparer.Ordinal);
        foreach (ConsoleFormatAttributeDefinition attribute in attributes)
        {
            if (attribute is null)
            {
                throw new ArgumentException("Format models cannot contain null attributes.", nameof(attributes));
            }

            if (!copiedAttributes.TryAdd(attribute.Id, attribute))
            {
                throw new ArgumentException($"Duplicate formatting attribute identifier '{attribute.Id}'.", nameof(attributes));
            }
        }

        _attributes = copiedAttributes;
    }

    /// <summary>
    /// Gets the standard console format model.
    /// </summary>
    public static ConsoleFormatModel Standard { get; } = new ConsoleFormatModel(new[]
    {
        new ConsoleFormatAttributeDefinition(ConsoleStandardFormatAttributes.ForegroundColor, ConsoleFormatAttributeKind.Value, typeof(ConsoleColor)),
        new ConsoleFormatAttributeDefinition(ConsoleStandardFormatAttributes.Bold, ConsoleFormatAttributeKind.Flag),
        new ConsoleFormatAttributeDefinition(ConsoleStandardFormatAttributes.Italic, ConsoleFormatAttributeKind.Flag),
        new ConsoleFormatAttributeDefinition(ConsoleStandardFormatAttributes.Underline, ConsoleFormatAttributeKind.Flag)
    });

    /// <summary>
    /// Gets the model attributes by identifier.
    /// </summary>
    public IReadOnlyDictionary<string, ConsoleFormatAttributeDefinition> Attributes => _attributes;

    /// <summary>
    /// Gets a value indicating whether the model contains the specified attribute.
    /// </summary>
    public bool ContainsAttribute(string id)
    {
        if (id is null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        return _attributes.ContainsKey(id);
    }

    /// <summary>
    /// Gets an attribute definition.
    /// </summary>
    public ConsoleFormatAttributeDefinition GetAttribute(string id)
    {
        if (id is null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        if (!_attributes.TryGetValue(id, out ConsoleFormatAttributeDefinition? attribute))
        {
            throw new ArgumentException($"Formatting attribute '{id}' is not defined by the format model.", nameof(id));
        }

        return attribute;
    }
}
