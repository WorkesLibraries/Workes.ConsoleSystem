namespace Workes.ConsoleSystem.Commands;

/// <summary>Identifies whether a progress notification is an update or terminal outcome.</summary>
public enum CommandProgressEventKind
{
    /// <summary>A command reported a progress message while running.</summary>
    Updated = 0,

    /// <summary>A command that reported progress has finished.</summary>
    Completed = 1
}
