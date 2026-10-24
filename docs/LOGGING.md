# Logging

Write logs directly through `ConsoleManager`. Each call appends a `LogEntry` to the same chronological history used by command input, output, failures, and custom entries.

```csharp
console.LogInformation("Connected to the server.");
console.LogWarning("The cache is nearly full.");
console.LogError(ConsoleText.Build("status")
    .Text("Could not load ")
    .Value("settings.json", "file-name")
    .Build());
```

For every level—Trace, Debug, Information, Warning, Error, and Critical—there are overloads for `string` and `ConsoleText`. The package records every call; it does not apply a minimum-level filter. The host can filter or render entries by `LogEntry.Level`.

`LogEntry.Message` provides plain text. `LogEntry.Content` preserves semantic segments for a UI that can use them. A log entry may also carry an optional `Source` string and an `Exception` object; the exception remains referenced until its history entry is evicted or history is cleared. There is no separate public logger object.

External logging integrations can create a `LogEntry` and pass it to `ConsoleManager.RecordEntry`. Each adapter owns its source-specific severity mapping and subscription lifetime. See the [adapter author guide](ADAPTER_AUTHOR_GUIDE.md) for the recommended pattern. For custom non-log entry types, see [Entries and semantic text](ENTRIES_AND_TEXT.md); to read or clear retained entries, see [History](HISTORY.md).
