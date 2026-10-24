using System;
using System.Collections.Generic;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Configuration;

namespace Workes.ConsoleSystem.Commands;

/// <summary>Provides command and access information to help formatters and access policies.</summary>
public sealed class CommandHelpContext
{
    internal CommandHelpContext(
        CommandDefinition command,
        string? searchTerm,
        ConsoleFailure? accessFailure,
        IReadOnlyList<CommandDefinition> matchedCommands,
        string? syntax,
        CommandParsingOptions parsingOptions)
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
        SearchTerm = searchTerm;
        AccessFailure = accessFailure;
        MatchedCommands = matchedCommands ?? throw new ArgumentNullException(nameof(matchedCommands));
        Syntax = syntax;
        OptionPrefix = parsingOptions.FlagAndOptionPrefix;
    }

    /// <summary>Gets the command currently being considered for help output.</summary>
    public CommandDefinition Command { get; }

    /// <summary>Gets the search term, or <see langword="null"/> for the overview.</summary>
    public string? SearchTerm { get; }

    /// <summary>Gets whether the command passes this manager's permission and availability checks.</summary>
    public bool IsAccessible => AccessFailure is null;

    /// <summary>Gets the access failure for a blocked command, or <see langword="null"/> when accessible.</summary>
    public ConsoleFailure? AccessFailure { get; }

    /// <summary>Gets the read-only command set matching the current overview or search.</summary>
    public IReadOnlyList<CommandDefinition> MatchedCommands { get; }

    /// <summary>Gets command syntax for the active manager, or null when syntax is absent.</summary>
    public string? Syntax { get; }

    /// <summary>Gets the option prefix used by the active manager.</summary>
    public string OptionPrefix { get; }
}

/// <summary>Provides access-check information to a help command's blocked-command policy.</summary>
public sealed class CommandHelpAccessContext
{
    internal CommandHelpAccessContext(
        CommandDefinition command,
        string? searchTerm,
        ConsoleFailure accessFailure,
        IReadOnlyList<CommandDefinition> matchedCommands)
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
        SearchTerm = searchTerm;
        AccessFailure = accessFailure ?? throw new ArgumentNullException(nameof(accessFailure));
        MatchedCommands = matchedCommands ?? throw new ArgumentNullException(nameof(matchedCommands));
    }

    /// <summary>Gets the command that failed an access check.</summary>
    public CommandDefinition Command { get; }

    /// <summary>Gets the search term, or <see langword="null"/> for the overview.</summary>
    public string? SearchTerm { get; }

    /// <summary>Gets the permission, availability, or extension failure from the manager's access check.</summary>
    public ConsoleFailure AccessFailure { get; }

    /// <summary>Gets the read-only command set matching this overview or search.</summary>
    public IReadOnlyList<CommandDefinition> MatchedCommands { get; }
}
