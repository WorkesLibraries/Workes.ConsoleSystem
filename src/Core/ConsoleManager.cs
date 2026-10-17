using System;
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Entries;
using Workes.ConsoleSystem.History;
using Workes.ConsoleSystem.Logging;
using Workes.ConsoleSystem.Presentation;
using Workes.ContentSystem.Core;

namespace Workes.ConsoleSystem.Core;

/// <summary>
/// Provides the root configuration and coordination point for one console instance.
/// </summary>
public sealed class ConsoleManager
{
    private readonly ConsoleManagerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleManager"/> class with default options.
    /// </summary>
    public ConsoleManager()
        : this(new ConsoleManagerOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleManager"/> class.
    /// </summary>
    /// <param name="options">The setup options to snapshot for this console instance.</param>
    public ConsoleManager(ConsoleManagerOptions options)
    {
        _options = ConsoleManagerOptions.CreateSnapshot(options);

        var structure = new ContentSequenceStructure(
            ContentOverflowPolicy.DropOldest(_options.History.ConsoleHistoryCapacity),
            ContentSequenceReadOrder.OldestFirst);
        ContentSequence = new ContentSequenceManager(structure);
        History = new ConsoleHistory(ContentSequence, _options.History.ConsoleHistoryCapacity);
        CommandHistory = new CommandHistory(
            _options.History.CommandHistoryCapacity,
            _options.History.CommandHistoryDuplicatePolicy);
        Log = new ConsoleLog(History);
    }

    /// <summary>
    /// Gets a detached snapshot of the resolved options for this console instance.
    /// </summary>
    public ConsoleManagerOptions Options => ConsoleManagerOptions.CreateSnapshot(_options);

    /// <summary>
    /// Gets the shared chronological console history.
    /// </summary>
    public ConsoleHistory History { get; }

    /// <summary>
    /// Gets the retained command input strings used by console UI navigation.
    /// </summary>
    public CommandHistory CommandHistory { get; }

    /// <summary>
    /// Gets the developer-facing logging facade used by this console manager.
    /// </summary>
    public ConsoleLog Log { get; }

    /// <summary>
    /// Records a console entry in the shared visible history.
    /// </summary>
    /// <param name="entry">The console entry to record.</param>
    public void RecordEntry(IConsoleEntry entry)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        History.RecordEntry(entry);
    }

    /// <summary>
    /// Records a submitted command input for console UI navigation.
    /// </summary>
    /// <param name="input">The original submitted command input.</param>
    /// <returns><see langword="true"/> when the input was retained; otherwise, <see langword="false"/>.</returns>
    public bool RecordCommandInput(string input) => CommandHistory.Add(input);

    /// <summary>
    /// Adds a trace log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void LogTrace(string message) => Log.Trace(message);

    /// <summary>
    /// Adds trace log content to the shared console history.
    /// </summary>
    /// <param name="content">The log content.</param>
    public void LogTrace(ConsoleText content) => Log.Trace(content);

    /// <summary>
    /// Adds a debug log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void LogDebug(string message) => Log.Debug(message);

    /// <summary>
    /// Adds debug log content to the shared console history.
    /// </summary>
    /// <param name="content">The log content.</param>
    public void LogDebug(ConsoleText content) => Log.Debug(content);

    /// <summary>
    /// Adds an informational log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void LogInformation(string message) => Log.Information(message);

    /// <summary>
    /// Adds informational log content to the shared console history.
    /// </summary>
    /// <param name="content">The log content.</param>
    public void LogInformation(ConsoleText content) => Log.Information(content);

    /// <summary>
    /// Adds a warning log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void LogWarning(string message) => Log.Warning(message);

    /// <summary>
    /// Adds warning log content to the shared console history.
    /// </summary>
    /// <param name="content">The log content.</param>
    public void LogWarning(ConsoleText content) => Log.Warning(content);

    /// <summary>
    /// Adds an error log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void LogError(string message) => Log.Error(message);

    /// <summary>
    /// Adds error log content to the shared console history.
    /// </summary>
    /// <param name="content">The log content.</param>
    public void LogError(ConsoleText content) => Log.Error(content);

    /// <summary>
    /// Adds a critical log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void LogCritical(string message) => Log.Critical(message);

    /// <summary>
    /// Adds critical log content to the shared console history.
    /// </summary>
    /// <param name="content">The log content.</param>
    public void LogCritical(ConsoleText content) => Log.Critical(content);

    /// <summary>
    /// Gets the internal store used by the console history facade.
    /// </summary>
    internal ContentSequenceManager ContentSequence { get; }
}
