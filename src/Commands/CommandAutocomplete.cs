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
        CommandAutocompleteOptions autocompleteOptions)
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

        IReadOnlyList<AutocompleteToken> tokens = Tokenize(input);
        AutocompleteToken? currentToken = FindCurrentToken(tokens, cursorIndex);
        StringComparer comparer = options.IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        StringComparison comparison = options.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        if (tokens.Count == 0 || (currentToken is not null && currentToken.Value.Index == 0))
        {
            string partialPath = currentToken is null ? string.Empty : currentToken.Value.TextBefore(cursorIndex);
            int replacementStart = currentToken is null
                ? cursorIndex
                : GetPathReplacementStart(currentToken.Value, autocompleteOptions.PathCompletionMode);
            int replacementLength = currentToken is null
                ? 0
                : GetPathReplacementLength(currentToken.Value, autocompleteOptions.PathCompletionMode);
            return CreateResult(
                input,
                cursorIndex,
                replacementStart,
                replacementLength,
                CreatePathCandidates(commands, partialPath, autocompleteOptions.PathCompletionMode, comparison));
        }

        CommandDefinition? command = FindCommand(commands, tokens[0].Text, comparer);
        if (command is null)
        {
            return CreateResult(
                input,
                cursorIndex,
                GetPathReplacementStart(tokens[0], autocompleteOptions.PathCompletionMode),
                GetPathReplacementLength(tokens[0], autocompleteOptions.PathCompletionMode),
                CreatePathCandidates(commands, tokens[0].Text, autocompleteOptions.PathCompletionMode, comparison));
        }

        return CompleteCommandBody(input, cursorIndex, tokens, currentToken, command, options, comparer, comparison);
    }

    private static CommandAutocompleteResult CompleteCommandBody(
        string input,
        int cursorIndex,
        IReadOnlyList<AutocompleteToken> tokens,
        AutocompleteToken? currentToken,
        CommandDefinition command,
        CommandParsingOptions options,
        StringComparer comparer,
        StringComparison comparison)
    {
        int tokenIndex = 1;
        foreach (CommandArgumentDefinition argument in command.Arguments)
        {
            if (tokenIndex >= tokens.Count)
            {
                return CompleteValue(input, cursorIndex, cursorIndex, 0, string.Empty, command, argument, tokens);
            }

            AutocompleteToken token = tokens[tokenIndex];
            if (currentToken is not null && token.Index == currentToken.Value.Index)
            {
                if (IsPrefixedName(token.TextBefore(cursorIndex), options.FlagAndOptionPrefix))
                {
                    return Empty(input, cursorIndex, token.ContentStart, token.ContentEnd - token.ContentStart);
                }

                string partial = token.TextBefore(cursorIndex);
                return CompleteValue(input, cursorIndex, token.ContentStart, token.ContentEnd - token.ContentStart, partial, command, argument, tokens);
            }

            if (IsPrefixedName(token.Text, options.FlagAndOptionPrefix))
            {
                return Empty(input, cursorIndex, cursorIndex, 0);
            }

            tokenIndex++;
        }

        HashSet<string> usedNamedMembers = FindUsedNamedMembers(tokens, currentToken, tokenIndex, command, options, comparer);
        AutocompleteToken? previousToken = FindPreviousToken(tokens, cursorIndex, currentToken);
        PendingOptionValue? pendingOption = FindPendingOptionValue(previousToken, command, options, comparer);
        if (pendingOption is not null && (currentToken is null || currentToken.Value.Index != previousToken!.Value.Index))
        {
            if (currentToken is null)
            {
                return CompleteValue(input, cursorIndex, cursorIndex, 0, string.Empty, command, pendingOption.Value.Option, tokens);
            }

            string partialValue = currentToken.Value.TextBefore(cursorIndex);
            return CompleteValue(input, cursorIndex, currentToken.Value.ContentStart, currentToken.Value.ContentEnd - currentToken.Value.ContentStart, partialValue, command, pendingOption.Value.Option, tokens);
        }

        if (currentToken is null)
        {
            return CompleteNamedMembers(input, cursorIndex, cursorIndex, 0, string.Empty, command, options, usedNamedMembers, comparison);
        }

        string currentText = currentToken.Value.TextBefore(cursorIndex);
        if (!IsPrefixedName(currentText, options.FlagAndOptionPrefix))
        {
            return Empty(input, cursorIndex, currentToken.Value.ContentStart, currentToken.Value.ContentEnd - currentToken.Value.ContentStart);
        }

        string nameAndMaybeValue = currentText.Substring(options.FlagAndOptionPrefix.Length);
        int equalsIndex = nameAndMaybeValue.IndexOf('=');
        if (equalsIndex >= 0)
        {
            string optionName = nameAndMaybeValue.Substring(0, equalsIndex);
            CommandOptionDefinition? option = FindOption(command.Options, optionName, comparer);
            if (option is not null && options.OptionValueStyle != OptionValueStyle.SpaceSeparated)
            {
                int valueStart = currentToken.Value.ContentStart + options.FlagAndOptionPrefix.Length + equalsIndex + 1;
                int valueLength = currentToken.Value.ContentEnd - valueStart;
                string partialValue = currentText.Substring(options.FlagAndOptionPrefix.Length + equalsIndex + 1);
                return CompleteValue(input, cursorIndex, valueStart, valueLength, partialValue, command, option, tokens);
            }
        }

        string partialName = equalsIndex >= 0 ? nameAndMaybeValue.Substring(0, equalsIndex) : nameAndMaybeValue;
        return CompleteNamedMembers(
            input,
            cursorIndex,
            currentToken.Value.ContentStart,
            currentToken.Value.ContentEnd - currentToken.Value.ContentStart,
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
        IReadOnlyList<AutocompleteToken> tokens)
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

                if (!candidate.StartsWith(partialValue, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                CommandAutocompleteCandidateKind kind = member is CommandArgumentDefinition
                    ? CommandAutocompleteCandidateKind.ArgumentValue
                    : CommandAutocompleteCandidateKind.OptionValue;
                candidates.Add(new CommandAutocompleteCandidate(candidate, kind));
            }
        }

        return CreateResult(input, cursorIndex, replacementStart, replacementLength, candidates);
    }

    private static IEnumerable<CommandAutocompleteCandidate> CreatePathCandidates(
        IReadOnlyList<CommandDefinition> commands,
        string partialPath,
        CommandPathCompletionMode pathCompletionMode,
        StringComparison comparison)
    {
        if (pathCompletionMode == CommandPathCompletionMode.DotSegment)
        {
            return CreateDotSegmentPathCandidates(commands, partialPath, comparison);
        }

        return CreateFullPathCandidates(commands, partialPath, comparison);
    }

    private static IEnumerable<CommandAutocompleteCandidate> CreateFullPathCandidates(
        IReadOnlyList<CommandDefinition> commands,
        string partialPath,
        StringComparison comparison)
    {
        foreach (CommandDefinition command in commands)
        {
            if (command.Path.StartsWith(partialPath, comparison))
            {
                yield return new CommandAutocompleteCandidate(
                    command.Path,
                    CommandAutocompleteCandidateKind.CommandPath,
                    command.Path,
                    command.Description);
            }
        }
    }

    private static IEnumerable<CommandAutocompleteCandidate> CreateDotSegmentPathCandidates(
        IReadOnlyList<CommandDefinition> commands,
        string partialPath,
        StringComparison comparison)
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

            yield return new CommandAutocompleteCandidate(
                segment,
                CommandAutocompleteCandidateKind.CommandPath,
                displayText,
                command.Description);
        }
    }

    private static int GetPathReplacementStart(AutocompleteToken token, CommandPathCompletionMode pathCompletionMode)
    {
        if (pathCompletionMode == CommandPathCompletionMode.FullPath)
        {
            return token.ContentStart;
        }

        int dotIndex = token.Text.LastIndexOf('.');
        return dotIndex < 0 ? token.ContentStart : token.ContentStart + dotIndex + 1;
    }

    private static int GetPathReplacementLength(AutocompleteToken token, CommandPathCompletionMode pathCompletionMode)
    {
        if (pathCompletionMode == CommandPathCompletionMode.FullPath)
        {
            return token.ContentEnd - token.ContentStart;
        }

        int dotIndex = token.Text.LastIndexOf('.');
        return dotIndex < 0 ? token.ContentEnd - token.ContentStart : token.ContentEnd - token.ContentStart - dotIndex - 1;
    }

    private static HashSet<string> FindUsedNamedMembers(
        IReadOnlyList<AutocompleteToken> tokens,
        AutocompleteToken? currentToken,
        int startIndex,
        CommandDefinition command,
        CommandParsingOptions options,
        StringComparer comparer)
    {
        var used = new HashSet<string>(comparer);
        for (int index = startIndex; index < tokens.Count; index++)
        {
            AutocompleteToken token = tokens[index];
            if (currentToken is not null && token.Index == currentToken.Value.Index)
            {
                continue;
            }

            if (!IsPrefixedName(token.Text, options.FlagAndOptionPrefix))
            {
                continue;
            }

            string nameAndMaybeValue = token.Text.Substring(options.FlagAndOptionPrefix.Length);
            int equalsIndex = nameAndMaybeValue.IndexOf('=');
            string providedName = equalsIndex >= 0 ? nameAndMaybeValue.Substring(0, equalsIndex) : nameAndMaybeValue;

            CommandFlagDefinition? flag = FindFlag(command.Flags, providedName, comparer);
            if (flag is not null)
            {
                used.Add(flag.Name);
                continue;
            }

            CommandOptionDefinition? option = FindOption(command.Options, providedName, comparer);
            if (option is not null)
            {
                used.Add(option.Name);
            }
        }

        return used;
    }

    private static PendingOptionValue? FindPendingOptionValue(
        AutocompleteToken? previousToken,
        CommandDefinition command,
        CommandParsingOptions options,
        StringComparer comparer)
    {
        if (previousToken is null || !IsPrefixedName(previousToken.Value.Text, options.FlagAndOptionPrefix))
        {
            return null;
        }

        if (options.OptionValueStyle == OptionValueStyle.EqualSeparated)
        {
            return null;
        }

        string nameAndMaybeValue = previousToken.Value.Text.Substring(options.FlagAndOptionPrefix.Length);
        if (nameAndMaybeValue.IndexOf('=') >= 0)
        {
            return null;
        }

        CommandOptionDefinition? option = FindOption(command.Options, nameAndMaybeValue, comparer);
        return option is null ? null : new PendingOptionValue(option);
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

    private static AutocompleteToken? FindCurrentToken(IReadOnlyList<AutocompleteToken> tokens, int cursorIndex)
    {
        foreach (AutocompleteToken token in tokens)
        {
            if (cursorIndex >= token.ContentStart && cursorIndex <= token.ContentEnd)
            {
                return token;
            }
        }

        return null;
    }

    private static AutocompleteToken? FindPreviousToken(
        IReadOnlyList<AutocompleteToken> tokens,
        int cursorIndex,
        AutocompleteToken? currentToken)
    {
        AutocompleteToken? previous = null;
        foreach (AutocompleteToken token in tokens)
        {
            if (token.End <= cursorIndex)
            {
                if (currentToken is not null && token.Index == currentToken.Value.Index)
                {
                    continue;
                }

                previous = token;
            }
        }

        return previous;
    }

    private static IReadOnlyList<string> GetTokenTextsBeforeCursor(IReadOnlyList<AutocompleteToken> tokens, int cursorIndex)
    {
        var result = new List<string>();
        foreach (AutocompleteToken token in tokens)
        {
            if (token.Start >= cursorIndex)
            {
                continue;
            }

            result.Add(token.TextBefore(cursorIndex));
        }

        return result.AsReadOnly();
    }

    private static IReadOnlyList<AutocompleteToken> Tokenize(string input)
    {
        var tokens = new List<AutocompleteToken>();
        int index = 0;

        while (index < input.Length)
        {
            while (index < input.Length && char.IsWhiteSpace(input[index]))
            {
                index++;
            }

            if (index >= input.Length)
            {
                break;
            }

            int start = index;
            char quote = input[index];
            if (quote == '"' || quote == '\'')
            {
                index++;
                int contentStart = index;
                bool closed = false;
                var text = new System.Text.StringBuilder();
                while (index < input.Length)
                {
                    char character = input[index];
                    if (character == '\\' && index + 1 < input.Length)
                    {
                        char next = input[index + 1];
                        if (next == quote || next == '\\')
                        {
                            text.Append(next);
                            index += 2;
                            continue;
                        }
                    }

                    if (character == quote)
                    {
                        closed = true;
                        break;
                    }

                    text.Append(character);
                    index++;
                }

                int contentEnd = index;
                if (closed)
                {
                    index++;
                }

                tokens.Add(new AutocompleteToken(tokens.Count, start, index, contentStart, contentEnd, text.ToString()));
                continue;
            }

            while (index < input.Length && !char.IsWhiteSpace(input[index]))
            {
                index++;
            }

            tokens.Add(new AutocompleteToken(tokens.Count, start, index, start, index, input.Substring(start, index - start)));
        }

        return tokens.AsReadOnly();
    }

    private readonly struct PendingOptionValue
    {
        public PendingOptionValue(CommandOptionDefinition option)
        {
            Option = option;
        }

        public CommandOptionDefinition Option { get; }
    }

    private readonly struct AutocompleteToken
    {
        public AutocompleteToken(int index, int start, int end, int contentStart, int contentEnd, string text)
        {
            Index = index;
            Start = start;
            End = end;
            ContentStart = contentStart;
            ContentEnd = contentEnd;
            Text = text;
        }

        public int Index { get; }

        public int Start { get; }

        public int End { get; }

        public int ContentStart { get; }

        public int ContentEnd { get; }

        public string Text { get; }

        public string TextBefore(int cursorIndex)
        {
            int length = Math.Max(0, Math.Min(cursorIndex, ContentEnd) - ContentStart);
            if (length >= Text.Length)
            {
                return Text;
            }

            return Text.Substring(0, length);
        }
    }
}
