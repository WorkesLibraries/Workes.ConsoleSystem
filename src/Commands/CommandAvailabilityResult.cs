using System;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Represents whether a command is available, with an optional explanation when it is unavailable.
/// </summary>
public sealed class CommandAvailabilityResult
{
    private CommandAvailabilityResult(bool isAvailable, string? reason)
    {
        IsAvailable = isAvailable;
        Reason = reason;
    }

    /// <summary>Gets whether the command is available.</summary>
    public bool IsAvailable { get; }

    /// <summary>Gets the optional explanation when the command is unavailable.</summary>
    public string? Reason { get; }

    /// <summary>Creates an available result.</summary>
    public static CommandAvailabilityResult Available() => new CommandAvailabilityResult(true, null);

    /// <summary>Creates an unavailable result with a user-facing reason.</summary>
    /// <param name="reason">The reason the command cannot currently run.</param>
    public static CommandAvailabilityResult Unavailable(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("An unavailable result requires a reason.", nameof(reason));
        }

        return new CommandAvailabilityResult(false, reason);
    }
}
