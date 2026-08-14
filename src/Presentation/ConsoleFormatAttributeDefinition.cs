using System;

namespace Workes.ConsoleSystem.Presentation;

/// <summary>
/// Describes one formatting attribute supported by a console format model.
/// </summary>
public sealed class ConsoleFormatAttributeDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleFormatAttributeDefinition"/> class.
    /// </summary>
    public ConsoleFormatAttributeDefinition(string id, ConsoleFormatAttributeKind kind, Type? valueType = null)
    {
        if (id is null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Formatting attribute identifiers cannot be empty.", nameof(id));
        }

        if (!Enum.IsDefined(typeof(ConsoleFormatAttributeKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (kind == ConsoleFormatAttributeKind.Value && valueType is null)
        {
            throw new ArgumentException("Value attributes require a value type.", nameof(valueType));
        }

        if (kind == ConsoleFormatAttributeKind.Flag && valueType is not null)
        {
            throw new ArgumentException("Flag attributes cannot define a value type.", nameof(valueType));
        }

        Id = id;
        Kind = kind;
        ValueType = valueType;
    }

    /// <summary>
    /// Gets the attribute identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the attribute kind.
    /// </summary>
    public ConsoleFormatAttributeKind Kind { get; }

    /// <summary>
    /// Gets the required value type for value attributes.
    /// </summary>
    public Type? ValueType { get; }
}
