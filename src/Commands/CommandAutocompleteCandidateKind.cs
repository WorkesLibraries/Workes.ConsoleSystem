namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Describes the kind of autocomplete candidate.
/// </summary>
public enum CommandAutocompleteCandidateKind
{
    /// <summary>
    /// A command path candidate.
    /// </summary>
    CommandPath,

    /// <summary>
    /// A positional argument value candidate.
    /// </summary>
    ArgumentValue,

    /// <summary>
    /// A flag name candidate.
    /// </summary>
    Flag,

    /// <summary>
    /// An option name candidate.
    /// </summary>
    Option,

    /// <summary>
    /// An option value candidate.
    /// </summary>
    OptionValue
}
