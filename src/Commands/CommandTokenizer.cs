using System;
using System.Collections.Generic;
using Workes.ConsoleSystem.Core;

namespace Workes.ConsoleSystem.Commands;

internal static class CommandTokenizer
{
    public static CommandTokenizationResult Tokenize(string input, bool allowQuotedStrings, bool allowIncompleteQuotes)
    {
        var tokens = new List<CommandToken>();
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
            if (allowQuotedStrings && (quote == '"' || quote == '\''))
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
                if (!closed && !allowIncompleteQuotes)
                {
                    return CommandTokenizationResult.Failed(
                        ConsoleFailures.CommandParsing(
                            ConsoleFailureCodes.CommandQuoteUnclosed,
                            "Command input contains an unclosed quoted string."),
                        start,
                        input.Length - start);
                }

                if (closed)
                {
                    index++;
                }

                tokens.Add(new CommandToken(input, tokens.Count, start, index, contentStart, contentEnd, text.ToString(), wasQuoted: true));
                continue;
            }

            while (index < input.Length && !char.IsWhiteSpace(input[index]))
            {
                index++;
            }

            tokens.Add(new CommandToken(
                input,
                tokens.Count,
                start,
                index,
                start,
                index,
                input.Substring(start, index - start),
                wasQuoted: false));
        }

        return CommandTokenizationResult.Succeeded(tokens.AsReadOnly());
    }
}

internal readonly struct CommandToken
{
    public CommandToken(
        string input,
        int index,
        int start,
        int end,
        int contentStart,
        int contentEnd,
        string text,
        bool wasQuoted)
    {
        Input = input;
        Index = index;
        Start = start;
        End = end;
        ContentStart = contentStart;
        ContentEnd = contentEnd;
        Text = text;
        WasQuoted = wasQuoted;
    }

    private string Input { get; }

    public int Index { get; }

    public int Start { get; }

    public int End { get; }

    public int Length => End - Start;

    public int ContentStart { get; }

    public int ContentEnd { get; }

    public string Text { get; }

    public bool WasQuoted { get; }

    public string TextBefore(int cursorIndex)
    {
        int end = Math.Max(ContentStart, Math.Min(cursorIndex, ContentEnd));
        if (!WasQuoted)
        {
            return Input.Substring(ContentStart, end - ContentStart);
        }

        var value = new System.Text.StringBuilder();
        int index = ContentStart;
        while (index < end)
        {
            char character = Input[index];
            if (character == '\\' && index + 1 < end)
            {
                char next = Input[index + 1];
                if (next == Input[Start] || next == '\\')
                {
                    value.Append(next);
                    index += 2;
                    continue;
                }
            }

            value.Append(character);
            index++;
        }

        return value.ToString();
    }
}

internal sealed class CommandTokenizationResult
{
    private CommandTokenizationResult(
        IReadOnlyList<CommandToken> tokens,
        ConsoleFailure? failure,
        int? failureOffset,
        int? failureLength)
    {
        Tokens = tokens;
        Failure = failure;
        FailureOffset = failureOffset;
        FailureLength = failureLength;
    }

    public IReadOnlyList<CommandToken> Tokens { get; }

    public ConsoleFailure? Failure { get; }

    public int? FailureOffset { get; }

    public int? FailureLength { get; }

    public static CommandTokenizationResult Succeeded(IReadOnlyList<CommandToken> tokens)
    {
        return new CommandTokenizationResult(tokens, null, null, null);
    }

    public static CommandTokenizationResult Failed(ConsoleFailure failure, int offset, int length)
    {
        return new CommandTokenizationResult(Array.Empty<CommandToken>(), failure, offset, length);
    }
}
