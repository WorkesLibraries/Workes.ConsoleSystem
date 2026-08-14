using System;
using Workes.ConsoleSystem.Entries;
using Workes.ConsoleSystem.History;
using Workes.ConsoleSystem.Presentation;

namespace Workes.ConsoleSystem.Logging;

/// <summary>
/// Provides a developer-facing facade for adding log entries to the shared console history.
/// </summary>
public sealed class ConsoleLog
{
    private readonly ConsoleHistory _history;

    internal ConsoleLog(ConsoleHistory history)
    {
        _history = history ?? throw new ArgumentNullException(nameof(history));
    }

    /// <summary>
    /// Adds a trace log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    internal void Trace(string message)
    {
        Write(LogLevel.Trace, message);
    }

    internal void Trace(ConsoleText content)
    {
        Write(LogLevel.Trace, content);
    }

    /// <summary>
    /// Adds a debug log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    internal void Debug(string message)
    {
        Write(LogLevel.Debug, message);
    }

    internal void Debug(ConsoleText content)
    {
        Write(LogLevel.Debug, content);
    }

    /// <summary>
    /// Adds an informational log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    internal void Information(string message)
    {
        Write(LogLevel.Information, message);
    }

    internal void Information(ConsoleText content)
    {
        Write(LogLevel.Information, content);
    }

    /// <summary>
    /// Adds a warning log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    internal void Warning(string message)
    {
        Write(LogLevel.Warning, message);
    }

    internal void Warning(ConsoleText content)
    {
        Write(LogLevel.Warning, content);
    }

    /// <summary>
    /// Adds an error log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    internal void Error(string message)
    {
        Write(LogLevel.Error, message);
    }

    internal void Error(ConsoleText content)
    {
        Write(LogLevel.Error, content);
    }

    /// <summary>
    /// Adds a critical log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    internal void Critical(string message)
    {
        Write(LogLevel.Critical, message);
    }

    internal void Critical(ConsoleText content)
    {
        Write(LogLevel.Critical, content);
    }

    /// <summary>
    /// Adds a log message with the specified severity to the shared console history.
    /// </summary>
    /// <param name="level">The severity of the log message.</param>
    /// <param name="message">The log message.</param>
    internal void Write(LogLevel level, string message)
    {
        Write(level, ConsoleText.Plain(message));
    }

    /// <summary>
    /// Adds log content with the specified severity to the shared console history.
    /// </summary>
    internal void Write(LogLevel level, ConsoleText content)
    {
        _history.Add(new LogEntry(DateTimeOffset.UtcNow, level, content));
    }
}
