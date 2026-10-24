namespace Workes.ConsoleSystem.Commands;

/// <summary>Describes how a command execution that reported progress ended.</summary>
public enum CommandProgressOutcome
{
    /// <summary>The command completed successfully.</summary>
    Succeeded = 0,

    /// <summary>The command completed with a failure.</summary>
    Failed = 1,

    /// <summary>The command was cancelled by its execution cancellation token.</summary>
    Cancelled = 2
}
