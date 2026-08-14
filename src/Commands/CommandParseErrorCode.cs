namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Identifies the stable reason a command input failed to parse.
/// </summary>
public enum CommandParseErrorCode
{
    /// <summary>
    /// The input was blank.
    /// </summary>
    EmptyInput,

    /// <summary>
    /// The command path did not match a registered command.
    /// </summary>
    UnknownCommand,

    /// <summary>
    /// A required positional argument was not provided.
    /// </summary>
    MissingArgument,

    /// <summary>
    /// Extra positional input was provided after all required arguments were bound.
    /// </summary>
    ExtraArgument,

    /// <summary>
    /// A flag or option name was not recognized.
    /// </summary>
    UnknownFlagOrOption,

    /// <summary>
    /// An option was provided without a value.
    /// </summary>
    MissingOptionValue,

    /// <summary>
    /// The same flag or option was provided more than once.
    /// </summary>
    DuplicateFlagOrOption,

    /// <summary>
    /// The option value separator did not match the configured option style.
    /// </summary>
    OptionValueSyntaxNotAllowed,

    /// <summary>
    /// A value could not be converted to its target type.
    /// </summary>
    InvalidValue,

    /// <summary>
    /// The input contained an unclosed quoted string.
    /// </summary>
    UnclosedQuote,

    /// <summary>
    /// The typed command state could not be created from bound values.
    /// </summary>
    StateBindingFailed
}
