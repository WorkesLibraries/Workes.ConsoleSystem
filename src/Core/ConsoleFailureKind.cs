namespace Workes.ConsoleSystem.Core;

/// <summary>
/// Describes the broad subsystem or reason category for an expected console-system failure.
/// </summary>
public enum ConsoleFailureKind
{
    /// <summary>
    /// The failure could not be classified more precisely.
    /// </summary>
    Unknown,

    /// <summary>
    /// General validation rejected the request.
    /// </summary>
    Validation,

    /// <summary>
    /// Configuration validation rejected the request.
    /// </summary>
    Configuration,

    /// <summary>
    /// Command definition validation rejected the request.
    /// </summary>
    CommandDefinition,

    /// <summary>
    /// Command registration rejected the request.
    /// </summary>
    CommandRegistration,

    /// <summary>
    /// Command input parsing rejected the request.
    /// </summary>
    CommandParsing,

    /// <summary>
    /// Command value binding rejected the request.
    /// </summary>
    CommandBinding,

    /// <summary>
    /// Command constraint validation rejected the request.
    /// </summary>
    CommandConstraint,

    /// <summary>
    /// Command execution rejected or failed the request.
    /// </summary>
    CommandExecution,

    /// <summary>
    /// Extension-provided code rejected or failed inside an expected validation path.
    /// </summary>
    Extension
}
