namespace Workes.ConsoleSystem.Core;

/// <summary>
/// Exception thrown by expected-success console operations when validation rejects the operation.
/// </summary>
public sealed class ConsoleOperationException : ConsoleSystemException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleOperationException"/> class.
    /// </summary>
    /// <param name="failure">The structured operation failure.</param>
    public ConsoleOperationException(ConsoleFailure failure)
        : base(failure)
    {
    }
}
