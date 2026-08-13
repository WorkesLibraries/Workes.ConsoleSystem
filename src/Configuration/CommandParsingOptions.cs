namespace Workes.ConsoleSystem.Configuration;

/// <summary>
/// Configures command parsing preferences.
/// </summary>
public sealed class CommandParsingOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandParsingOptions"/> class.
    /// </summary>
    public CommandParsingOptions()
    {
    }

    private CommandParsingOptions(CommandParsingOptions source)
    {
        OptionValueStyle = source.OptionValueStyle;
        IsCaseSensitive = source.IsCaseSensitive;
        AllowQuotedStrings = source.AllowQuotedStrings;
        AllowFlagsAndOptionsInAnyOrder = source.AllowFlagsAndOptionsInAnyOrder;
    }

    /// <summary>
    /// Gets or sets how command options accept values.
    /// </summary>
    public OptionValueStyle OptionValueStyle { get; set; } = OptionValueStyle.SpaceSeparated;

    /// <summary>
    /// Gets or sets a value indicating whether command paths, flags, and options are matched case-sensitively.
    /// </summary>
    public bool IsCaseSensitive { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether quoted strings are allowed in command input.
    /// </summary>
    public bool AllowQuotedStrings { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether flags and options may appear in any order after positional arguments.
    /// </summary>
    public bool AllowFlagsAndOptionsInAnyOrder { get; set; } = true;

    internal static CommandParsingOptions CreateSnapshot(CommandParsingOptions? source)
    {
        return source is null ? new CommandParsingOptions() : new CommandParsingOptions(source);
    }
}
