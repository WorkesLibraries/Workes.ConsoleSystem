using System;

namespace Workes.ConsoleSystem.Entries;

/// <summary>
/// Represents command text submitted to the console.
/// </summary>
public sealed class CommandInputEntry : IConsoleEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandInputEntry"/> class.
    /// </summary>
    /// <param name="timestamp">The time associated with the entry.</param>
    /// <param name="input">The submitted command input.</param>
    public CommandInputEntry(DateTimeOffset timestamp, string input)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        Timestamp = timestamp;
    }

    /// <inheritdoc />
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the submitted command input.
    /// </summary>
    public string Input { get; }
}
