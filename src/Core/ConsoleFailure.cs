using System;

namespace Workes.ConsoleSystem.Core;

/// <summary>
/// Structured, non-exception result data for an expected console-system rejection.
/// </summary>
public sealed class ConsoleFailure : IEquatable<ConsoleFailure>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleFailure"/> class.
    /// </summary>
    /// <param name="kind">The broad failure category.</param>
    /// <param name="code">The stable machine-readable failure code.</param>
    /// <param name="message">The human-readable failure message.</param>
    /// <param name="component">The optional component that produced or wrapped the failure.</param>
    /// <param name="source">The optional stable source identifier.</param>
    /// <param name="cause">The optional nested lower-level failure.</param>
    public ConsoleFailure(
        ConsoleFailureKind kind,
        string code,
        string message,
        string? component = null,
        string? source = null,
        ConsoleFailure? cause = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Failure code cannot be null or empty.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Failure message cannot be null or empty.", nameof(message));
        }

        Kind = kind;
        Code = code;
        Message = message;
        Component = component;
        Source = source;
        Cause = cause;
    }

    /// <summary>
    /// Gets the broad failure category.
    /// </summary>
    public ConsoleFailureKind Kind { get; }

    /// <summary>
    /// Gets the stable machine-readable failure code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the human-readable failure message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the optional component that produced or wrapped the failure.
    /// </summary>
    public string? Component { get; }

    /// <summary>
    /// Gets the optional stable source identifier.
    /// </summary>
    public string? Source { get; }

    /// <summary>
    /// Gets the optional nested lower-level failure.
    /// </summary>
    public ConsoleFailure? Cause { get; }

    /// <summary>
    /// Creates a structured console-system failure.
    /// </summary>
    /// <param name="kind">The broad failure category.</param>
    /// <param name="code">The stable machine-readable failure code.</param>
    /// <param name="message">The human-readable failure message.</param>
    /// <param name="component">The optional component that produced or wrapped the failure.</param>
    /// <param name="source">The optional stable source identifier.</param>
    /// <param name="cause">The optional nested lower-level failure.</param>
    /// <returns>The created failure.</returns>
    public static ConsoleFailure Create(
        ConsoleFailureKind kind,
        string code,
        string message,
        string? component = null,
        string? source = null,
        ConsoleFailure? cause = null)
    {
        return new ConsoleFailure(kind, code, message, component, source, cause);
    }

    /// <summary>
    /// Creates a higher-level failure that preserves a lower-level cause.
    /// </summary>
    /// <param name="kind">The broad failure category.</param>
    /// <param name="code">The stable machine-readable failure code.</param>
    /// <param name="message">The human-readable wrapping message.</param>
    /// <param name="cause">The optional lower-level cause.</param>
    /// <param name="component">The optional component that wrapped the failure.</param>
    /// <param name="source">The optional stable source identifier.</param>
    /// <returns>The wrapped failure.</returns>
    public static ConsoleFailure Wrap(
        ConsoleFailureKind kind,
        string code,
        string message,
        ConsoleFailure? cause,
        string? component = null,
        string? source = null)
    {
        return new ConsoleFailure(
            kind,
            code,
            cause is null ? message : $"{message}: {cause.Message}",
            component,
            source,
            cause);
    }

    /// <summary>
    /// Converts an exception from an expected extension path into a structured failure.
    /// </summary>
    /// <param name="exception">The exception to convert.</param>
    /// <param name="kind">The failure category to use for non-console exceptions.</param>
    /// <param name="code">The optional stable code to use for non-console exceptions.</param>
    /// <returns>The structured failure.</returns>
    public static ConsoleFailure FromException(
        Exception exception,
        ConsoleFailureKind kind = ConsoleFailureKind.Extension,
        string? code = null)
    {
        if (exception is null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        if (exception is ConsoleSystemException consoleException)
        {
            return consoleException.Failure;
        }

        return new ConsoleFailure(kind, code ?? ConsoleFailureCodes.ExtensionRejected, exception.Message);
    }

    /// <summary>
    /// Returns the human-readable message with concise cause context when present.
    /// </summary>
    /// <returns>The displayable failure string.</returns>
    public override string ToString()
    {
        return Cause is null ? Message : $"{Message} Cause: {Cause.Message}";
    }

    /// <summary>
    /// Determines whether this failure is structurally equal to another failure.
    /// </summary>
    /// <param name="other">The other failure.</param>
    /// <returns><see langword="true"/> when all failure fields are equal.</returns>
    public bool Equals(ConsoleFailure? other)
    {
        if (other is null)
        {
            return false;
        }

        return Kind == other.Kind &&
               Code == other.Code &&
               Message == other.Message &&
               Component == other.Component &&
               Source == other.Source &&
               Equals(Cause, other.Cause);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return Equals(obj as ConsoleFailure);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (int)Kind;
            hash = (hash * 397) ^ Code.GetHashCode();
            hash = (hash * 397) ^ Message.GetHashCode();
            hash = (hash * 397) ^ (Component?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ (Source?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ (Cause?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
