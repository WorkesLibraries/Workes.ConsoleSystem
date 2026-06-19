using System;
using Workes.ConsoleSystem.Commands;

namespace Workes.ConsoleSystem.Entries;

/// <summary>
/// Represents output produced by command handling.
/// </summary>
public sealed class CommandOutputEntry : IConsoleEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandOutputEntry"/> class.
    /// </summary>
    /// <param name="timestamp">The time associated with the entry.</param>
    /// <param name="level">The output severity or purpose.</param>
    /// <param name="message">The output message.</param>
    public CommandOutputEntry(DateTimeOffset timestamp, CommandOutputLevel level, string message)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Timestamp = timestamp;
        Level = level;
    }

    /// <inheritdoc />
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the output severity or purpose.
    /// </summary>
    public CommandOutputLevel Level { get; }

    /// <summary>
    /// Gets the output message.
    /// </summary>
    public string Message { get; }
}
