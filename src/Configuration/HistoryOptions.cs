using System;

namespace Workes.ConsoleSystem.Configuration;

/// <summary>
/// Configures retained console and command input history.
/// </summary>
public sealed class HistoryOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryOptions"/> class.
    /// </summary>
    public HistoryOptions()
    {
    }

    private HistoryOptions(HistoryOptions source)
    {
        ConsoleHistoryCapacity = ValidateCapacity(source.ConsoleHistoryCapacity, nameof(ConsoleHistoryCapacity));
        CommandHistoryCapacity = ValidateCapacity(source.CommandHistoryCapacity, nameof(CommandHistoryCapacity));
        CommandHistoryDuplicatePolicy = ValidateDuplicatePolicy(source.CommandHistoryDuplicatePolicy);
    }

    /// <summary>
    /// Gets or sets the maximum number of console entries retained.
    /// </summary>
    public int ConsoleHistoryCapacity { get; set; } = 200;

    /// <summary>
    /// Gets or sets the maximum number of submitted command inputs retained.
    /// </summary>
    public int CommandHistoryCapacity { get; set; } = 100;

    /// <summary>
    /// Gets or sets how submitted command input history handles duplicate entries.
    /// </summary>
    public CommandHistoryDuplicatePolicy CommandHistoryDuplicatePolicy { get; set; } = CommandHistoryDuplicatePolicy.RejectConsecutive;

    internal static HistoryOptions CreateSnapshot(HistoryOptions? source)
    {
        return source is null ? new HistoryOptions() : new HistoryOptions(source);
    }

    private static int ValidateCapacity(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Capacity values must be greater than zero.");
        }

        return value;
    }

    private static CommandHistoryDuplicatePolicy ValidateDuplicatePolicy(CommandHistoryDuplicatePolicy value)
    {
        if (!Enum.IsDefined(typeof(CommandHistoryDuplicatePolicy), value))
        {
            throw new ArgumentOutOfRangeException(nameof(CommandHistoryDuplicatePolicy), value, "Unknown command history duplicate policy.");
        }

        return value;
    }
}
