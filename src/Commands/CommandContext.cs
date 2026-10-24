using System;
using System.Threading;
using Workes.ConsoleSystem.Core;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Provides contextual information to a command handler during execution.
/// </summary>
public sealed class CommandContext
{
    private readonly object _gate = new object();
    private readonly Action<CommandOutput> _writeOutput;
    private readonly Action<CommandContext, string> _reportProgress;
    private object? _progressState;
    private bool _isActive = true;
    private bool _hasReportedProgress;

    internal CommandContext(
        string input,
        CommandDefinition definition,
        BoundCommand command,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken,
        ICommandDispatcher? dispatcher,
        object? progressState,
        Action<CommandOutput> writeOutput,
        Action<CommandContext, string> reportProgress)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Command = command ?? throw new ArgumentNullException(nameof(command));
        Timestamp = timestamp;
        CancellationToken = cancellationToken;
        Dispatcher = dispatcher;
        _progressState = progressState;
        _writeOutput = writeOutput ?? throw new ArgumentNullException(nameof(writeOutput));
        _reportProgress = reportProgress ?? throw new ArgumentNullException(nameof(reportProgress));
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

    /// <summary>
    /// Writes command output to the manager's visible history immediately.
    /// </summary>
    /// <param name="output">The semantic output to write.</param>
    /// <exception cref="ArgumentNullException">The output is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The command handler has already completed.</exception>
    public void WriteOutput(CommandOutput output)
    {
        if (output is null)
        {
            throw new ArgumentNullException(nameof(output));
        }

        lock (_gate)
        {
            EnsureActive();
            _writeOutput(output);
        }
    }

    /// <summary>
    /// Gets the custom progress state instance created for this command execution.
    /// </summary>
    /// <typeparam name="TProgressState">The progress state type configured on the command.</typeparam>
    /// <returns>The execution-specific progress state.</returns>
    /// <exception cref="InvalidOperationException">No progress state was configured, or its type does not match.</exception>
    public TProgressState GetProgressState<TProgressState>()
        where TProgressState : class
    {
        lock (_gate)
        {
            EnsureActive();
            if (_progressState is not TProgressState progressState)
            {
                throw new InvalidOperationException(
                    _progressState is null
                        ? "This command has no configured progress state."
                        : $"The configured progress state is {_progressState.GetType().Name}, not {typeof(TProgressState).Name}.");
            }

            return progressState;
        }
    }

    /// <summary>
    /// Reports a live progress message through the owning <see cref="ConsoleManager"/>.
    /// </summary>
    /// <param name="message">The progress message.</param>
    /// <exception cref="ArgumentNullException">The message is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The command handler has already completed.</exception>
    public void ReportProgress(string message)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        lock (_gate)
        {
            EnsureActive();
            _hasReportedProgress = true;
            _reportProgress(this, message);
        }
    }

    internal bool HasReportedProgress
    {
        get
        {
            lock (_gate)
            {
                return _hasReportedProgress;
            }
        }
    }

    internal object? ProgressState
    {
        get
        {
            lock (_gate)
            {
                return _progressState;
            }
        }
    }

    internal void Complete()
    {
        lock (_gate)
        {
            _isActive = false;
        }
    }

    private void EnsureActive()
    {
        if (!_isActive)
        {
            throw new InvalidOperationException("Command output and progress can only be written while the handler is running.");
        }
    }
}
