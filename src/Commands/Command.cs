namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Base type for commands that can eventually be registered with a <see cref="CommandSystem"/>.
/// </summary>
public abstract class Command
{
    /// <summary>
    /// Gets the command metadata.
    /// </summary>
    public abstract CommandDefinition Definition { get; }
}
