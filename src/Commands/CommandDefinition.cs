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
        Type? stateType,
        IReadOnlyList<CommandArgumentDefinition> arguments,
        IReadOnlyList<CommandFlagDefinition> flags,
        IReadOnlyList<CommandOptionDefinition> options,
        IReadOnlyList<CommandConstraintDefinition> constraints,
        Delegate handler)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        StateType = stateType;
        Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        Flags = flags ?? throw new ArgumentNullException(nameof(flags));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Constraints = constraints ?? throw new ArgumentNullException(nameof(constraints));
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

    /// <summary>
    /// Gets the typed command state type, if the command uses one.
    /// </summary>
    public Type? StateType { get; }

    /// <summary>
    /// Gets the required positional argument definitions.
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

    /// <summary>
    /// Gets the constraint metadata definitions.
    /// </summary>
    public IReadOnlyList<CommandConstraintDefinition> Constraints { get; }

    /// <summary>
    /// Gets a value indicating whether the command has a stored execution handler.
    /// </summary>
    public bool HasHandler => Handler is not null;

    internal Delegate Handler { get; }
}
