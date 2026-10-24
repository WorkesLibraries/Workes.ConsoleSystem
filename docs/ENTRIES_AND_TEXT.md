# Entries and semantic text

All items in visible history implement the ConsoleSystem-owned `IConsoleEntry` interface, which exposes a `DateTimeOffset Timestamp`. Built-in entry types are:

- `LogEntry`: a log level, semantic text content, and optional source and exception metadata.
- `CommandInputEntry`: submitted command input when echo is enabled.
- `CommandOutputEntry`: inline or block output from command execution.
- `CommandFailureEntry`: structured command failure plus visible text.

The host decides how each entry is presented. ConsoleSystem does not interpret markup, choose colors, or depend on a UI or game engine.

## Semantic text

Use `ConsoleText.Plain` for literal text, or build a sequence of text and semantic value segments:

```csharp
ConsoleText content = ConsoleText.Build("notice")
    .Text("Saved ")
    .Value("settings.json", semanticId: "file-name", data: new { Path = "settings.json" })
    .Text(" successfully.")
    .Build();

console.LogInformation(content);
```

`PlainText` concatenates the segment text. `SemanticId` can identify a value's role; `Data` can carry an associated value. A segment's explicit semantic ID takes precedence over `ConsoleText.DefaultSemanticId`. The host may use these values for styling, interaction, localization, or accessibility, and may ignore them and display `PlainText`.

## Custom entries

Implement `IConsoleEntry` and submit the instance through `ConsoleManager.RecordEntry`:

```csharp
using System;
using Workes.ConsoleSystem.Entries;

public sealed class NetworkEventEntry : IConsoleEntry
{
    public NetworkEventEntry(DateTimeOffset timestamp, string peer)
    {
        Timestamp = timestamp;
        Peer = peer;
    }

    public DateTimeOffset Timestamp { get; }
    public string Peer { get; }
}

console.RecordEntry(new NetworkEventEntry(DateTimeOffset.UtcNow, "peer-17"));
```

History retains the same entry instance, so custom entries should be treated as immutable after recording. The public contracts are ConsoleSystem-owned; callers do not create or manage ContentSystem entries or records.

## Log metadata

`LogEntry.Source` is an optional source or category string. `LogEntry.Exception` is an optional standard .NET `Exception` reference. Both properties are `null` when omitted. History retains the exception reference for as long as it retains the entry; it does not serialize or clone exception objects.

Integrations that receive logs from another framework can populate these fields when the source provides that information. They can leave either field unset when it is unavailable or not useful. See the [adapter author guide](ADAPTER_AUTHOR_GUIDE.md).
