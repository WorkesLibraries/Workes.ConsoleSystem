using System;
using System.Collections.Generic;
using System.Linq;
using Workes.ConsoleSystem.Configuration;

namespace Workes.ConsoleSystem.Commands;

/// <summary>Provides a command and the active parser settings to a syntax formatter.</summary>
public sealed class CommandSyntaxContext
{
    internal CommandSyntaxContext(CommandDefinition command, CommandParsingOptions parsingOptions)
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
        ParsingOptions = parsingOptions ?? throw new ArgumentNullException(nameof(parsingOptions));
    }

    /// <summary>The command being formatted.</summary>
    public CommandDefinition Command { get; }

    /// <summary>A detached snapshot of the manager's parsing settings.</summary>
    public CommandParsingOptions ParsingOptions { get; }
}

/// <summary>Creates syntax text for a command and parser configuration.</summary>
public delegate string CommandSyntaxFormatter(CommandSyntaxContext context);

/// <summary>Built-in command syntax formatters.</summary>
public static class CommandSyntaxFormatters
{
    /// <summary>Formats a parser-aware syntax line, including optional positionals and shared optional/option values.</summary>
    public static CommandSyntaxFormatter Canonical { get; } = FormatCanonical;

    private static string FormatCanonical(CommandSyntaxContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        CommandDefinition command = context.Command;
        string prefix = context.ParsingOptions.FlagAndOptionPrefix;
        var pieces = new List<string> { command.Path };
        for (int i = 0; i < command.Arguments.Count; i++)
        {
            CommandArgumentDefinition argument = command.Arguments[i];
            string positional = $"<{argument.Name}>";
            CommandOptionDefinition? shared = command.Options.FirstOrDefault(option => option.Id == argument.Id);
            if (argument.IsOptional && shared is not null)
            {
                string optionToken = FormatOption(shared, prefix, context.ParsingOptions.OptionValueStyle);
                pieces.Add($"[{positional} | {optionToken}]");
            }
            else
            {
                pieces.Add(argument.IsOptional ? $"[{positional}]" : positional);
            }
        }

        foreach (CommandFlagDefinition flag in command.Flags)
        {
            var names = new List<string> { prefix + flag.Name };
            names.AddRange(flag.Aliases.Select(alias => prefix + alias));
            pieces.Add($"[{string.Join(" | ", names)}]");
        }
        foreach (CommandOptionDefinition option in command.Options)
        {
            if (command.Arguments.Any(argument => argument.IsOptional && argument.Id == option.Id)) continue;
            pieces.Add($"[{FormatOption(option, prefix, context.ParsingOptions.OptionValueStyle)}]");
        }
        return string.Join(" ", pieces);
    }

    private static string FormatOption(CommandOptionDefinition option, string prefix, OptionValueStyle style)
    {
        string value = $"<{option.Name}>";
        var names = new List<string> { option.Name };
        names.AddRange(option.Aliases);
        return string.Join(" | ", names.Select(name => style switch
        {
            OptionValueStyle.EqualSeparated => $"{prefix}{name}={value}",
            OptionValueStyle.AnySeparated => $"{prefix}{name} {value} | {prefix}{name}={value}",
            _ => $"{prefix}{name} {value}"
        }));
    }
}
