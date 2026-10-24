using System;
using System.Collections.Generic;

namespace Workes.ConsoleSystem.Commands;

/// <summary>Describes a configured, manager-registered help command.</summary>
public sealed class HelpCommandDefinition
{
    internal HelpCommandDefinition(
        string path,
        Func<CommandHelpContext, List<string>> formatter,
        bool searchPositionalEnabled,
        bool searchOptionEnabled,
        string? searchOptionName,
        IReadOnlyList<string> searchOptionAliases,
        Func<CommandHelpContext, List<string>>? searchFormatter,
        bool commandHelpPositionalEnabled,
        bool commandHelpOptionEnabled,
        string? commandHelpOptionName,
        IReadOnlyList<string> commandHelpOptionAliases,
        Func<CommandHelpContext, List<string>>? commandHelpFormatter,
        HelpCommandAccessPolicy accessPolicy,
        bool errorsEnabled,
        HelpCommandErrorFormatter unknownCommand,
        HelpCommandErrorFormatter noSearchResults,
        HelpCommandErrorFormatter permissionDenied,
        HelpCommandErrorFormatter unavailable,
        HelpCommandSearchMatcher searchMatcher,
        HelpCommandResultOrder resultOrder,
        IComparer<CommandDefinition>? resultComparer)
    {
        Path = path;
        Formatter = formatter;
        SearchPositionalEnabled = searchPositionalEnabled;
        SearchOptionEnabled = searchOptionEnabled;
        SearchOptionName = searchOptionName;
        SearchOptionAliases = searchOptionAliases;
        SearchFormatter = searchFormatter;
        CommandHelpPositionalEnabled = commandHelpPositionalEnabled;
        CommandHelpOptionEnabled = commandHelpOptionEnabled;
        CommandHelpOptionName = commandHelpOptionName;
        CommandHelpOptionAliases = commandHelpOptionAliases;
        CommandHelpFormatter = commandHelpFormatter;
        AccessPolicy = accessPolicy;
        ErrorsEnabled = errorsEnabled;
        UnknownCommand = unknownCommand;
        NoSearchResults = noSearchResults;
        PermissionDenied = permissionDenied;
        Unavailable = unavailable;
        SearchMatcher = searchMatcher;
        ResultOrder = resultOrder;
        ResultComparer = resultComparer;
    }

    /// <summary>Gets the registered command path.</summary>
    public string Path { get; }

    internal Func<CommandHelpContext, List<string>> Formatter { get; }
    internal bool SearchPositionalEnabled { get; }
    internal bool SearchOptionEnabled { get; }
    internal string? SearchOptionName { get; }
    internal IReadOnlyList<string> SearchOptionAliases { get; }
    internal Func<CommandHelpContext, List<string>>? SearchFormatter { get; }
    internal bool CommandHelpPositionalEnabled { get; }
    internal bool CommandHelpOptionEnabled { get; }
    internal string? CommandHelpOptionName { get; }
    internal IReadOnlyList<string> CommandHelpOptionAliases { get; }
    internal Func<CommandHelpContext, List<string>>? CommandHelpFormatter { get; }
    internal HelpCommandAccessPolicy AccessPolicy { get; }
    internal bool ErrorsEnabled { get; }
    internal HelpCommandErrorFormatter UnknownCommand { get; }
    internal HelpCommandErrorFormatter NoSearchResults { get; }
    internal HelpCommandErrorFormatter PermissionDenied { get; }
    internal HelpCommandErrorFormatter Unavailable { get; }
    internal HelpCommandSearchMatcher SearchMatcher { get; }
    internal HelpCommandResultOrder ResultOrder { get; }
    internal IComparer<CommandDefinition>? ResultComparer { get; }
}
