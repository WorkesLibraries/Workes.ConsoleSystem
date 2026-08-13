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
    }

    /// <summary>
    /// Gets or sets the maximum number of console entries retained.
    /// </summary>
    public int ConsoleHistoryCapacity { get; set; } = 200;

    /// <summary>
    /// Gets or sets the maximum number of submitted command inputs retained.
    /// </summary>
    public int CommandHistoryCapacity { get; set; } = 100;

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
}
