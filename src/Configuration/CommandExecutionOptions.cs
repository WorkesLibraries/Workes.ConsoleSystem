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
        EchoInputDefaultStyle = source.EchoInputDefaultStyle;
    }

    /// <summary>
    /// Gets or sets a value indicating whether command execution should echo submitted input by default.
    /// </summary>
    public bool EchoInput { get; set; } = true;

    /// <summary>
    /// Gets or sets the optional default style used for echoed command input.
    /// </summary>
    public string? EchoInputDefaultStyle { get; set; }

    internal static CommandExecutionOptions CreateSnapshot(CommandExecutionOptions? source)
    {
        var snapshot = source is null ? new CommandExecutionOptions() : new CommandExecutionOptions(source);
        if (snapshot.EchoInputDefaultStyle is not null && string.IsNullOrWhiteSpace(snapshot.EchoInputDefaultStyle))
        {
            throw new ArgumentException("Echo input style identifiers cannot be empty.", nameof(EchoInputDefaultStyle));
        }

        return snapshot;
    }
}
