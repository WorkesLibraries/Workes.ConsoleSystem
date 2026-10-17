using System;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Describes success output declared on a command definition.
/// </summary>
public sealed class CommandSuccessOutputDefinition
{
    internal CommandSuccessOutputDefinition(CommandOutput output)
    {
        Output = output ?? throw new ArgumentNullException(nameof(output));
        Kind = output.Kind;
    }

    /// <summary>
    /// Gets the output kind.
    /// </summary>
    public CommandOutputKind Kind { get; }

    /// <summary>
    /// Gets the declared success output.
    /// </summary>
    public CommandOutput Output { get; }
}
