using System;
using System.Collections.Generic;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Describes settled command metadata.
/// </summary>
public sealed class CommandDefinition
{
    internal CommandDefinition(
        string path,
        string description,
        IReadOnlyList<string> notes,
        IReadOnlyList<string> examples,
        string? syntax,
        CommandSyntaxFormatter? syntaxFormatter,
        Type? stateType,
        IReadOnlyList<CommandArgumentDefinition> arguments,
        IReadOnlyList<CommandFlagDefinition> flags,
        IReadOnlyList<CommandOptionDefinition> options,
        IReadOnlyDictionary<string, object?> defaults,
        IReadOnlyList<CommandConstraintDefinition> constraints,
        IReadOnlyList<string> requiredPermissions,
        IReadOnlyList<IReadOnlyList<string>> requiredAnyPermissionGroups,
        IReadOnlyList<Func<CommandAvailabilityContext, CommandAvailabilityResult>> availabilityChecks,
        CommandEchoInputDefinition echoInput,
        IReadOnlyList<CommandSuccessOutputDefinition> successOutputs,
        Delegate handler)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Notes = notes ?? throw new ArgumentNullException(nameof(notes));
        Examples = examples ?? throw new ArgumentNullException(nameof(examples));
        Syntax = syntax;
        SyntaxFormatter = syntaxFormatter;
        StateType = stateType;
        Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        Flags = flags ?? throw new ArgumentNullException(nameof(flags));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Defaults = defaults ?? throw new ArgumentNullException(nameof(defaults));
        Constraints = constraints ?? throw new ArgumentNullException(nameof(constraints));
        RequiredPermissions = requiredPermissions ?? throw new ArgumentNullException(nameof(requiredPermissions));
        RequiredAnyPermissionGroups = requiredAnyPermissionGroups ?? throw new ArgumentNullException(nameof(requiredAnyPermissionGroups));
        AvailabilityChecks = availabilityChecks ?? throw new ArgumentNullException(nameof(availabilityChecks));
        EchoInput = echoInput ?? throw new ArgumentNullException(nameof(echoInput));
        SuccessOutputs = successOutputs ?? throw new ArgumentNullException(nameof(successOutputs));
        Handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <summary>
    /// Gets the complete command path.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets a short description of the command.
    /// </summary>
    public string Description { get; }

    /// <summary>Gets optional command notes.</summary>
    public IReadOnlyList<string> Notes { get; }

    /// <summary>Gets complete invocation examples for this command.</summary>
    public IReadOnlyList<string> Examples { get; }

    /// <summary>Gets manually authored syntax, or <see langword="null"/> when syntax is absent or generated.</summary>
    public string? Syntax { get; }

    /// <summary>Gets whether this command has manually supplied or generated syntax.</summary>
    public bool HasSyntax => Syntax is not null || SyntaxFormatter is not null;

    /// <summary>
    /// Gets the typed command state type, if the command uses one.
    /// </summary>
    public Type? StateType { get; }

    /// <summary>
    /// Gets the positional argument definitions in input order.
    /// </summary>
    public IReadOnlyList<CommandArgumentDefinition> Arguments { get; }

    /// <summary>
    /// Gets the flag definitions.
    /// </summary>
    public IReadOnlyList<CommandFlagDefinition> Flags { get; }

    /// <summary>
    /// Gets the option definitions.
    /// </summary>
    public IReadOnlyList<CommandOptionDefinition> Options { get; }

    /// <summary>Gets configured defaults by logical value ID.</summary>
    public IReadOnlyDictionary<string, object?> Defaults { get; }

    /// <summary>
    /// Gets the constraint metadata definitions.
    /// </summary>
    public IReadOnlyList<CommandConstraintDefinition> Constraints { get; }

    /// <summary>Gets the permissions that are all required to execute this command.</summary>
    public IReadOnlyList<string> RequiredPermissions { get; }

    /// <summary>Gets permission groups for which at least one permission in each group is required.</summary>
    public IReadOnlyList<IReadOnlyList<string>> RequiredAnyPermissionGroups { get; }

    /// <summary>Gets whether the command has runtime availability checks.</summary>
    public bool HasAvailabilityChecks => AvailabilityChecks.Count > 0;

    /// <summary>
    /// Gets command-specific input echo metadata.
    /// </summary>
    public CommandEchoInputDefinition EchoInput { get; }

    /// <summary>
    /// Gets declared success output metadata.
    /// </summary>
    public IReadOnlyList<CommandSuccessOutputDefinition> SuccessOutputs { get; }

    /// <summary>
    /// Gets a value indicating whether the command has a stored execution handler.
    /// </summary>
    public bool HasHandler => Handler is not null;

    internal Delegate Handler { get; }
    internal CommandSyntaxFormatter? SyntaxFormatter { get; }

    internal IReadOnlyList<Func<CommandAvailabilityContext, CommandAvailabilityResult>> AvailabilityChecks { get; }
}
