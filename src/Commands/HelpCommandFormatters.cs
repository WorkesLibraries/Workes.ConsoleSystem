using System;
using System.Collections.Generic;
using System.Linq;

namespace Workes.ConsoleSystem.Commands;

/// <summary>Built-in help command formatters.</summary>
public static class HelpCommandFormatters
{
    /// <summary>Shows command path and its description when available.</summary>
    public static Func<CommandHelpContext, List<string>> Simple { get; } = context =>
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        return string.IsNullOrEmpty(context.Command.Description)
            ? new List<string> { context.Command.Path }
            : new List<string> { $"{context.Command.Path} : {context.Command.Description}" };
    };

    /// <summary>Shows available syntax, descriptions, notes, arguments, flags, options, and examples.</summary>
    public static Func<CommandHelpContext, List<string>> Detailed { get; } = context =>
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        CommandDefinition command = context.Command;
        var lines = new List<string> { command.Path };
        if (!string.IsNullOrEmpty(command.Description)) lines.Add(command.Description);
        if (!string.IsNullOrEmpty(context.Syntax)) lines.Add("Usage: " + context.Syntax);
        Append(lines, "Notes", command.Notes);
        Append(lines, "Examples", command.Examples);
        foreach (CommandArgumentDefinition argument in command.Arguments)
        {
            string heading = $"Argument {argument.Name}" + (argument.IsOptional ? " (optional)" : string.Empty);
            AddBinding(lines, heading, argument.Description, argument.Examples);
        }
        foreach (CommandFlagDefinition flag in command.Flags)
        {
            string label = "Flag " + context.OptionPrefix + flag.Name;
            if (flag.Aliases.Count > 0) label += " (" + string.Join(", ", flag.Aliases.Select(alias => context.OptionPrefix + alias)) + ")";
            AddBinding(lines, label, flag.Description, Array.Empty<string>());
        }
        foreach (CommandOptionDefinition option in command.Options)
        {
            string label = "Option " + context.OptionPrefix + option.Name;
            if (option.Aliases.Count > 0) label += " (" + string.Join(", ", option.Aliases.Select(alias => context.OptionPrefix + alias)) + ")";
            AddBinding(lines, label, option.Description, option.Examples);
        }
        return lines;
    };

    private static void Append(List<string> output, string heading, IReadOnlyList<string> values)
    {
        if (values.Count == 0) return;
        output.Add(heading + ":");
        output.AddRange(values);
    }

    private static void AddBinding(List<string> output, string heading, string description, IReadOnlyList<string> examples)
    {
        if (string.IsNullOrEmpty(description) && examples.Count == 0) return;
        output.Add(heading + (string.IsNullOrEmpty(description) ? string.Empty : ": " + description));
        if (examples.Count > 0) output.Add("  Examples: " + string.Join(", ", examples));
    }
}
