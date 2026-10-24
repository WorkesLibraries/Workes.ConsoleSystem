using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;

namespace Workes.ConsoleSystem.Commands;

internal static class CommandParser
{
    public static CommandParseResult Parse(
        string input,
        IReadOnlyList<CommandDefinition> commands,
        CommandParsingOptions options)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            return CommandParseResult.Failed(input, ConsoleFailures.CommandParsing(
                ConsoleFailureCodes.CommandInputEmpty,
                "Command input cannot be blank."), 0, input.Length);
        }

        CommandTokenizationResult tokenizeResult = CommandTokenizer.Tokenize(input, options.AllowQuotedStrings, allowIncompleteQuotes: false);
        if (tokenizeResult.Failure is not null)
        {
            return CommandParseResult.Failed(input, tokenizeResult.Failure!, tokenizeResult.FailureOffset, tokenizeResult.FailureLength);
        }

        IReadOnlyList<CommandToken> tokens = tokenizeResult.Tokens;
        if (tokens.Count == 0)
        {
            return CommandParseResult.Failed(input, ConsoleFailures.CommandParsing(
                ConsoleFailureCodes.CommandInputEmpty,
                "Command input cannot be blank."), 0, input.Length);
        }

        StringComparer comparer = options.IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        CommandDefinition? command = FindCommand(commands, tokens[0].Text, comparer);
        if (command is null)
        {
            return CommandParseResult.Failed(input, ConsoleFailures.CommandParsing(
                ConsoleFailureCodes.CommandUnknown,
                $"Unknown command '{tokens[0].Text}'.",
                source: tokens[0].Text), tokens[0].Start, tokens[0].Length);
        }

        return ParseCommandTokens(input, command, tokens, options, comparer);
    }

    private static CommandParseResult ParseCommandTokens(
        string input,
        CommandDefinition command,
        IReadOnlyList<CommandToken> tokens,
        CommandParsingOptions options,
        StringComparer comparer)
    {
        var argumentValues = new Dictionary<string, object?>(StringComparer.Ordinal);
        var flagValues = new Dictionary<string, object?>(StringComparer.Ordinal);
        var optionValues = new Dictionary<string, object?>(StringComparer.Ordinal);
        var explicitValues = new Dictionary<string, object?>(StringComparer.Ordinal);
        var resolvedValues = new Dictionary<string, object?>(StringComparer.Ordinal);
        var propertyValues = new Dictionary<string, object?>(comparer);
        var seenNamedMembers = new HashSet<string>(comparer);

        int argumentIndex = 0;
        int tokenIndex = 1;
        bool namedMembersStarted = false;
        foreach (CommandFlagDefinition flag in command.Flags)
        {
            resolvedValues[flag.Id] = false;
        }

        while (tokenIndex < tokens.Count)
        {
            CommandToken token = tokens[tokenIndex];
            if (!IsPrefixedName(token, options.FlagAndOptionPrefix))
            {
                if (argumentIndex >= command.Arguments.Count || (!options.AllowInterleavedOptions && namedMembersStarted))
                {
                    return CommandParseResult.Failed(input, ConsoleFailures.CommandParsing(
                        ConsoleFailureCodes.CommandArgumentUnexpected,
                        $"Unexpected positional value '{token.Text}'."), token.Start, token.Length);
                }

                CommandArgumentDefinition argument = command.Arguments[argumentIndex];
                if (explicitValues.ContainsKey(argument.Id))
                {
                    return CommandParseResult.Failed(input, ConsoleFailures.CommandParsing(
                        ConsoleFailureCodes.CommandMemberDuplicate,
                        $"Value '{argument.Id}' was supplied through more than one input binding.", source: argument.Id), token.Start, token.Length);
                }
                if (!TryConvert(token.Text, argument.ValueType, options, out object? convertedArgument, out string? message))
                {
                    return CommandParseResult.Failed(input, ConsoleFailures.CommandBinding(
                        ConsoleFailureCodes.CommandValueInvalid,
                        $"Invalid value for argument '{argument.Name}': {message}",
                        source: argument.Name), token.Start, token.Length);
                }

                argumentValues.Add(argument.Name, convertedArgument);
                explicitValues.Add(argument.Id, convertedArgument);
                resolvedValues[argument.Id] = convertedArgument;
                argumentIndex++;
                tokenIndex++;
                continue;
            }

            if (!options.AllowInterleavedOptions && argumentIndex < command.Arguments.Count && !command.Arguments[argumentIndex].IsOptional)
            {
                CommandArgumentDefinition missingArgument = command.Arguments[argumentIndex];
                return CommandParseResult.Failed(input, ConsoleFailures.CommandParsing(
                    ConsoleFailureCodes.CommandArgumentMissing,
                    $"Missing required argument '{missingArgument.Name}'.",
                    source: missingArgument.Name), token.Start, token.Length);
            }

            namedMembersStarted = true;
            string nameAndMaybeValue = token.Text.Substring(options.FlagAndOptionPrefix.Length);
            int equalsIndex = nameAndMaybeValue.IndexOf('=');
            string providedName = equalsIndex >= 0 ? nameAndMaybeValue.Substring(0, equalsIndex) : nameAndMaybeValue;
            string? inlineValue = equalsIndex >= 0 ? nameAndMaybeValue.Substring(equalsIndex + 1) : null;

            if (string.IsNullOrEmpty(providedName))
            {
                return CommandParseResult.Failed(input, ConsoleFailures.CommandParsing(
                    ConsoleFailureCodes.CommandMemberUnknown,
                    $"Unknown flag or option '{token.Text}'.",
                    source: token.Text), token.Start, token.Length);
            }

            CommandFlagDefinition? flag = FindFlag(command.Flags, providedName, comparer);
            CommandOptionDefinition? option = FindOption(command.Options, providedName, comparer);

            if (flag is null && option is null)
            {
                return CommandParseResult.Failed(input, ConsoleFailures.CommandParsing(
                    ConsoleFailureCodes.CommandMemberUnknown,
                    $"Unknown flag or option '{token.Text}'.",
                    source: providedName), token.Start, token.Length);
            }

            if (flag is not null)
            {
                if (inlineValue is not null)
                {
                    return CommandParseResult.Failed(input, ConsoleFailures.CommandParsing(
                        ConsoleFailureCodes.CommandOptionValueSyntaxInvalid,
                        $"Flag '{providedName}' cannot use an option value.",
                        source: providedName), token.Start, token.Length);
                }

                if (!seenNamedMembers.Add(flag.Name))
                {
                    return CommandParseResult.Failed(input, ConsoleFailures.CommandParsing(
                        ConsoleFailureCodes.CommandMemberDuplicate,
                        $"Flag '{flag.Name}' was provided more than once.",
                        source: flag.Name), token.Start, token.Length);
                }

                if (explicitValues.ContainsKey(flag.Id))
                {
                    return CommandParseResult.Failed(input, ConsoleFailures.CommandParsing(
                        ConsoleFailureCodes.CommandMemberDuplicate,
                        $"Value '{flag.Id}' was supplied more than once.",
                        source: flag.Id), token.Start, token.Length);
                }

                flagValues[flag.Name] = true;
                explicitValues[flag.Id] = true;
                resolvedValues[flag.Id] = true;
                tokenIndex++;
                continue;
            }

            if (option is null)
            {
                throw new InvalidOperationException("Named command member lookup produced no option.");
            }

            if (!seenNamedMembers.Add(option.Name))
            {
                return CommandParseResult.Failed(input, ConsoleFailures.CommandParsing(
                    ConsoleFailureCodes.CommandMemberDuplicate,
                    $"Option '{option.Name}' was provided more than once.",
                    source: option.Name), token.Start, token.Length);
            }

            if (explicitValues.ContainsKey(option.Id))
            {
                return CommandParseResult.Failed(input, ConsoleFailures.CommandParsing(
                    ConsoleFailureCodes.CommandMemberDuplicate,
                    $"Value '{option.Id}' was supplied through more than one input binding.", source: option.Id), token.Start, token.Length);
            }

            if (!TryReadOptionValue(input, tokens, tokenIndex, inlineValue, option, options, out string? rawValue, out int consumedTokens, out CommandParseResult? failure))
            {
                return failure ?? CommandParseResult.Failed(input, ConsoleFailures.CommandParsing(
                    ConsoleFailureCodes.CommandOptionValueMissing,
                    $"Missing value for option '{option.Name}'.",
                    source: option.Name), token.Start, token.Length);
            }

            if (!TryConvert(rawValue!, option.ValueType, options, out object? convertedOption, out string? optionMessage))
            {
                return CommandParseResult.Failed(input, ConsoleFailures.CommandBinding(
                    ConsoleFailureCodes.CommandValueInvalid,
                    $"Invalid value for option '{option.Name}': {optionMessage}",
                    source: option.Name), tokens[tokenIndex + consumedTokens - 1].Start, tokens[tokenIndex + consumedTokens - 1].Length);
            }

            optionValues.Add(option.Name, convertedOption);
            explicitValues.Add(option.Id, convertedOption);
            resolvedValues[option.Id] = convertedOption;
            tokenIndex += consumedTokens;
        }

        if (argumentIndex < command.Arguments.Count && !command.Arguments[argumentIndex].IsOptional)
        {
            CommandArgumentDefinition missingArgument = command.Arguments[argumentIndex];
            return CommandParseResult.Failed(input, ConsoleFailures.CommandParsing(
                ConsoleFailureCodes.CommandArgumentMissing,
                $"Missing required argument '{missingArgument.Name}'.",
                source: missingArgument.Name), input.Length, 0);
        }

        foreach (CommandArgumentDefinition argument in command.Arguments)
        {
            ResolveValue(command, argument.Id, argument.ValueType, argument.PropertyName, explicitValues, resolvedValues, propertyValues);
        }
        foreach (CommandOptionDefinition option in command.Options)
        {
            ResolveValue(command, option.Id, option.ValueType, option.PropertyName, explicitValues, resolvedValues, propertyValues);
        }

        foreach (CommandFlagDefinition flag in command.Flags)
        {
            propertyValues[flag.PropertyName] = resolvedValues[flag.Id];
        }

        if (!TryCreateState(command.StateType, propertyValues, options.IsCaseSensitive, out object? state, out string? stateMessage))
        {
            return CommandParseResult.Failed(input, ConsoleFailures.CommandBinding(
                ConsoleFailureCodes.CommandStateBindingFailed,
                stateMessage ?? "Could not create command state.",
                source: command.StateType?.Name));
        }

        return CommandParseResult.Succeeded(input, new BoundCommand(
            input,
            command,
            state,
            new ReadOnlyDictionary<string, object?>(resolvedValues),
            new ReadOnlyDictionary<string, object?>(argumentValues),
            new ReadOnlyDictionary<string, object?>(flagValues),
            new ReadOnlyDictionary<string, object?>(optionValues)));
    }

    private static void ResolveValue(
        CommandDefinition command, string id, Type type, string propertyName,
        IReadOnlyDictionary<string, object?> explicitValues, IDictionary<string, object?> resolvedValues,
        IDictionary<string, object?> propertyValues)
    {
        if (explicitValues.TryGetValue(id, out object? explicitValue))
        {
            resolvedValues[id] = explicitValue;
        }
        else if (command.Defaults.TryGetValue(id, out object? configuredDefault))
        {
            resolvedValues[id] = configuredDefault;
        }
        else if (!resolvedValues.ContainsKey(id))
        {
            resolvedValues[id] = GetDefaultValue(type);
        }

        propertyValues[propertyName] = resolvedValues[id];
    }

    private static bool TryReadOptionValue(
        string input,
        IReadOnlyList<CommandToken> tokens,
        int optionTokenIndex,
        string? inlineValue,
        CommandOptionDefinition option,
        CommandParsingOptions options,
        out string? value,
        out int consumedTokens,
        out CommandParseResult? failure)
    {
        value = null;
        consumedTokens = 1;
        failure = null;

        if (inlineValue is not null)
        {
            if (options.OptionValueStyle == OptionValueStyle.SpaceSeparated)
            {
                failure = CommandParseResult.Failed(
                    input,
                    ConsoleFailures.CommandParsing(
                        ConsoleFailureCodes.CommandOptionValueSyntaxInvalid,
                        $"Option '{option.Name}' does not accept '=' values with the current option value style.",
                        source: option.Name), tokens[optionTokenIndex].Start, tokens[optionTokenIndex].Length);
                return false;
            }

            value = inlineValue;
            return true;
        }

        if (options.OptionValueStyle == OptionValueStyle.EqualSeparated)
        {
            failure = CommandParseResult.Failed(
                input,
                ConsoleFailures.CommandParsing(
                    ConsoleFailureCodes.CommandOptionValueSyntaxInvalid,
                    $"Option '{option.Name}' requires '=' values with the current option value style.",
                    source: option.Name), tokens[optionTokenIndex].Start, tokens[optionTokenIndex].Length);
            return false;
        }

        int valueTokenIndex = optionTokenIndex + 1;
        if (valueTokenIndex >= tokens.Count)
        {
            return false;
        }

        CommandToken valueToken = tokens[valueTokenIndex];
        if (IsPrefixedName(valueToken, options.FlagAndOptionPrefix))
        {
            return false;
        }

        value = valueToken.Text;
        consumedTokens = 2;
        return true;
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
            if (NameMatches(flag.Name, flag.Aliases, name, comparer))
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
            if (NameMatches(option.Name, option.Aliases, name, comparer))
            {
                return option;
            }
        }

        return null;
    }

    private static bool NameMatches(string name, IReadOnlyList<string> aliases, string providedName, StringComparer comparer)
    {
        if (comparer.Equals(name, providedName))
        {
            return true;
        }

        foreach (string alias in aliases)
        {
            if (comparer.Equals(alias, providedName))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryConvert(
        string rawValue,
        Type targetType,
        CommandParsingOptions options,
        out object? value,
        out string? message)
    {
        Type conversionType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        value = null;
        message = null;

        try
        {
            if (conversionType == typeof(string))
            {
                value = rawValue;
                return true;
            }

            if (conversionType == typeof(bool))
            {
                if (TryParseBoolean(rawValue, options, out bool boolValue))
                {
                    value = boolValue;
                    return true;
                }

                message = $"expected one of: {FormatExpectedBooleanLiterals(options.BooleanLiterals)}.";
                return false;
            }

            if (conversionType.IsEnum)
            {
                value = Enum.Parse(conversionType, rawValue, ignoreCase: !options.IsCaseSensitive);
                return true;
            }

            value = Convert.ChangeType(rawValue, conversionType, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is InvalidCastException || ex is OverflowException)
        {
            message = $"expected {conversionType.Name}.";
            return false;
        }
    }

    private static bool TryCreateState(
        Type? stateType,
        IReadOnlyDictionary<string, object?> propertyValues,
        bool isCaseSensitive,
        out object? state,
        out string? message)
    {
        state = null;
        message = null;

        if (stateType is null)
        {
            return true;
        }

        StringComparer comparer = isCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        ConstructorInfo[] constructors = stateType.GetConstructors();
        foreach (ConstructorInfo constructor in constructors)
        {
            ParameterInfo[] parameters = constructor.GetParameters();
            var values = new object?[parameters.Length];
            bool matches = true;

            for (int i = 0; i < parameters.Length; i++)
            {
                string? parameterName = parameters[i].Name;
                if (parameterName is null || !TryGetPropertyValue(propertyValues, parameterName, comparer, out object? value))
                {
                    matches = false;
                    break;
                }

                values[i] = value;
            }

            if (!matches)
            {
                continue;
            }

            try
            {
                state = constructor.Invoke(values);
                return true;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is TargetInvocationException || ex is MethodAccessException)
            {
                message = $"Could not create command state '{stateType.Name}'.";
                return false;
            }
        }

        message = $"Command state '{stateType.Name}' must have a public constructor matching bound property names.";
        return false;
    }

    private static bool TryGetPropertyValue(
        IReadOnlyDictionary<string, object?> propertyValues,
        string propertyName,
        StringComparer comparer,
        out object? value)
    {
        foreach (KeyValuePair<string, object?> propertyValue in propertyValues)
        {
            if (comparer.Equals(propertyValue.Key, propertyName))
            {
                value = propertyValue.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static object? GetDefaultValue(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    private static bool TryParseBoolean(string rawValue, CommandParsingOptions options, out bool value)
    {
        StringComparer comparer = options.IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

        foreach (string literal in options.BooleanLiterals.TrueLiterals)
        {
            if (comparer.Equals(rawValue, literal))
            {
                value = true;
                return true;
            }
        }

        foreach (string literal in options.BooleanLiterals.FalseLiterals)
        {
            if (comparer.Equals(rawValue, literal))
            {
                value = false;
                return true;
            }
        }

        value = false;
        return false;
    }

    private static string FormatExpectedBooleanLiterals(BooleanLiteralOptions options)
    {
        return string.Join(", ", options.TrueLiterals) + ", " + string.Join(", ", options.FalseLiterals);
    }

    private static bool IsPrefixedName(CommandToken token, string prefix)
    {
        return !token.WasQuoted && token.Text.StartsWith(prefix, StringComparison.Ordinal);
    }

}
