using System;
using System.Collections.Generic;
using Workes.ConsoleSystem.Core;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Represents the result returned by a command handler.
/// </summary>
public sealed class CommandResult
{
    private readonly IReadOnlyList<CommandOutput> _outputs;

    /// <summary>
    /// Initializes a new successful command result with no output.
    /// </summary>
    public CommandResult()
        : this(null, Array.Empty<CommandOutput>())
    {
    }

    private CommandResult(ConsoleFailure? failure, IEnumerable<CommandOutput> outputs)
    {
        if (outputs is null)
        {
            throw new ArgumentNullException(nameof(outputs));
        }

        var outputList = new List<CommandOutput>();
        foreach (CommandOutput output in outputs)
        {
            if (output is null)
            {
                throw new ArgumentException("Command result outputs cannot contain null values.", nameof(outputs));
            }

            outputList.Add(output);
        }

        Failure = failure;
        _outputs = outputList.AsReadOnly();
    }

    /// <summary>
    /// Gets a value indicating whether the command completed successfully.
    /// </summary>
    public bool IsSuccess => Failure is null;

    /// <summary>
    /// Gets the structured command failure when the command did not complete successfully.
    /// </summary>
    public ConsoleFailure? Failure { get; }

    /// <summary>
    /// Gets the output produced by the command.
    /// </summary>
    public IReadOnlyList<CommandOutput> Outputs => _outputs;

    /// <summary>
    /// Creates a successful command result.
    /// </summary>
    /// <param name="outputs">The output produced by the command.</param>
    /// <returns>The created result.</returns>
    public static CommandResult Success(params CommandOutput[] outputs)
    {
        return new CommandResult(null, outputs);
    }

    /// <summary>
    /// Creates a failed command result.
    /// </summary>
    /// <param name="failure">The structured command failure.</param>
    /// <param name="outputs">Optional output produced by the command.</param>
    /// <returns>The created result.</returns>
    public static CommandResult Failed(ConsoleFailure failure, params CommandOutput[] outputs)
    {
        if (failure is null)
        {
            throw new ArgumentNullException(nameof(failure));
        }

        return new CommandResult(failure, outputs);
    }
}
