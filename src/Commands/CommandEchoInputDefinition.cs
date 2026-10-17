namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Describes command-specific input echo behavior.
/// </summary>
public sealed class CommandEchoInputDefinition
{
    internal CommandEchoInputDefinition(bool? echoInput)
    {
        EchoInput = echoInput;
    }

    /// <summary>
    /// Gets the command-specific echo setting, or <see langword="null"/> to use manager defaults.
    /// </summary>
    public bool? EchoInput { get; }
}
