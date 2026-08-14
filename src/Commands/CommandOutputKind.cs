namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Describes the structural shape of command output.
/// </summary>
public enum CommandOutputKind
{
    /// <summary>
    /// Output intended to be rendered as inline text.
    /// </summary>
    Inline = 0,

    /// <summary>
    /// Output intended to be rendered as a block, often with multiple lines.
    /// </summary>
    Block = 1
}
