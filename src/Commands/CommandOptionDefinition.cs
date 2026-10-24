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
        string id,
        string name,
        IReadOnlyList<string> aliases,
        string description,
        IReadOnlyList<string> examples,
        object? rangeMinimum,
        object? rangeMaximum,
        IReadOnlyList<object?> allowedValues,
        bool suggestAllowedValues,
        Func<CommandAutocompleteContext, IEnumerable<string>>? valueCandidateProvider)
        : base(propertyName, valueType, id, name, aliases, description, examples, valueCandidateProvider, false, rangeMinimum, rangeMaximum, allowedValues, suggestAllowedValues)
    {
    }
}
