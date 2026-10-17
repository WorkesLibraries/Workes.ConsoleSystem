using System;
using System.Collections.Generic;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Describes a boolean command flag.
/// </summary>
public sealed class CommandFlagDefinition : CommandMemberDefinition
{
    internal CommandFlagDefinition(
        string propertyName,
        string name,
        IReadOnlyList<string> aliases,
        string description)
        : base(propertyName, typeof(bool), name, aliases, description)
    {
    }
}
