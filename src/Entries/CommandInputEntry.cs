using System;
using Workes.ConsoleSystem.Presentation;

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
        : this(timestamp, ConsoleText.Plain(input))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandInputEntry"/> class.
    /// </summary>
    /// <param name="timestamp">The time associated with the entry.</param>
    /// <param name="content">The submitted command input content.</param>
    public CommandInputEntry(DateTimeOffset timestamp, ConsoleText content)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Timestamp = timestamp;
    }

    /// <inheritdoc />
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the submitted command input.
    /// </summary>
    public string Input => Content.PlainText;

    /// <summary>
    /// Gets the semantic submitted command input content.
    /// </summary>
    public ConsoleText Content { get; }
}
