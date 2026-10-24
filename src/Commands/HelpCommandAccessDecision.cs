using System;
using System.Collections.Generic;
using System.Linq;

namespace Workes.ConsoleSystem.Commands;

/// <summary>Describes how a blocked command is handled by a help command.</summary>
public sealed class HelpCommandAccessDecision
{
    private HelpCommandAccessDecision(HelpCommandAccessDecisionKind kind, IReadOnlyList<string> responseLines)
    {
        Kind = kind;
        ResponseLines = responseLines;
    }

    /// <summary>Gets the selected blocked-command behavior.</summary>
    public HelpCommandAccessDecisionKind Kind { get; }

    /// <summary>Gets response lines when <see cref="Kind"/> is <see cref="HelpCommandAccessDecisionKind.Respond"/>.</summary>
    public IReadOnlyList<string> ResponseLines { get; }

    /// <summary>Hides the blocked command and treats direct requests as though it did not exist.</summary>
    public static HelpCommandAccessDecision Hide() => new HelpCommandAccessDecision(
        HelpCommandAccessDecisionKind.Hide,
        Array.Empty<string>());

    /// <summary>Includes the blocked command and lets its configured formatter produce the output.</summary>
    public static HelpCommandAccessDecision Show() => new HelpCommandAccessDecision(
        HelpCommandAccessDecisionKind.Show,
        Array.Empty<string>());

    /// <summary>Returns custom lines instead of formatted command details.</summary>
    public static HelpCommandAccessDecision Respond(IEnumerable<string> lines)
    {
        if (lines is null)
        {
            throw new ArgumentNullException(nameof(lines));
        }

        string[] copy = lines.Select(line => line ?? throw new ArgumentException("Response lines cannot contain null.", nameof(lines))).ToArray();
        return new HelpCommandAccessDecision(HelpCommandAccessDecisionKind.Respond, Array.AsReadOnly(copy));
    }
}

/// <summary>Specifies how a blocked command is handled for help output.</summary>
public enum HelpCommandAccessDecisionKind
{
    /// <summary>Do not include the command in help output.</summary>
    Hide,

    /// <summary>Include the command and use the configured help formatter.</summary>
    Show,

    /// <summary>Use the decision's response lines instead of the command formatter.</summary>
    Respond
}

/// <summary>Receives help context for commands that fail a permission or availability check.</summary>
public delegate HelpCommandAccessDecision HelpCommandAccessPolicy(CommandHelpAccessContext context);

/// <summary>Built-in policies for handling blocked commands.</summary>
public static class HelpCommandAccessPolicies
{
    /// <summary>Hides blocked commands. This is the default policy.</summary>
    public static HelpCommandAccessPolicy HideBlocked { get; } = _ => HelpCommandAccessDecision.Hide();

    /// <summary>Shows blocked commands using the configured formatters.</summary>
    public static HelpCommandAccessPolicy ShowBlocked { get; } = _ => HelpCommandAccessDecision.Show();

    /// <summary>Returns a concise permission or availability response for each blocked command.</summary>
    public static HelpCommandAccessPolicy ExplainBlocked { get; } = context =>
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        string message = context.AccessFailure?.Kind switch
        {
            Workes.ConsoleSystem.Core.ConsoleFailureKind.CommandAuthorization => $"You do not have access to command '{context.Command.Path}'.",
            Workes.ConsoleSystem.Core.ConsoleFailureKind.CommandAvailability => $"Command '{context.Command.Path}' cannot be used right now.",
            _ => $"Command '{context.Command.Path}' is unavailable."
        };
        return HelpCommandAccessDecision.Respond(new[] { message });
    };
}
