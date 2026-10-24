using System;
using System.Collections.Generic;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Describes a positional command argument.
/// </summary>
public sealed class CommandArgumentDefinition : CommandMemberDefinition
{
    internal CommandArgumentDefinition(
        string propertyName,
        Type valueType,
        string id,
        string name,
        string description,
        Func<CommandAutocompleteContext, IEnumerable<string>>? valueCandidateProvider,
        bool isOptional,
        object? rangeMinimum,
        object? rangeMaximum,
        IReadOnlyList<object?> allowedValues,
        bool suggestAllowedValues)
        : base(propertyName, valueType, id, name, Array.Empty<string>(), description, valueCandidateProvider, isOptional, rangeMinimum, rangeMaximum, allowedValues, suggestAllowedValues)
    {
    }
}
