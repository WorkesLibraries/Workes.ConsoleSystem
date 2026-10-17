using System;
using System.Collections.Generic;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Represents the permanent command registry used by parsing, validation, autocomplete, and execution.
/// </summary>
/// <remarks>
/// Registration is owned by <see cref="Core.ConsoleManager"/>.
/// </remarks>
public sealed class CommandSystem
{
    private readonly List<CommandDefinition> _definitions = new List<CommandDefinition>();

    /// <summary>
    /// Gets the registered command definitions.
    /// </summary>
    public IReadOnlyList<CommandDefinition> Definitions => _definitions.AsReadOnly();

    internal void Add(CommandDefinition definition)
    {
        _definitions.Add(definition ?? throw new ArgumentNullException(nameof(definition)));
    }
}
