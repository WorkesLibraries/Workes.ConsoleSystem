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

`LogEntry.Message` provides plain text. `LogEntry.Content` preserves semantic segments for a UI that can use them. There is no separate public logger object. For custom entry types, see [Entries and semantic text](ENTRIES_AND_TEXT.md); to read or clear retained log entries, see [History](HISTORY.md).
