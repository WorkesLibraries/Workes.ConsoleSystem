using System;
using System.Collections.Generic;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Describes a required positional command argument.
/// </summary>
public sealed class CommandArgumentDefinition : CommandMemberDefinition
{
    internal CommandArgumentDefinition(string propertyName, Type valueType, string name, string description)
        : base(propertyName, valueType, name, Array.Empty<string>(), description)
    {
    }
}
