using System;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Describes command-specific input echo behavior.
/// </summary>
public sealed class CommandEchoInputDefinition
{
    internal CommandEchoInputDefinition(bool? echoInput, string? defaultStyleId)
    {
        if (defaultStyleId is not null && string.IsNullOrWhiteSpace(defaultStyleId))
        {
            throw new ArgumentException("Echo input style identifiers cannot be empty.", nameof(defaultStyleId));
        }

        EchoInput = echoInput;
        DefaultStyleId = defaultStyleId;
    }

    /// <summary>
    /// Gets the command-specific echo override, or null to use manager defaults.
    /// </summary>
    public bool? EchoInput { get; }

    /// <summary>
    /// Gets the optional default style identifier for echoed input.
    /// </summary>
    public string? DefaultStyleId { get; }
}
