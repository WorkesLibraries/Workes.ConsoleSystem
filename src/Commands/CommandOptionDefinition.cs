using System;
using System.Collections.Generic;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Describes a named command option.
/// </summary>
public sealed class CommandOptionDefinition : CommandMemberDefinition
{
    internal CommandOptionDefinition(
        string propertyName,
        Type valueType,
        string name,
        IReadOnlyList<string> aliases,
        string description,
        object? defaultValue,
        object? rangeMinimum,
        object? rangeMaximum,
        IReadOnlyList<object?> allowedValues,
        Func<CommandAutocompleteContext, IEnumerable<string>>? valueCandidateProvider)
        : base(propertyName, valueType, name, aliases, description, valueCandidateProvider)
    {
        DefaultValue = defaultValue;
        RangeMinimum = rangeMinimum;
        RangeMaximum = rangeMaximum;
        AllowedValues = allowedValues ?? throw new ArgumentNullException(nameof(allowedValues));
    }

    /// <summary>
    /// Gets the configured default value metadata.
    /// </summary>
    public object? DefaultValue { get; }

    /// <summary>
    /// Gets the configured inclusive range minimum metadata.
    /// </summary>
    public object? RangeMinimum { get; }

    /// <summary>
    /// Gets the configured inclusive range maximum metadata.
    /// </summary>
    public object? RangeMaximum { get; }

    /// <summary>
    /// Gets the configured allowed value metadata.
    /// </summary>
    public IReadOnlyList<object?> AllowedValues { get; }
}
