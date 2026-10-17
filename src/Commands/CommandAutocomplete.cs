using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Workes.ConsoleSystem.Configuration;

namespace Workes.ConsoleSystem.Commands;

internal static class CommandAutocomplete
{
    public static CommandAutocompleteResult GetAutocomplete(
        string input,
        int cursorIndex,
        IReadOnlyList<CommandDefinition> commands,
        CommandParsingOptions options,
        CommandAutocompleteOptions autocompleteOptions,
        IReadOnlyDictionary<CommandDefinition, CommandAvailabilityResult>? availability = null)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        if (cursorIndex < 0 || cursorIndex > input.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(cursorIndex), "Cursor index must be inside the input.");
        }

        if (commands is null)
        {
            throw new ArgumentNullException(nameof(commands));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (autocompleteOptions is null)
        {
            throw new ArgumentNullException(nameof(autocompleteOptions));
        }

        IReadOnlyList<CommandToken> tokens = CommandTokenizer.Tokenize(input, options.AllowQuotedStrings, allowIncompleteQuotes: true).Tokens;
        CommandToken? currentToken = FindCurrentToken(tokens, cursorIndex);
        StringComparer comparer = options.IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        StringComparison comparison = options.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        bool cursorBeforeFirstToken = tokens.Count > 0 && currentToken is null && cursorIndex <= tokens[0].Start;
        if (tokens.Count == 0 || cursorBeforeFirstToken || (currentToken is not null && currentToken.Value.Index == 0))
        {
            string partialPath = currentToken is null ? string.Empty : currentToken.Value.TextBefore(cursorIndex);
            int replacementStart = currentToken is null
                ? cursorIndex
                : GetPathReplacementStart(currentToken.Value, autocompleteOptions.PathCompletionMode, cursorIndex);
            int replacementLength = currentToken is null
                ? 0
                : GetPathReplacementLength(currentToken.Value, autocompleteOptions.PathCompletionMode, cursorIndex);
            return CreateResult(
                input,
                cursorIndex,
                replacementStart,
                replacementLength,
                CreatePathCandidates(commands, partialPath, autocompleteOptions.PathCompletionMode, comparison, availability));
        }

        CommandDefinition? command = FindCommand(commands, tokens[0].Text, comparer);
        if (command is null)
        {
            return CreateResult(
                input,
                cursorIndex,
                GetPathReplacementStart(tokens[0], autocompleteOptions.PathCompletionMode, cursorIndex),
                GetPathReplacementLength(tokens[0], autocompleteOptions.PathCompletionMode, cursorIndex),
                CreatePathCandidates(commands, tokens[0].Text, autocompleteOptions.PathCompletionMode, comparison, availability));
        }

        if (availability is not null && availability.TryGetValue(command, out CommandAvailabilityResult? commandStatus) && !commandStatus.IsAvailable)
        {
            return Empty(input, cursorIndex, cursorIndex, 0);
        }

        return CompleteCommandBody(input, cursorIndex, tokens, currentToken, command, options, comparer, comparison);
    }

    private static CommandAutocompleteResult CompleteCommandBody(
        string input,
        int cursorIndex,
        IReadOnlyList<CommandToken> tokens,
        CommandToken? currentToken,
        CommandDefinition command,
        CommandParsingOptions options,
        StringComparer comparer,
        StringComparison comparison)
    {
        int argumentIndex = 0;
        var usedNamedMembers = new HashSet<string>(comparer);
        CommandOptionDefinition? pendingOption = null;

        for (int index = 1; index < tokens.Count; index++)
        {
            CommandToken token = tokens[index];
            if (currentToken is not null && token.Index == currentToken.Value.Index)
            {
                break;
            }

            if (token.Start >= cursorIndex)
            {
                break;
            }

            if (pendingOption is not null)
            {
                if (token.WasQuoted || !IsPrefixedName(token.Text, options.FlagAndOptionPrefix))
                {
                    pendingOption = null;
                    continue;
                }

                pendingOption = null;
            }

            if (token.WasQuoted || !IsPrefixedName(token.Text, options.FlagAndOptionPrefix))
            {
                if (argumentIndex < command.Arguments.Count)
                {
                    argumentIndex++;
                }

                continue;
            }

            if (!options.AllowInterleavedOptions && argumentIndex < command.Arguments.Count)
            {
                return Empty(input, cursorIndex, cursorIndex, 0);
            }

            string memberText = token.Text.Substring(options.FlagAndOptionPrefix.Length);
            int memberEqualsIndex = memberText.IndexOf('=');
            string memberName = memberEqualsIndex < 0 ? memberText : memberText.Substring(0, memberEqualsIndex);
            CommandFlagDefinition? flag = FindFlag(command.Flags, memberName, comparer);
            if (flag is not null)
            {
                usedNamedMembers.Add(flag.Name);
                continue;
            }

            CommandOptionDefinition? option = FindOption(command.Options, memberName, comparer);
            if (option is null)
            {
                continue;
            }

            usedNamedMembers.Add(option.Name);
            if (memberEqualsIndex >= 0)
            {
                continue;
            }

            if (options.OptionValueStyle != OptionValueStyle.EqualSeparated)
            {
                pendingOption = option;
            }
        }

        int replacementStart = currentToken?.ContentStart ?? cursorIndex;
        int replacementLength = currentToken is null ? 0 : currentToken.Value.ContentEnd - currentToken.Value.ContentStart;
        string currentText = currentToken?.TextBefore(cursorIndex) ?? string.Empty;
        bool currentIsNamedMember = currentToken?.WasQuoted != true && IsPrefixedName(currentText, options.FlagAndOptionPrefix);

        if (pendingOption is not null)
        {
            if (currentToken is not null && currentIsNamedMember)
            {
                return Empty(input, cursorIndex, replacementStart, replacementLength);
            }

            return CompleteValue(
                input,
                cursorIndex,
                replacementStart,
                replacementLength,
                currentText,
                command,
                pendingOption,
                tokens,
                comparison);
        }

        if (!currentIsNamedMember)
        {
            if (argumentIndex < command.Arguments.Count)
            {
                return CompleteValue(
                    input,
                    cursorIndex,
                    replacementStart,
                    replacementLength,
                    currentText,
                    command,
                    command.Arguments[argumentIndex],
                    tokens,
                    comparison);
            }

            return Empty(input, cursorIndex, replacementStart, replacementLength);
        }

        if (!options.AllowInterleavedOptions && argumentIndex < command.Arguments.Count)
        {
            return Empty(input, cursorIndex, replacementStart, replacementLength);
        }

        CommandToken activeToken = currentToken!.Value;
        string nameAndMaybeValue = currentText.Substring(options.FlagAndOptionPrefix.Length);
        int equalsIndex = nameAndMaybeValue.IndexOf('=');
        if (equalsIndex >= 0)
        {
            string optionName = nameAndMaybeValue.Substring(0, equalsIndex);
            CommandOptionDefinition? option = FindOption(command.Options, optionName, comparer);
            if (option is not null && options.OptionValueStyle != OptionValueStyle.SpaceSeparated)
            {
                int valueStart = activeToken.ContentStart + options.FlagAndOptionPrefix.Length + equalsIndex + 1;
                int valueLength = activeToken.ContentEnd - valueStart;
                string partialValue = currentText.Substring(options.FlagAndOptionPrefix.Length + equalsIndex + 1);
                return CompleteValue(input, cursorIndex, valueStart, valueLength, partialValue, command, option, tokens, comparison);
            }
        }

        string partialName = equalsIndex >= 0 ? nameAndMaybeValue.Substring(0, equalsIndex) : nameAndMaybeValue;
        return CompleteNamedMembers(
            input,
            cursorIndex,
            activeToken.ContentStart,
            activeToken.ContentEnd - activeToken.ContentStart,
            partialName,
            command,
            options,
            usedNamedMembers,
            comparison);
    }

    private static CommandAutocompleteResult CompleteNamedMembers(
        string input,
        int cursorIndex,
        int replacementStart,
        int replacementLength,
        string partialName,
        CommandDefinition command,
        CommandParsingOptions options,
        HashSet<string> usedNamedMembers,
        StringComparison comparison)
    {
        var candidates = new List<CommandAutocompleteCandidate>();
        foreach (CommandFlagDefinition flag in command.Flags)
        {
            AddMemberCandidates(candidates, flag, CommandAutocompleteCandidateKind.Flag, options.FlagAndOptionPrefix, partialName, usedNamedMembers, comparison);
        }

        foreach (CommandOptionDefinition option in command.Options)
        {
            AddMemberCandidates(candidates, option, CommandAutocompleteCandidateKind.Option, options.FlagAndOptionPrefix, partialName, usedNamedMembers, comparison);
        }

        return CreateResult(input, cursorIndex, replacementStart, replacementLength, candidates);
    }

    private static void AddMemberCandidates(
        List<CommandAutocompleteCandidate> candidates,
        CommandMemberDefinition member,
        CommandAutocompleteCandidateKind kind,
        string prefix,
        string partialName,
        HashSet<string> usedNamedMembers,
        StringComparison comparison)
    {
        if (usedNamedMembers.Contains(member.Name))
        {
            return;
        }

        AddMemberCandidate(candidates, member.Name, member, kind, prefix, partialName, comparison);
        foreach (string alias in member.Aliases)
        {
            AddMemberCandidate(candidates, alias, member, kind, prefix, partialName, comparison);
        }
    }

    private static void AddMemberCandidate(
        List<CommandAutocompleteCandidate> candidates,
        string name,
        CommandMemberDefinition member,
        CommandAutocompleteCandidateKind kind,
        string prefix,
        string partialName,
        StringComparison comparison)
    {
        if (!name.StartsWith(partialName, comparison))
        {
            return;
        }

        string text = prefix + name;
        candidates.Add(new CommandAutocompleteCandidate(text, kind, text, member.Description));
    }

    private static CommandAutocompleteResult CompleteValue(
        string input,
        int cursorIndex,
        int replacementStart,
        int replacementLength,
        string partialValue,
        CommandDefinition command,
        CommandMemberDefinition member,
        IReadOnlyList<CommandToken> tokens,
        StringComparison comparison)
    {
        var candidates = new List<CommandAutocompleteCandidate>();
        if (member.ValueCandidateProvider is not null)
        {
            var context = new CommandAutocompleteContext(
                input,
                cursorIndex,
                partialValue,
                command,
                member,
                GetTokenTextsBeforeCursor(tokens, cursorIndex));

            foreach (string candidate in member.ValueCandidateProvider(context))
            {
                if (candidate is null)
                {
                    continue;
                }

                AddValueCandidate(candidates, candidate, member, partialValue, comparison);
            }
        }
        else if (member is CommandOptionDefinition { SuggestAllowedValues: true } option)
        {
            foreach (object? allowedValue in option.AllowedValues)
            {
                if (allowedValue is null)
                {
                    continue;
                }

                string candidate = Convert.ToString(allowedValue, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
                AddValueCandidate(candidates, candidate, member, partialValue, comparison);
            }
        }

        return CreateResult(input, cursorIndex, replacementStart, replacementLength, candidates);
    }

    private static void AddValueCandidate(
        List<CommandAutocompleteCandidate> candidates,
        string candidate,
        CommandMemberDefinition member,
        string partialValue,
        StringComparison comparison)
    {
        if (!candidate.StartsWith(partialValue, comparison))
        {
            return;
        }

        CommandAutocompleteCandidateKind kind = member is CommandArgumentDefinition
            ? CommandAutocompleteCandidateKind.ArgumentValue
            : CommandAutocompleteCandidateKind.OptionValue;
        candidates.Add(new CommandAutocompleteCandidate(candidate, kind));
    }

    private static IEnumerable<CommandAutocompleteCandidate> CreatePathCandidates(
        IReadOnlyList<CommandDefinition> commands,
        string partialPath,
        CommandPathCompletionMode pathCompletionMode,
        StringComparison comparison,
        IReadOnlyDictionary<CommandDefinition, CommandAvailabilityResult>? availability)
    {
        if (pathCompletionMode == CommandPathCompletionMode.DotSegment)
        {
            return CreateDotSegmentPathCandidates(commands, partialPath, comparison, availability);
        }

        return CreateFullPathCandidates(commands, partialPath, comparison, availability);
    }

    private static IEnumerable<CommandAutocompleteCandidate> CreateFullPathCandidates(
        IReadOnlyList<CommandDefinition> commands,
        string partialPath,
        StringComparison comparison,
        IReadOnlyDictionary<CommandDefinition, CommandAvailabilityResult>? availability)
    {
        foreach (CommandDefinition command in commands)
        {
            if (command.Path.StartsWith(partialPath, comparison))
            {
                CommandAvailabilityResult? status = GetAvailability(command, availability);
                yield return new CommandAutocompleteCandidate(
                    command.Path,
                    CommandAutocompleteCandidateKind.CommandPath,
                    command.Path,
                    command.Description,
                    status?.IsAvailable ?? true,
                    status?.Reason);
            }
        }
    }

    private static IEnumerable<CommandAutocompleteCandidate> CreateDotSegmentPathCandidates(
        IReadOnlyList<CommandDefinition> commands,
        string partialPath,
        StringComparison comparison,
        IReadOnlyDictionary<CommandDefinition, CommandAvailabilityResult>? availability)
    {
        int segmentStart = partialPath.LastIndexOf('.') + 1;
        string prefix = partialPath.Substring(0, segmentStart);
        string partialSegment = partialPath.Substring(segmentStart);
        var seen = new HashSet<string>(comparison == StringComparison.Ordinal ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);

        foreach (CommandDefinition command in commands)
        {
            if (string.Equals(command.Path, partialPath, comparison))
            {
                continue;
            }

            if (!command.Path.StartsWith(prefix, comparison))
            {
                continue;
            }

            string remaining = command.Path.Substring(prefix.Length);
            int dotIndex = remaining.IndexOf('.');
            string segment = dotIndex < 0 ? remaining : remaining.Substring(0, dotIndex);
            if (!segment.StartsWith(partialSegment, comparison))
            {
                continue;
            }

            if (string.Equals(segment, partialSegment, comparison))
            {
                continue;
            }

            if (!seen.Add(segment))
            {
                continue;
            }

            string displayText = prefix + segment;
            if (dotIndex >= 0)
            {
                displayText += ".";
            }

            CommandDefinition? source = FindPathSource(commands, prefix + segment, comparison, availability);
            CommandAvailabilityResult? status = source is null ? null : GetAvailability(source, availability);
            yield return new CommandAutocompleteCandidate(
                segment,
                CommandAutocompleteCandidateKind.CommandPath,
                displayText,
                command.Description,
                status?.IsAvailable ?? true,
                status?.Reason);
        }
    }

    private static CommandDefinition? FindPathSource(
        IReadOnlyList<CommandDefinition> commands,
        string partialPath,
        StringComparison comparison,
        IReadOnlyDictionary<CommandDefinition, CommandAvailabilityResult>? availability)
    {
        CommandDefinition? firstMatch = null;
        foreach (CommandDefinition command in commands)
        {
            if (command.Path.StartsWith(partialPath, comparison))
            {
                firstMatch ??= command;
                if (GetAvailability(command, availability)?.IsAvailable ?? true)
                {
                    return command;
                }
            }
        }

        return firstMatch;
    }

    private static CommandAvailabilityResult? GetAvailability(
        CommandDefinition command,
        IReadOnlyDictionary<CommandDefinition, CommandAvailabilityResult>? availability)
    {
        if (availability is not null && availability.TryGetValue(command, out CommandAvailabilityResult? result))
        {
            return result;
        }

        return null;
    }

    private static int GetPathReplacementStart(CommandToken token, CommandPathCompletionMode pathCompletionMode, int cursorIndex)
    {
        if (pathCompletionMode == CommandPathCompletionMode.FullPath)
        {
            return token.ContentStart;
        }

        int dotIndex = token.TextBefore(cursorIndex).LastIndexOf('.');
        return dotIndex < 0 ? token.ContentStart : token.ContentStart + dotIndex + 1;
    }

    private static int GetPathReplacementLength(CommandToken token, CommandPathCompletionMode pathCompletionMode, int cursorIndex)
    {
        if (pathCompletionMode == CommandPathCompletionMode.FullPath)
        {
            return token.ContentEnd - token.ContentStart;
        }

        string partialPath = token.TextBefore(cursorIndex);
        int dotIndex = partialPath.LastIndexOf('.');
        int replacementStart = dotIndex < 0 ? token.ContentStart : token.ContentStart + dotIndex + 1;
        int nextDotIndex = token.Text.IndexOf('.', partialPath.Length);
        int replacementEnd = nextDotIndex < 0 ? token.ContentEnd : token.ContentStart + nextDotIndex;
        return replacementEnd - replacementStart;
    }

    private static CommandDefinition? FindCommand(IReadOnlyList<CommandDefinition> commands, string path, StringComparer comparer)
    {
        foreach (CommandDefinition command in commands)
        {
            if (comparer.Equals(command.Path, path))
            {
                return command;
            }
        }

        return null;
    }

    private static CommandFlagDefinition? FindFlag(IReadOnlyList<CommandFlagDefinition> flags, string name, StringComparer comparer)
    {
        foreach (CommandFlagDefinition flag in flags)
        {
            if (NameMatches(flag, name, comparer))
            {
                return flag;
            }
        }

        return null;
    }

    private static CommandOptionDefinition? FindOption(IReadOnlyList<CommandOptionDefinition> options, string name, StringComparer comparer)
    {
        foreach (CommandOptionDefinition option in options)
        {
            if (NameMatches(option, name, comparer))
            {
                return option;
            }
        }

        return null;
    }

    private static bool NameMatches(CommandMemberDefinition member, string providedName, StringComparer comparer)
    {
        if (comparer.Equals(member.Name, providedName))
        {
            return true;
        }

        foreach (string alias in member.Aliases)
        {
            if (comparer.Equals(alias, providedName))
            {
                return true;
            }
        }

        return false;
    }

    private static CommandAutocompleteResult Empty(string input, int cursorIndex, int replacementStart, int replacementLength)
    {
        return CreateResult(input, cursorIndex, replacementStart, replacementLength, Array.Empty<CommandAutocompleteCandidate>());
    }

    private static CommandAutocompleteResult CreateResult(
        string input,
        int cursorIndex,
        int replacementStart,
        int replacementLength,
        IEnumerable<CommandAutocompleteCandidate> candidates)
    {
        return new CommandAutocompleteResult(
            input,
            cursorIndex,
            replacementStart,
            replacementLength,
            new ReadOnlyCollection<CommandAutocompleteCandidate>(new List<CommandAutocompleteCandidate>(candidates)));
    }

    private static bool IsPrefixedName(string text, string prefix)
    {
        return text.StartsWith(prefix, StringComparison.Ordinal);
    }

    private static CommandToken? FindCurrentToken(IReadOnlyList<CommandToken> tokens, int cursorIndex)
    {
        foreach (CommandToken token in tokens)
        {
            if (cursorIndex >= token.ContentStart && cursorIndex <= token.ContentEnd)
            {
                return token;
            }
        }

        return null;
    }

    private static IReadOnlyList<string> GetTokenTextsBeforeCursor(IReadOnlyList<CommandToken> tokens, int cursorIndex)
    {
        var result = new List<string>();
        foreach (CommandToken token in tokens)
        {
            if (token.Start >= cursorIndex)
            {
                continue;
            }

            result.Add(token.TextBefore(cursorIndex));
        }

        return result.AsReadOnly();
    }

}
