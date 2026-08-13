using System;

namespace Workes.ConsoleSystem.Configuration;

/// <summary>
/// Configures the root console manager.
/// </summary>
public sealed class ConsoleManagerOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleManagerOptions"/> class.
    /// </summary>
    public ConsoleManagerOptions()
    {
    }

    private ConsoleManagerOptions(ConsoleManagerOptions source)
    {
        CommandParsing = CommandParsingOptions.CreateSnapshot(source.CommandParsing);
        History = HistoryOptions.CreateSnapshot(source.History);
        Presentation = PresentationOptions.CreateSnapshot(source.Presentation);
    }

    /// <summary>
    /// Gets or sets command parsing options.
    /// </summary>
    public CommandParsingOptions CommandParsing { get; set; } = new CommandParsingOptions();

    /// <summary>
    /// Gets or sets retained history options.
    /// </summary>
    public HistoryOptions History { get; set; } = new HistoryOptions();

    /// <summary>
    /// Gets or sets presentation extension-point options.
    /// </summary>
    public PresentationOptions Presentation { get; set; } = new PresentationOptions();

    internal static ConsoleManagerOptions CreateSnapshot(ConsoleManagerOptions source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return new ConsoleManagerOptions(source);
    }
}
