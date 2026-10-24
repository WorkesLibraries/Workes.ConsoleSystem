using System;
using System.Collections.Generic;
using Workes.ConsoleSystem.Core;

namespace Workes.ConsoleSystem.Commands;

/// <summary>Identifies a help lookup outcome that can have a configured message.</summary>
public enum HelpCommandErrorKind
{
    /// <summary>A requested command path is not registered or is hidden by access policy.</summary>
    UnknownCommand,
    /// <summary>A search has no visible results.</summary>
    NoSearchResults,
    /// <summary>The caller lacks permission for a command.</summary>
    PermissionDenied,
    /// <summary>A command is currently unavailable.</summary>
    Unavailable
}

/// <summary>Context for one configurable help lookup message.</summary>
public sealed class HelpCommandErrorContext
{
    internal HelpCommandErrorContext(HelpCommandErrorKind kind, string? query, CommandDefinition? command, ConsoleFailure? failure, IReadOnlyList<CommandDefinition> matches)
    { Kind = kind; Query = query; Command = command; AccessFailure = failure; MatchedCommands = matches; }
    /// <summary>The lookup outcome.</summary>
    public HelpCommandErrorKind Kind { get; }
    /// <summary>The supplied query, when present.</summary>
    public string? Query { get; }
    /// <summary>The blocked or requested command, when known.</summary>
    public CommandDefinition? Command { get; }
    /// <summary>The access failure for blocked commands.</summary>
    public ConsoleFailure? AccessFailure { get; }
    /// <summary>The registered commands matched before access filtering.</summary>
    public IReadOnlyList<CommandDefinition> MatchedCommands { get; }
}

/// <summary>Produces an optional help error line; null or empty suppresses output.</summary>
public delegate string? HelpCommandErrorFormatter(HelpCommandErrorContext context);

/// <summary>Creates the built-in lookup error text.</summary>
public static class HelpCommandErrorMessages
{
    /// <summary>Default text for a missing or hidden command.</summary>
    public static HelpCommandErrorFormatter UnknownCommand { get; } = c => $"No command named '{c.Query}' is available.";
    /// <summary>Default text when no visible search result is available.</summary>
    public static HelpCommandErrorFormatter NoSearchResults { get; } = c => $"No commands matched '{c.Query}'.";
    /// <summary>Default text for a permission-denied command.</summary>
    public static HelpCommandErrorFormatter PermissionDenied { get; } = c => $"You do not have access to command '{c.Command?.Path}'.";
    /// <summary>Default text for an unavailable command.</summary>
    public static HelpCommandErrorFormatter Unavailable { get; } = c => $"Command '{c.Command?.Path}' cannot be used right now.";
}

/// <summary>How help results are ordered.</summary>
public enum HelpCommandResultOrder
{
    /// <summary>Keep the manager's command registration order.</summary>
    Registration,
    /// <summary>Sort paths alphabetically, case-insensitively.</summary>
    Alphabetical
}

/// <summary>Matches a registered command against a help search term.</summary>
public delegate bool HelpCommandSearchMatcher(CommandDefinition command, string searchTerm, bool isCaseSensitive);

/// <summary>Built-in help search matching policies.</summary>
public static class HelpCommandSearchMatchers
{
    /// <summary>Matches a substring of the command path, respecting manager case sensitivity.</summary>
    public static HelpCommandSearchMatcher PathSubstring { get; } = (command, term, sensitive) =>
        command.Path.IndexOf(term, sensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase) >= 0;
}
