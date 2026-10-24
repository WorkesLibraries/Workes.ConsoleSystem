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
        : this(timestamp, level, ConsoleText.Plain(message), source: null, exception: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogEntry"/> class with optional metadata values.
    /// </summary>
    /// <param name="timestamp">The time associated with the entry.</param>
    /// <param name="level">The severity of the log message.</param>
    /// <param name="message">The log message.</param>
    /// <param name="source">The optional source or category associated with the message.</param>
    /// <param name="exception">The optional exception associated with the message.</param>
    public LogEntry(DateTimeOffset timestamp, LogLevel level, string message, string? source, Exception? exception = null)
        : this(timestamp, level, ConsoleText.Plain(message), source, exception)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogEntry"/> class.
    /// </summary>
    /// <param name="timestamp">The time associated with the entry.</param>
    /// <param name="level">The severity of the log message.</param>
    /// <param name="content">The log content.</param>
    public LogEntry(DateTimeOffset timestamp, LogLevel level, ConsoleText content)
        : this(timestamp, level, content, source: null, exception: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogEntry"/> class with optional metadata values.
    /// </summary>
    /// <param name="timestamp">The time associated with the entry.</param>
    /// <param name="level">The severity of the log message.</param>
    /// <param name="content">The log content.</param>
    /// <param name="source">The optional source or category associated with the message.</param>
    /// <param name="exception">The optional exception associated with the message.</param>
    public LogEntry(DateTimeOffset timestamp, LogLevel level, ConsoleText content, string? source, Exception? exception = null)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Timestamp = timestamp;
        Level = level;
        Source = source;
        Exception = exception;
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

    /// <summary>
    /// Gets the optional source or category associated with the log message.
    /// </summary>
    public string? Source { get; }

    /// <summary>
    /// Gets the optional exception associated with the log message.
    /// </summary>
    /// <remarks>The exception object is retained with this entry until the entry is evicted or history is cleared.</remarks>
    public Exception? Exception { get; }
}
