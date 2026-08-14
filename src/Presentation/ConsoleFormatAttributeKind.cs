namespace Workes.ConsoleSystem.Presentation;

/// <summary>
/// Describes how a formatting attribute carries data.
/// </summary>
public enum ConsoleFormatAttributeKind
{
    /// <summary>
    /// The attribute is either present or absent.
    /// </summary>
    Flag,

    /// <summary>
    /// The attribute carries a value.
    /// </summary>
    Value
}
