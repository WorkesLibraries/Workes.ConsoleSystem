using System;

namespace Workes.ConsoleSystem.Core;

/// <summary>
/// Base exception for expected-success console-system wrappers that fail due to domain rejection.
/// </summary>
public class ConsoleSystemException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleSystemException"/> class.
    /// </summary>
    /// <param name="failure">The structured failure.</param>
    public ConsoleSystemException(ConsoleFailure failure)
        : base((failure ?? throw new ArgumentNullException(nameof(failure))).Message)
    {
        Failure = failure;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleSystemException"/> class.
    /// </summary>
    /// <param name="failure">The structured failure.</param>
    /// <param name="innerException">The exception that caused this failure.</param>
    public ConsoleSystemException(ConsoleFailure failure, Exception? innerException)
        : base((failure ?? throw new ArgumentNullException(nameof(failure))).Message, innerException)
    {
        Failure = failure;
    }

    /// <summary>
    /// Gets the structured failure.
    /// </summary>
    public ConsoleFailure Failure { get; }
}
