using System;

namespace Workes.ConsoleSystem.Commands;

/// <summary>Provides the context and state associated with a command progress notification.</summary>
public sealed class CommandProgressChangedEventArgs : EventArgs
{
    internal CommandProgressChangedEventArgs(
        CommandProgressEventKind kind,
        CommandContext context,
        string? message,
        object? progressState,
        CommandProgressOutcome? outcome)
    {
        Kind = kind;
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Message = message;
        ProgressState = progressState;
        Outcome = outcome;
    }

    /// <summary>Gets whether this is an in-progress update or a terminal notification.</summary>
    public CommandProgressEventKind Kind { get; }

    /// <summary>Gets the command execution that reported progress.</summary>
    public CommandContext Context { get; }

    /// <summary>Gets the message for an update, or <see langword="null"/> for a terminal notification.</summary>
    public string? Message { get; }

    /// <summary>Gets the custom state instance for this execution, when one was configured.</summary>
    public object? ProgressState { get; }

    /// <summary>Gets the terminal outcome, or <see langword="null"/> for an update.</summary>
    public CommandProgressOutcome? Outcome { get; }
}
