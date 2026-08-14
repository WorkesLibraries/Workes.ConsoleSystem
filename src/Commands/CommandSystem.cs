using System;
using System.Collections.Generic;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Represents the permanent command registry and future parsing and execution infrastructure.
/// </summary>
/// <remarks>
/// Execution, permission checks, and autocomplete are intentionally not implemented yet.
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
