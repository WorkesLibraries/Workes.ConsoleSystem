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
    /// <param name="output">The semantic command output.</param>
    public CommandOutputEntry(DateTimeOffset timestamp, CommandOutput output)
    {
        Output = output ?? throw new ArgumentNullException(nameof(output));
        Timestamp = timestamp;
    }

    /// <inheritdoc />
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the semantic command output.
    /// </summary>
    public CommandOutput Output { get; }

    /// <summary>
    /// Gets the derived plain text for the output.
    /// </summary>
    public string PlainText => Output.PlainText;
}
