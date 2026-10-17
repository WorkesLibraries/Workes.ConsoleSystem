using System;
using Workes.ConsoleSystem.History;
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Entries;
using Workes.ConsoleSystem.Internal;
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
    /// Records a console entry in the shared visible history.
    /// </summary>
    /// <param name="entry">The console entry to record.</param>
    public void RecordEntry(IConsoleEntry entry)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        ContentSequence.Add(new ContentEntryAdapter(entry));
    }

    /// <summary>
    /// Gets the internal store used by the console history facade.
    /// </summary>
    internal ContentSequenceManager ContentSequence { get; }
}
