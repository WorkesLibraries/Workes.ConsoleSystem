using System;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Describes settled command metadata.
/// </summary>
public sealed class CommandDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandDefinition"/> class.
    /// </summary>
    /// <param name="path">The complete command path.</param>
    /// <param name="description">A short description of the command.</param>
    public CommandDefinition(string path, string description)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }

    /// <summary>
    /// Gets the complete command path.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets a short description of the command.
    /// </summary>
    public string Description { get; }
}
