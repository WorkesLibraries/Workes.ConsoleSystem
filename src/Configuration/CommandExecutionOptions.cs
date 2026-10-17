using System;

namespace Workes.ConsoleSystem.Configuration;

/// <summary>
/// Configures command execution behavior.
/// </summary>
public sealed class CommandExecutionOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandExecutionOptions"/> class.
    /// </summary>
    public CommandExecutionOptions()
    {
    }

    private CommandExecutionOptions(CommandExecutionOptions source)
    {
        EchoInput = source.EchoInput;
    }

    /// <summary>
    /// Gets or sets a value indicating whether command execution should echo submitted input by default.
    /// </summary>
    public bool EchoInput { get; set; } = true;

    internal static CommandExecutionOptions CreateSnapshot(CommandExecutionOptions? source)
    {
        return source is null ? new CommandExecutionOptions() : new CommandExecutionOptions(source);
    }
}
