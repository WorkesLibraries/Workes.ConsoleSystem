using System;
using Workes.ConsoleSystem.Logging;
using Workes.ConsoleSystem.Presentation;

namespace Workes.ConsoleSystem.Entries;

/// <summary>
/// Represents a general game, tool, or mod log message in the console history.
/// </summary>
public sealed class LogEntry : IConsoleEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogEntry"/> class.
    /// </summary>
    /// <param name="timestamp">The time associated with the entry.</param>
    /// <param name="level">The severity of the log message.</param>
    /// <param name="message">The log message.</param>
    public LogEntry(DateTimeOffset timestamp, LogLevel level, string message)
        : this(timestamp, level, ConsoleText.Plain(message))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogEntry"/> class.
    /// </summary>
    /// <param name="timestamp">The time associated with the entry.</param>
    /// <param name="level">The severity of the log message.</param>
    /// <param name="content">The log content.</param>
    public LogEntry(DateTimeOffset timestamp, LogLevel level, ConsoleText content)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Timestamp = timestamp;
        Level = level;
    }

    /// <inheritdoc />
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the severity of the log message.
    /// </summary>
    public LogLevel Level { get; }

    /// <summary>
    /// Gets the log message.
    /// </summary>
    public string Message => Content.PlainText;

    /// <summary>
    /// Gets the semantic log content.
    /// </summary>
    public ConsoleText Content { get; }
}
