using System;
using System.Threading;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Provides contextual information to a command handler during execution.
/// </summary>
public sealed class CommandContext
{
    internal CommandContext(
        string input,
        CommandDefinition definition,
        BoundCommand command,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken,
        ICommandDispatcher? dispatcher)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Command = command ?? throw new ArgumentNullException(nameof(command));
        Timestamp = timestamp;
        CancellationToken = cancellationToken;
        Dispatcher = dispatcher;
    }

    /// <summary>
    /// Gets the original command input being executed.
    /// </summary>
    public string Input { get; }

    /// <summary>
    /// Gets the matched command definition.
    /// </summary>
    public CommandDefinition Definition { get; }

    /// <summary>
    /// Gets the bound command being executed.
    /// </summary>
    public BoundCommand Command { get; }

    /// <summary>
    /// Gets the time associated with this execution.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the token that cancels this asynchronous command execution, or <see cref="CancellationToken.None"/>
    /// for synchronous execution.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>Gets the optional host dispatcher for explicitly dispatched work.</summary>
    /// <remarks>The dispatcher is <see langword="null"/> when none was configured on the manager.</remarks>
    public ICommandDispatcher? Dispatcher { get; }
}
