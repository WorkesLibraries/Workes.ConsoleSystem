using System;
using System.Collections.Generic;
using System.Linq;

namespace Workes.ConsoleSystem.Commands;

/// <summary>Builds an opt-in help command configuration for registration with a <see cref="Core.ConsoleManager"/>.</summary>
public sealed class HelpCommandBuilder
{
    private readonly string _path;
    private Func<CommandHelpContext, List<string>>? _formatter;
    private bool _searchPositionalEnabled;
    private bool _searchOptionEnabled;
    private string? _searchOptionName;
    private readonly List<string> _searchOptionAliases = new List<string>();
    private Func<CommandHelpContext, List<string>>? _searchFormatter;
    private bool _commandHelpPositionalEnabled;
    private bool _commandHelpOptionEnabled;
    private string? _commandHelpOptionName;
    private readonly List<string> _commandHelpOptionAliases = new List<string>();
    private Func<CommandHelpContext, List<string>>? _commandHelpFormatter;
    private HelpCommandAccessPolicy _accessPolicy = HelpCommandAccessPolicies.HideBlocked;

    /// <summary>Creates a help command builder.</summary>
    /// <param name="path">The help command path. Defaults to <c>help</c>.</param>
    public HelpCommandBuilder(string path = "help")
    {
        _path = path;
    }

    /// <summary>Sets the shared formatter used by the overview and as the fallback for specialized formatters.</summary>
    public HelpCommandBuilder FormatWith(Func<CommandHelpContext, List<string>> formatter)
    {
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        return this;
    }

    /// <summary>Enables searching the overview with one optional positional query.</summary>
    /// <param name="formatter">An optional search formatter; the shared formatter is used when omitted.</param>
    public HelpCommandBuilder EnableSearchPositional(Func<CommandHelpContext, List<string>>? formatter = null)
    {
        _searchPositionalEnabled = true;
        _searchFormatter = formatter;
        return this;
    }

    /// <summary>Enables searching the overview through a named option.</summary>
    /// <param name="name">The option name, without the configured prefix.</param>
    /// <param name="formatter">An optional search formatter; the shared formatter is used when omitted.</param>
    /// <param name="aliases">Optional aliases for the search option.</param>
    public HelpCommandBuilder EnableSearchOption(
        string name,
        Func<CommandHelpContext, List<string>>? formatter = null,
        params string[] aliases)
    {
        _searchOptionName = ValidateName(name, nameof(name));
        _searchOptionAliases.Clear();
        _searchOptionAliases.AddRange(ValidateAliases(aliases, nameof(aliases)));
        _searchFormatter = formatter;
        _searchOptionEnabled = true;
        return this;
    }

    /// <summary>Enables command-specific help through one optional positional query.</summary>
    /// <param name="formatter">An optional command-help formatter; the shared formatter is used when omitted.</param>
    public HelpCommandBuilder EnableCommandHelpPositional(Func<CommandHelpContext, List<string>>? formatter = null)
    {
        _commandHelpPositionalEnabled = true;
        _commandHelpFormatter = formatter;
        return this;
    }

    /// <summary>Enables command-specific help through a named option.</summary>
    /// <param name="name">The option name, without the configured prefix.</param>
    /// <param name="formatter">An optional command-help formatter; the shared formatter is used when omitted.</param>
    /// <param name="aliases">Optional aliases for the command-help option.</param>
    public HelpCommandBuilder EnableCommandHelpOption(
        string name,
        Func<CommandHelpContext, List<string>>? formatter = null,
        params string[] aliases)
    {
        _commandHelpOptionName = ValidateName(name, nameof(name));
        _commandHelpOptionAliases.Clear();
        _commandHelpOptionAliases.AddRange(ValidateAliases(aliases, nameof(aliases)));
        _commandHelpFormatter = formatter;
        _commandHelpOptionEnabled = true;
        return this;
    }

    /// <summary>Sets the policy used when a command fails permission or availability checks.</summary>
    /// <param name="policy">The policy used for blocked commands.</param>
    public HelpCommandBuilder AccessPolicy(HelpCommandAccessPolicy policy)
    {
        _accessPolicy = policy ?? throw new ArgumentNullException(nameof(policy));
        return this;
    }

    /// <summary>Builds an immutable help command configuration.</summary>
    public HelpCommandDefinition Build()
    {
        CommandPathValidation.Validate(_path);

        if (_formatter is null)
        {
            throw new InvalidOperationException("A shared formatter must be configured before building a help command.");
        }

        return new HelpCommandDefinition(
            _path,
            _formatter,
            _searchPositionalEnabled,
            _searchOptionEnabled,
            _searchOptionName,
            new List<string>(_searchOptionAliases).AsReadOnly(),
            _searchFormatter,
            _commandHelpPositionalEnabled,
            _commandHelpOptionEnabled,
            _commandHelpOptionName,
            new List<string>(_commandHelpOptionAliases).AsReadOnly(),
            _commandHelpFormatter,
            _accessPolicy);
    }

    private static string ValidateName(string value, string parameterName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Names cannot be blank.", parameterName);
        }

        return value;
    }

    private static List<string> ValidateAliases(string[] aliases, string parameterName)
    {
        if (aliases is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        var copy = new List<string>(aliases.Length);
        foreach (string alias in aliases)
        {
            string validAlias = ValidateName(alias, parameterName);
            if (!copy.Contains(validAlias, StringComparer.Ordinal))
            {
                copy.Add(validAlias);
            }
        }

        return copy;
    }
}
