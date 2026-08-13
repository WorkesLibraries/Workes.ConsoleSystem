using System;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Describes command constraint metadata.
/// </summary>
public sealed class CommandConstraintDefinition
{
    internal CommandConstraintDefinition(string name, string message)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }

    /// <summary>
    /// Gets the constraint name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the message shown when the constraint fails.
    /// </summary>
    public string Message { get; }
}
