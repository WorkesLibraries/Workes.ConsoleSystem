using System;
using System.Collections.Generic;

namespace Workes.ConsoleSystem.Permissions;

internal static class PermissionPath
{
    public static string ValidateName(string value, string parameterName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (string.IsNullOrWhiteSpace(value) || value != value.Trim() || value.Contains('*') ||
            value.StartsWith(".", StringComparison.Ordinal) || value.EndsWith(".", StringComparison.Ordinal) ||
            value.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException("Permission names must be dot-separated non-empty segments and cannot contain wildcards.", parameterName);
        }

        foreach (char character in value)
        {
            if (char.IsWhiteSpace(character))
            {
                throw new ArgumentException("Permission names cannot contain whitespace.", parameterName);
            }
        }

        return value;
    }

    public static string ValidateRule(string value, string parameterName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (value.EndsWith(".*", StringComparison.Ordinal))
        {
            string prefix = value.Substring(0, value.Length - 2);
            ValidateName(prefix, parameterName);
            return value;
        }

        return ValidateName(value, parameterName);
    }

    public static bool HasMatchingRule(IEnumerable<string> rules, string permission)
    {
        foreach (string rule in rules)
        {
            if (string.Equals(rule, permission, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (rule.EndsWith(".*", StringComparison.Ordinal))
            {
                string prefix = rule.Substring(0, rule.Length - 1);
                if (permission.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
