# Adapter author guide

This guide explains how to forward logs from another framework into Workes.ConsoleSystem. Platform-specific integrations belong in separate packages. The core package stays independent of Unity, Godot, Microsoft.Extensions.Logging, and UI frameworks.

## Forwarding a log

An adapter receives a framework-specific event or logger call, converts its severity to `Workes.ConsoleSystem.Logging.LogLevel`, creates a ConsoleSystem-owned `LogEntry`, then records it through the manager:

```csharp
using System;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Entries;
using Workes.ConsoleSystem.Logging;

static void ForwardLog(
    ConsoleManager console,
    LogLevel level,
    string message,
    string? source,
    Exception? exception)
{
    console.RecordEntry(new LogEntry(
        DateTimeOffset.UtcNow,
        level,
        message,
        source,
        exception));
}
```

`RecordEntry` appends to the same bounded, oldest-first history used by ConsoleSystem logs and command entries. History capacity, retention, and change notifications remain manager-owned. Existing manager logging methods are also available when an adapter does not need to attach source or exception metadata.

## Severity mapping

Severity types differ between providers, so the core package does not define a universal mapping or filtering policy. Each adapter should provide a sensible default mapping and a per-adapter customization point. For example, an adapter can accept a mapping callback or mapping table from the source framework's severity type to ConsoleSystem's `LogLevel`.

When a provider level is intentionally ignored or has no configured mapping, the adapter should skip the record. It should not silently assign an arbitrary fallback severity. The default mapping should work without users needing to learn or configure it.

## Source and exception metadata

`LogEntry.Source` can carry a useful source, category, logger name, or object name when the source framework provides one. `LogEntry.Exception` retains the original standard .NET exception object when available. Both are optional; do not invent values when the source does not provide meaningful metadata.

History retains the exception reference for the lifetime of the entry. Keep the manager's configured history capacity in mind when forwarding exceptions, particularly if exception objects contain large object graphs. ConsoleSystem does not serialize, clone, or format exceptions automatically; the host chooses how to display them.

## Subscription lifetime and threading

An adapter that subscribes to callbacks should expose an explicit, disposable attachment or provider lifetime. Detaching must unregister every callback and release references to the manager. Attaching should be opt-in and should not create global subscriptions as a side effect of package loading.

ConsoleSystem history writes are synchronized. History change notifications run synchronously on the thread performing the write, so a host that updates thread-affine UI must marshal that work itself. An adapter should document the source framework's callback threading behavior and should not claim that ConsoleSystem changes the callback thread.

## Keep package boundaries clear

- Put provider or engine references only in that provider's adapter package.
- Keep source-specific severity types and mapping options in the adapter package.
- Use `ConsoleManager.RecordEntry` and ConsoleSystem-owned `LogEntry`; do not expose ContentSystem types.
- Do not add engine rendering, UI controls, persistence, or a generic bridge API to the core package.
- Test attach/detach behavior, default mappings, custom mappings, ignored levels, metadata propagation, and callback threading expectations in the adapter package.

Project maintainers can use the [adapter package starter prompts](https://github.com/WorkesLibraries/Workes.ConsoleSystem/blob/main/docs/PROJECT/ADAPTER_PACKAGE_STARTER_PROMPTS.md) when creating the first integrations.
