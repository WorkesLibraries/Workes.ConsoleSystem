using System;

namespace Workes.ConsoleSystem.Commands;

internal static class CommandPathValidation
{
    public static void Validate(string path)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Command paths cannot be blank.", nameof(path));
        }

        if (char.IsWhiteSpace(path[0]) || char.IsWhiteSpace(path[path.Length - 1]) || ContainsWhitespace(path))
        {
            throw new ArgumentException("Command paths cannot contain whitespace.", nameof(path));
        }

        if (path[0] == '.' || path[path.Length - 1] == '.' || path.Contains(".."))
        {
            throw new ArgumentException("Command paths cannot have leading, trailing, or empty dot segments.", nameof(path));
        }
    }

    private static bool ContainsWhitespace(string value)
    {
        foreach (char character in value)
        {
            if (char.IsWhiteSpace(character))
            {
                return true;
            }
        }

        return false;
    }
}
