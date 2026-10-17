using System;
using Workes.ConsoleSystem.Entries;
using Workes.ContentSystem.Core;

namespace Workes.ConsoleSystem.Internal;

/// <summary>
/// Adapts a console-owned entry to the internal ContentSystem storage contract.
/// </summary>
internal sealed class ContentEntryAdapter : IContentEntry
{
    internal ContentEntryAdapter(IConsoleEntry consoleEntry)
    {
        ConsoleEntry = consoleEntry ?? throw new ArgumentNullException(nameof(consoleEntry));
        Timestamp = consoleEntry.Timestamp;
    }

    internal IConsoleEntry ConsoleEntry { get; }

    public DateTimeOffset Timestamp { get; }
}
