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
        HelpCommandAccessPolicy accessPolicy)
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
}
