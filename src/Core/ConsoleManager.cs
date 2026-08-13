using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Configuration;
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
public sealed class ConsoleManager
{
    private readonly ConsoleManagerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleManager"/> class.
    /// </summary>
    public ConsoleManager()
        : this(new ConsoleManagerOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleManager"/> class.
    /// </summary>
    /// <param name="options">The manager options.</param>
    public ConsoleManager(ConsoleManagerOptions options)
    {
        _options = ConsoleManagerOptions.CreateSnapshot(options);

        History = new ConsoleHistory();
        CommandHistory = new CommandHistory();
        Log = new ConsoleLog(History);
        Commands = new CommandSystem();
    }

    /// <summary>
    /// Gets the resolved manager options.
    /// </summary>
    public ConsoleManagerOptions Options => ConsoleManagerOptions.CreateSnapshot(_options);

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
