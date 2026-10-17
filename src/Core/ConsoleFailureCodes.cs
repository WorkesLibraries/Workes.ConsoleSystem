namespace Workes.ConsoleSystem.Core;

/// <summary>
/// Stable package-owned failure codes. Callers should branch on codes or kinds rather than display messages.
/// </summary>
public static class ConsoleFailureCodes
{
    /// <summary>
    /// Prefix reserved for built-in package failures.
    /// </summary>
    public const string PackagePrefix = "workes.console.";

    /// <summary>
    /// Unknown or unclassified failure.
    /// </summary>
    public const string Unknown = PackagePrefix + "unknown";

    /// <summary>
    /// General validation rejection.
    /// </summary>
    public const string ValidationRejected = PackagePrefix + "validation.rejected";

    /// <summary>
    /// Configuration rejection.
    /// </summary>
    public const string ConfigurationRejected = PackagePrefix + "configuration.rejected";

    /// <summary>
    /// Command definition rejection.
    /// </summary>
    public const string CommandDefinitionInvalid = PackagePrefix + "command.definition.invalid";

    /// <summary>
    /// Command registration rejection.
    /// </summary>
    public const string CommandRegistrationRejected = PackagePrefix + "command.registration.rejected";

    /// <summary>
    /// General command parsing rejection.
    /// </summary>
    public const string CommandParsingRejected = PackagePrefix + "command.parsing.rejected";

    /// <summary>
    /// Command input is blank.
    /// </summary>
    public const string CommandInputEmpty = PackagePrefix + "command.input.empty";

    /// <summary>
    /// Command path did not match a registered command.
    /// </summary>
    public const string CommandUnknown = PackagePrefix + "command.unknown";

    /// <summary>
    /// A required positional command argument is missing.
    /// </summary>
    public const string CommandArgumentMissing = PackagePrefix + "command.argument.missing";

    /// <summary>
    /// Extra positional command input was provided.
    /// </summary>
    public const string CommandArgumentUnexpected = PackagePrefix + "command.argument.unexpected";

    /// <summary>
    /// A flag or option name was not recognized.
    /// </summary>
    public const string CommandMemberUnknown = PackagePrefix + "command.member.unknown";

    /// <summary>
    /// An option was provided without a value.
    /// </summary>
    public const string CommandOptionValueMissing = PackagePrefix + "command.option.value.missing";

    /// <summary>
    /// A flag or option was provided more than once.
    /// </summary>
    public const string CommandMemberDuplicate = PackagePrefix + "command.member.duplicate";

    /// <summary>
    /// The option value syntax did not match configured parser settings.
    /// </summary>
    public const string CommandOptionValueSyntaxInvalid = PackagePrefix + "command.option.value.syntax.invalid";

    /// <summary>
    /// A command value could not be converted to its target type.
    /// </summary>
    public const string CommandValueInvalid = PackagePrefix + "command.value.invalid";

    /// <summary>
    /// Command input contains an unclosed quote.
    /// </summary>
    public const string CommandQuoteUnclosed = PackagePrefix + "command.quote.unclosed";

    /// <summary>
    /// Bound values could not create typed command state.
    /// </summary>
    public const string CommandStateBindingFailed = PackagePrefix + "command.state.binding.failed";

    /// <summary>
    /// Command constraint validation rejection.
    /// </summary>
    public const string CommandConstraintRejected = PackagePrefix + "command.constraint.rejected";

    /// <summary>
    /// Command execution rejection.
    /// </summary>
    public const string CommandExecutionRejected = PackagePrefix + "command.execution.rejected";

    /// <summary>
    /// Extension contract rejection.
    /// </summary>
    public const string ExtensionRejected = PackagePrefix + "extension.rejected";
}
