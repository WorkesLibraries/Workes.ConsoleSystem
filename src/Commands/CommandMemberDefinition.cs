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
        string id,
        string name,
        IReadOnlyList<string> aliases,
        string description,
        IReadOnlyList<string>? examples,
        Func<CommandAutocompleteContext, IEnumerable<string>>? valueCandidateProvider,
        bool isOptional = false,
        object? rangeMinimum = null,
        object? rangeMaximum = null,
        IReadOnlyList<object?>? allowedValues = null,
        bool suggestAllowedValues = false)
    {
        PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
        ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Aliases = aliases ?? throw new ArgumentNullException(nameof(aliases));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Examples = examples ?? Array.Empty<string>();
        ValueCandidateProvider = valueCandidateProvider;
        IsOptional = isOptional;
        RangeMinimum = rangeMinimum;
        RangeMaximum = rangeMaximum;
        AllowedValues = allowedValues ?? Array.Empty<object?>();
        SuggestAllowedValues = suggestAllowedValues;
    }

    /// <summary>
    /// Gets the bound state property name.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Gets the bound value type.
    /// </summary>
    public Type ValueType { get; }

    /// <summary>Gets the logical state value ID shared by compatible input bindings.</summary>
    public string Id { get; }

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

    /// <summary>Gets sample values for this argument or option.</summary>
    public IReadOnlyList<string> Examples { get; }

    /// <summary>Gets whether this is an optional positional argument.</summary>
    public bool IsOptional { get; }

    /// <summary>Gets the configured inclusive range minimum metadata.</summary>
    public object? RangeMinimum { get; }

    /// <summary>Gets the configured inclusive range maximum metadata.</summary>
    public object? RangeMaximum { get; }

    /// <summary>Gets the configured allowed value metadata.</summary>
    public IReadOnlyList<object?> AllowedValues { get; }

    /// <summary>Gets whether allowed values are offered as autocomplete candidates.</summary>
    public bool SuggestAllowedValues { get; }

    /// <summary>
    /// Gets a value indicating whether this member has a value autocomplete provider.
    /// </summary>
    public bool HasValueCandidates => ValueCandidateProvider is not null;

    internal Func<CommandAutocompleteContext, IEnumerable<string>>? ValueCandidateProvider { get; }
}
