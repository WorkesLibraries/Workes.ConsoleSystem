using System;
using System.Collections.Generic;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Describes a command schema member bound to typed command state.
/// </summary>
public abstract class CommandMemberDefinition
{
    private protected CommandMemberDefinition(
        string propertyName,
        Type valueType,
        string name,
        IReadOnlyList<string> aliases,
        string description,
        Func<CommandAutocompleteContext, IEnumerable<string>>? valueCandidateProvider)
    {
        PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
        ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Aliases = aliases ?? throw new ArgumentNullException(nameof(aliases));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        ValueCandidateProvider = valueCandidateProvider;
    }

    /// <summary>
    /// Gets the bound state property name.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Gets the bound value type.
    /// </summary>
    public Type ValueType { get; }

    /// <summary>
    /// Gets the canonical schema name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the aliases for the member.
    /// </summary>
    public IReadOnlyList<string> Aliases { get; }

    /// <summary>
    /// Gets the optional member description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets a value indicating whether this member has a value autocomplete provider.
    /// </summary>
    public bool HasValueCandidates => ValueCandidateProvider is not null;

    internal Func<CommandAutocompleteContext, IEnumerable<string>>? ValueCandidateProvider { get; }
}
