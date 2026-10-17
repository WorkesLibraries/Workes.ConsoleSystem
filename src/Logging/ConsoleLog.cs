using System;
using Workes.ConsoleSystem.Entries;
using Workes.ConsoleSystem.History;
using Workes.ConsoleSystem.Presentation;

namespace Workes.ConsoleSystem.Logging;

/// <summary>
/// Provides the manager-owned logging implementation for the shared console history.
/// </summary>
public sealed class ConsoleLog
{
    private readonly ConsoleHistory _history;

    internal ConsoleLog(ConsoleHistory history)
    {
        _history = history ?? throw new ArgumentNullException(nameof(history));
    }

    internal void Trace(string message) => Write(LogLevel.Trace, message);

    internal void Trace(ConsoleText content) => Write(LogLevel.Trace, content);

    internal void Debug(string message) => Write(LogLevel.Debug, message);

    internal void Debug(ConsoleText content) => Write(LogLevel.Debug, content);

    internal void Information(string message) => Write(LogLevel.Information, message);

    internal void Information(ConsoleText content) => Write(LogLevel.Information, content);

    internal void Warning(string message) => Write(LogLevel.Warning, message);

    internal void Warning(ConsoleText content) => Write(LogLevel.Warning, content);

    internal void Error(string message) => Write(LogLevel.Error, message);

    internal void Error(ConsoleText content) => Write(LogLevel.Error, content);

    internal void Critical(string message) => Write(LogLevel.Critical, message);

    internal void Critical(ConsoleText content) => Write(LogLevel.Critical, content);

    internal void Write(LogLevel level, string message)
    {
        Write(level, ConsoleText.Plain(message));
    }

    internal void Write(LogLevel level, ConsoleText content)
    {
        _history.RecordEntry(new LogEntry(DateTimeOffset.UtcNow, level, content));
    }
}
