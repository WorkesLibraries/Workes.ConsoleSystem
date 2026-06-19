using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.History;
using Workes.ConsoleSystem.Logging;

namespace Workes.ConsoleSystem.Core;

/// <summary>
/// Coordinates the shared console history, logging facade, command input history, and command system.
/// </summary>
/// <remarks>
/// Normal applications are expected to create one instance during startup and retain it for the
/// lifetime of the game, though the type does not enforce singleton usage.
/// </remarks>
public sealed class GameConsole
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GameConsole"/> class.
    /// </summary>
    public GameConsole()
    {
        History = new ConsoleHistory();
        CommandHistory = new CommandHistory();
        Log = new ConsoleLog(History);
        Commands = new CommandSystem();
    }

    /// <summary>
    /// Gets the shared chronological console history.
    /// </summary>
    public ConsoleHistory History { get; }

    /// <summary>
    /// Gets the interactive command input history used by console UIs.
    /// </summary>
    public CommandHistory CommandHistory { get; }

    /// <summary>
    /// Gets the developer-facing logging facade.
    /// </summary>
    public ConsoleLog Log { get; }

    /// <summary>
    /// Gets the command registry and future execution surface.
    /// </summary>
    public CommandSystem Commands { get; }
}
