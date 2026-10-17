using System;
using System.Collections.Generic;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Contains a successfully parsed command and its bound values.
/// </summary>
public sealed class BoundCommand
{
    internal BoundCommand(
        string input,
        CommandDefinition definition,
        object? state,
        IReadOnlyDictionary<string, object?> arguments,
        IReadOnlyDictionary<string, object?> flags,
        IReadOnlyDictionary<string, object?> options)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        State = state;
        Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        Flags = flags ?? throw new ArgumentNullException(nameof(flags));
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Gets the original command input that produced this bound command.
    /// </summary>
    public string Input { get; }

    /// <summary>
    /// Gets the matched command definition.
    /// </summary>
    public CommandDefinition Definition { get; }

    /// <summary>
    /// Gets the typed command state when the command has one.
    /// </summary>
    public object? State { get; }

    /// <summary>
    /// Gets bound positional argument values by schema name.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Arguments { get; }

    /// <summary>
    /// Gets bound flag values by schema name.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Flags { get; }

    /// <summary>
    /// Gets bound option values by schema name.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Options { get; }

    /// <summary>
    /// Gets the typed command state.
    /// </summary>
    /// <typeparam name="TState">The expected command state type.</typeparam>
    /// <returns>The typed command state.</returns>
    public TState? GetState<TState>()
        where TState : class
    {
        return (TState?)State;
    }
}
