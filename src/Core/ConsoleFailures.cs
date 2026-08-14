namespace Workes.ConsoleSystem.Core;

internal static class ConsoleFailures
{
    public static ConsoleFailure Unknown(string? message = null)
    {
        return Create(ConsoleFailureKind.Unknown, ConsoleFailureCodes.Unknown, message);
    }

    public static ConsoleFailure Validation(string? message = null)
    {
        return Create(ConsoleFailureKind.Validation, ConsoleFailureCodes.ValidationRejected, message);
    }

    public static ConsoleFailure Configuration(string? message = null)
    {
        return Create(ConsoleFailureKind.Configuration, ConsoleFailureCodes.ConfigurationRejected, message);
    }

    public static ConsoleFailure CommandDefinition(string? message = null)
    {
        return Create(ConsoleFailureKind.CommandDefinition, ConsoleFailureCodes.CommandDefinitionInvalid, message);
    }

    public static ConsoleFailure CommandRegistration(string? message = null)
    {
        return Create(ConsoleFailureKind.CommandRegistration, ConsoleFailureCodes.CommandRegistrationRejected, message);
    }

    public static ConsoleFailure CommandParsing(string code, string? message = null, string? source = null)
    {
        return Create(ConsoleFailureKind.CommandParsing, code, message, source: source);
    }

    public static ConsoleFailure CommandBinding(string code, string? message = null, string? source = null)
    {
        return Create(ConsoleFailureKind.CommandBinding, code, message, source: source);
    }

    public static ConsoleFailure CommandConstraint(string? message = null)
    {
        return Create(ConsoleFailureKind.CommandConstraint, ConsoleFailureCodes.CommandConstraintRejected, message);
    }

    public static ConsoleFailure CommandExecution(string? message = null)
    {
        return Create(ConsoleFailureKind.CommandExecution, ConsoleFailureCodes.CommandExecutionRejected, message);
    }

    public static ConsoleFailure Extension(string? message = null)
    {
        return Create(ConsoleFailureKind.Extension, ConsoleFailureCodes.ExtensionRejected, message);
    }

    private static ConsoleFailure Create(
        ConsoleFailureKind kind,
        string code,
        string? message,
        string? component = null,
        string? source = null,
        ConsoleFailure? cause = null)
    {
        return ConsoleFailure.Create(
            kind,
            code,
            string.IsNullOrWhiteSpace(message) ? "Console operation failed." : message!,
            component,
            source,
            cause);
    }
}
