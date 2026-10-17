using System;

namespace Workes.ConsoleSystem.Configuration;

/// <summary>
/// Configures command autocomplete behavior.
/// </summary>
public sealed class CommandAutocompleteOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandAutocompleteOptions"/> class.
    /// </summary>
    public CommandAutocompleteOptions()
    {
    }

    private CommandAutocompleteOptions(CommandAutocompleteOptions source)
    {
        PathCompletionMode = ValidatePathCompletionMode(source.PathCompletionMode);
        ShowUnavailableCommands = source.ShowUnavailableCommands;
    }

    /// <summary>
    /// Gets or sets how command paths are completed.
    /// </summary>
    public CommandPathCompletionMode PathCompletionMode { get; set; } = CommandPathCompletionMode.FullPath;

    /// <summary>
    /// Gets or sets whether autocomplete includes commands the current caller cannot run.
    /// </summary>
    public bool ShowUnavailableCommands { get; set; }

    internal static CommandAutocompleteOptions CreateSnapshot(CommandAutocompleteOptions? source)
    {
        return source is null ? new CommandAutocompleteOptions() : new CommandAutocompleteOptions(source);
    }

    private static CommandPathCompletionMode ValidatePathCompletionMode(CommandPathCompletionMode value)
    {
        switch (value)
        {
            case CommandPathCompletionMode.FullPath:
            case CommandPathCompletionMode.DotSegment:
                return value;
            default:
                throw new ArgumentOutOfRangeException(nameof(PathCompletionMode), "Unknown command path completion mode.");
        }
    }
}
