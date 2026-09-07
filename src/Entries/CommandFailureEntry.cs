using System;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Presentation;

namespace Workes.ConsoleSystem.Entries;

/// <summary>
/// Represents a framework-created command failure in the shared console history.
/// </summary>
public sealed class CommandFailureEntry : IConsoleEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandFailureEntry"/> class.
    /// </summary>
    /// <param name="timestamp">The time associated with the entry.</param>
    /// <param name="failure">The structured command failure.</param>
    public CommandFailureEntry(DateTimeOffset timestamp, ConsoleFailure failure)
        : this(timestamp, failure, ConsoleText.Plain((failure ?? throw new ArgumentNullException(nameof(failure))).Message, "Error"))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandFailureEntry"/> class.
    /// </summary>
    /// <param name="timestamp">The time associated with the entry.</param>
    /// <param name="failure">The structured command failure.</param>
    /// <param name="content">The visible failure content.</param>
    public CommandFailureEntry(DateTimeOffset timestamp, ConsoleFailure failure, ConsoleText content)
    {
        Timestamp = timestamp;
        Failure = failure ?? throw new ArgumentNullException(nameof(failure));
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }

    /// <inheritdoc />
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the structured command failure.
    /// </summary>
    public ConsoleFailure Failure { get; }

    /// <summary>
    /// Gets the visible failure message.
    /// </summary>
    public string Message => Content.PlainText;

    /// <summary>
    /// Gets the semantic visible failure content.
    /// </summary>
    public ConsoleText Content { get; }
}
