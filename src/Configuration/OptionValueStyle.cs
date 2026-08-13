namespace Workes.ConsoleSystem.Configuration;

/// <summary>
/// Defines how command options accept values.
/// </summary>
public enum OptionValueStyle
{
    /// <summary>
    /// Option values are separated from option names by whitespace, such as <c>--delay 10</c>.
    /// </summary>
    SpaceSeparated,

    /// <summary>
    /// Option values are separated from option names by an equals sign, such as <c>--delay=10</c>.
    /// </summary>
    EqualSeparated,

    /// <summary>
    /// Option values may use either whitespace or an equals sign.
    /// </summary>
    AnySeparated
}
