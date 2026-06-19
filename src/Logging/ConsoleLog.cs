using System;
using Workes.ConsoleSystem.Entries;
using Workes.ConsoleSystem.History;

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
    public void Trace(string message)
    {
        Write(LogLevel.Trace, message);
    }

    /// <summary>
    /// Adds a debug log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void Debug(string message)
    {
        Write(LogLevel.Debug, message);
    }

    /// <summary>
    /// Adds an informational log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void Information(string message)
    {
        Write(LogLevel.Information, message);
    }

    /// <summary>
    /// Adds a warning log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void Warning(string message)
    {
        Write(LogLevel.Warning, message);
    }

    /// <summary>
    /// Adds an error log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void Error(string message)
    {
        Write(LogLevel.Error, message);
    }

    /// <summary>
    /// Adds a critical log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void Critical(string message)
    {
        Write(LogLevel.Critical, message);
    }

    /// <summary>
    /// Adds a log message with the specified severity to the shared console history.
    /// </summary>
    /// <param name="level">The severity of the log message.</param>
    /// <param name="message">The log message.</param>
    public void Write(LogLevel level, string message)
    {
        _history.Add(new LogEntry(DateTimeOffset.UtcNow, level, message));
    }
}
