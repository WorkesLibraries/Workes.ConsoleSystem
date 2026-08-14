using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using Workes.ConsoleSystem.Configuration;

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
            return CommandParseResult.Failed(input, CommandParseErrorCode.EmptyInput, "Command input cannot be blank.");
        }

        TokenizeResult tokenizeResult = Tokenize(input, options.AllowQuotedStrings);
        if (tokenizeResult.Error is not null)
        {
            return CommandParseResult.Failed(input, tokenizeResult.Error.Value, "Command input contains an unclosed quoted string.");
        }

        IReadOnlyList<Token> tokens = tokenizeResult.Tokens;
        if (tokens.Count == 0)
        {
            return CommandParseResult.Failed(input, CommandParseErrorCode.EmptyInput, "Command input cannot be blank.");
        }

        StringComparer comparer = options.IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        CommandDefinition? command = FindCommand(commands, tokens[0].Text, comparer);
        if (command is null)
        {
            return CommandParseResult.Failed(input, CommandParseErrorCode.UnknownCommand, $"Unknown command '{tokens[0].Text}'.");
        }

        return ParseCommandTokens(input, command, tokens, options, comparer);
    }

    private static CommandParseResult ParseCommandTokens(
        string input,
        CommandDefinition command,
        IReadOnlyList<Token> tokens,
        CommandParsingOptions options,
        StringComparer comparer)
    {
        var argumentValues = new Dictionary<string, object?>(StringComparer.Ordinal);
        var flagValues = new Dictionary<string, object?>(StringComparer.Ordinal);
        var optionValues = new Dictionary<string, object?>(StringComparer.Ordinal);
        var propertyValues = new Dictionary<string, object?>(comparer);
        var seenNamedMembers = new HashSet<string>(comparer);

        int tokenIndex = 1;
        foreach (CommandArgumentDefinition argument in command.Arguments)
        {
            if (tokenIndex >= tokens.Count || IsPrefixedName(tokens[tokenIndex], options.FlagAndOptionPrefix))
            {
                return CommandParseResult.Failed(input, CommandParseErrorCode.MissingArgument, $"Missing required argument '{argument.Name}'.");
            }

            if (!TryConvert(tokens[tokenIndex].Text, argument.ValueType, options.IsCaseSensitive, out object? convertedArgument, out string? message))
            {
                return CommandParseResult.Failed(input, CommandParseErrorCode.InvalidValue, $"Invalid value for argument '{argument.Name}': {message}");
            }

            argumentValues.Add(argument.Name, convertedArgument);
            propertyValues[argument.PropertyName] = convertedArgument;
            tokenIndex++;
        }

        foreach (CommandFlagDefinition flag in command.Flags)
        {
            flagValues.Add(flag.Name, false);
            propertyValues[flag.PropertyName] = false;
        }

        while (tokenIndex < tokens.Count)
        {
            Token token = tokens[tokenIndex];
            if (!IsPrefixedName(token, options.FlagAndOptionPrefix))
            {
                return CommandParseResult.Failed(input, CommandParseErrorCode.ExtraArgument, $"Unexpected positional value '{token.Text}'.");
            }

            string nameAndMaybeValue = token.Text.Substring(options.FlagAndOptionPrefix.Length);
            int equalsIndex = nameAndMaybeValue.IndexOf('=');
            string providedName = equalsIndex >= 0 ? nameAndMaybeValue.Substring(0, equalsIndex) : nameAndMaybeValue;
            string? inlineValue = equalsIndex >= 0 ? nameAndMaybeValue.Substring(equalsIndex + 1) : null;

            if (string.IsNullOrEmpty(providedName))
            {
                return CommandParseResult.Failed(input, CommandParseErrorCode.UnknownFlagOrOption, $"Unknown flag or option '{token.Text}'.");
            }

            CommandFlagDefinition? flag = FindFlag(command.Flags, providedName, comparer);
            CommandOptionDefinition? option = FindOption(command.Options, providedName, comparer);

            if (flag is null && option is null)
            {
                return CommandParseResult.Failed(input, CommandParseErrorCode.UnknownFlagOrOption, $"Unknown flag or option '{token.Text}'.");
            }

            if (flag is not null)
            {
                if (inlineValue is not null)
                {
                    return CommandParseResult.Failed(input, CommandParseErrorCode.OptionValueSyntaxNotAllowed, $"Flag '{providedName}' cannot use an option value.");
                }

                if (!seenNamedMembers.Add(flag.Name))
                {
                    return CommandParseResult.Failed(input, CommandParseErrorCode.DuplicateFlagOrOption, $"Flag '{flag.Name}' was provided more than once.");
                }

                flagValues[flag.Name] = true;
                propertyValues[flag.PropertyName] = true;
                tokenIndex++;
                continue;
            }

            if (option is null)
            {
                throw new InvalidOperationException("Named command member lookup produced no option.");
            }

            if (!seenNamedMembers.Add(option.Name))
            {
                return CommandParseResult.Failed(input, CommandParseErrorCode.DuplicateFlagOrOption, $"Option '{option.Name}' was provided more than once.");
            }

            if (!TryReadOptionValue(input, tokens, tokenIndex, inlineValue, option, options, out string? rawValue, out int consumedTokens, out CommandParseResult? failure))
            {
                return failure ?? CommandParseResult.Failed(input, CommandParseErrorCode.MissingOptionValue, $"Missing value for option '{option.Name}'.");
            }

            if (!TryConvert(rawValue!, option.ValueType, options.IsCaseSensitive, out object? convertedOption, out string? message))
            {
                return CommandParseResult.Failed(input, CommandParseErrorCode.InvalidValue, $"Invalid value for option '{option.Name}': {message}");
            }

            optionValues.Add(option.Name, convertedOption);
            propertyValues[option.PropertyName] = convertedOption;
            tokenIndex += consumedTokens;
        }

        foreach (CommandOptionDefinition option in command.Options)
        {
            if (optionValues.ContainsKey(option.Name))
            {
                continue;
            }

            object? value = option.DefaultValue is not null
                ? NormalizeDefaultValue(option.DefaultValue, option.ValueType, options.IsCaseSensitive)
                : GetDefaultValue(option.ValueType);
            optionValues.Add(option.Name, value);
            propertyValues[option.PropertyName] = value;
        }

        if (!TryCreateState(command.StateType, propertyValues, options.IsCaseSensitive, out object? state, out string? stateMessage))
        {
            return CommandParseResult.Failed(input, CommandParseErrorCode.StateBindingFailed, stateMessage ?? "Could not create command state.");
        }

        return CommandParseResult.Succeeded(input, new BoundCommand(
            command,
            state,
            new ReadOnlyDictionary<string, object?>(argumentValues),
            new ReadOnlyDictionary<string, object?>(flagValues),
            new ReadOnlyDictionary<string, object?>(optionValues)));
    }

    private static bool TryReadOptionValue(
        string input,
        IReadOnlyList<Token> tokens,
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
                    CommandParseErrorCode.OptionValueSyntaxNotAllowed,
                    $"Option '{option.Name}' does not accept '=' values with the current option value style.");
                return false;
            }

            value = inlineValue;
            return true;
        }

        if (options.OptionValueStyle == OptionValueStyle.EqualSeparated)
        {
            failure = CommandParseResult.Failed(
                input,
                CommandParseErrorCode.OptionValueSyntaxNotAllowed,
                $"Option '{option.Name}' requires '=' values with the current option value style.");
            return false;
        }

        int valueTokenIndex = optionTokenIndex + 1;
        if (valueTokenIndex >= tokens.Count)
        {
            return false;
        }

        Token valueToken = tokens[valueTokenIndex];
        if (!valueToken.WasQuoted && IsPrefixedName(valueToken, options.FlagAndOptionPrefix))
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
        bool isCaseSensitive,
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
                if (bool.TryParse(rawValue, out bool boolValue))
                {
                    value = boolValue;
                    return true;
                }

                message = "expected true or false.";
                return false;
            }

            if (conversionType.IsEnum)
            {
                value = Enum.Parse(conversionType, rawValue, ignoreCase: !isCaseSensitive);
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

    private static object? NormalizeDefaultValue(object defaultValue, Type targetType, bool isCaseSensitive)
    {
        if (defaultValue is string rawDefault)
        {
            return TryConvert(rawDefault, targetType, isCaseSensitive, out object? converted, out _)
                ? converted
                : defaultValue;
        }

        Type conversionType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        return conversionType.IsInstanceOfType(defaultValue)
            ? defaultValue
            : Convert.ChangeType(defaultValue, conversionType, CultureInfo.InvariantCulture);
    }

    private static bool IsPrefixedName(Token token, string prefix)
    {
        return token.Text.StartsWith(prefix, StringComparison.Ordinal);
    }

    private static TokenizeResult Tokenize(string input, bool allowQuotedStrings)
    {
        var tokens = new List<Token>();
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

            bool wasQuoted = false;
            char quote = input[index];
            if (allowQuotedStrings && (quote == '"' || quote == '\''))
            {
                wasQuoted = true;
                index++;
                var value = new System.Text.StringBuilder();
                bool closed = false;

                while (index < input.Length)
                {
                    char character = input[index];
                    if (character == '\\' && index + 1 < input.Length)
                    {
                        char next = input[index + 1];
                        if (next == quote || next == '\\')
                        {
                            value.Append(next);
                            index += 2;
                            continue;
                        }
                    }

                    if (character == quote)
                    {
                        closed = true;
                        index++;
                        break;
                    }

                    value.Append(character);
                    index++;
                }

                if (!closed)
                {
                    return TokenizeResult.Failed(CommandParseErrorCode.UnclosedQuote);
                }

                tokens.Add(new Token(value.ToString(), wasQuoted));
                continue;
            }

            int start = index;
            while (index < input.Length && !char.IsWhiteSpace(input[index]))
            {
                index++;
            }

            tokens.Add(new Token(input.Substring(start, index - start), wasQuoted));
        }

        return TokenizeResult.Succeeded(tokens.AsReadOnly());
    }

    private readonly struct Token
    {
        public Token(string text, bool wasQuoted)
        {
            Text = text;
            WasQuoted = wasQuoted;
        }

        public string Text { get; }

        public bool WasQuoted { get; }
    }

    private sealed class TokenizeResult
    {
        private TokenizeResult(IReadOnlyList<Token> tokens, CommandParseErrorCode? error)
        {
            Tokens = tokens;
            Error = error;
        }

        public IReadOnlyList<Token> Tokens { get; }

        public CommandParseErrorCode? Error { get; }

        public static TokenizeResult Succeeded(IReadOnlyList<Token> tokens)
        {
            return new TokenizeResult(tokens, null);
        }

        public static TokenizeResult Failed(CommandParseErrorCode error)
        {
            return new TokenizeResult(Array.Empty<Token>(), error);
        }
    }
}
