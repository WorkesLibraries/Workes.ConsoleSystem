using System;
using System.Collections.Generic;

namespace Workes.ConsoleSystem.Configuration;

/// <summary>
/// Configures the accepted input literals for boolean option values.
/// </summary>
public sealed class BooleanLiteralOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BooleanLiteralOptions"/> class.
    /// </summary>
    public BooleanLiteralOptions()
    {
    }

    private BooleanLiteralOptions(BooleanLiteralOptions source, bool isCaseSensitive)
    {
        TrueLiterals = CopyLiterals(source.TrueLiterals, nameof(TrueLiterals));
        FalseLiterals = CopyLiterals(source.FalseLiterals, nameof(FalseLiterals));
        ValidateNoOverlap(TrueLiterals, FalseLiterals, isCaseSensitive);
    }

    /// <summary>
    /// Gets or sets the literals accepted as <see langword="true" />.
    /// </summary>
    public string[] TrueLiterals { get; set; } = new[] { "true" };

    /// <summary>
    /// Gets or sets the literals accepted as <see langword="false" />.
    /// </summary>
    public string[] FalseLiterals { get; set; } = new[] { "false" };

    internal static BooleanLiteralOptions CreateSnapshot(BooleanLiteralOptions? source, bool isCaseSensitive)
    {
        return source is null ? new BooleanLiteralOptions() : new BooleanLiteralOptions(source, isCaseSensitive);
    }

    private static string[] CopyLiterals(string[] literals, string parameterName)
    {
        if (literals is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (literals.Length == 0)
        {
            throw new ArgumentException("Boolean literal lists cannot be empty.", parameterName);
        }

        var copy = new string[literals.Length];
        for (int i = 0; i < literals.Length; i++)
        {
            string literal = literals[i];
            if (string.IsNullOrWhiteSpace(literal))
            {
                throw new ArgumentException("Boolean literals cannot be null, empty, or whitespace.", parameterName);
            }

            copy[i] = literal;
        }

        return copy;
    }

    private static void ValidateNoOverlap(string[] trueLiterals, string[] falseLiterals, bool isCaseSensitive)
    {
        StringComparer comparer = isCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        var seen = new HashSet<string>(comparer);

        foreach (string literal in trueLiterals)
        {
            if (!seen.Add(literal))
            {
                throw new ArgumentException($"Duplicate boolean literal '{literal}'.", nameof(TrueLiterals));
            }
        }

        foreach (string literal in falseLiterals)
        {
            if (!seen.Add(literal))
            {
                throw new ArgumentException($"Boolean literal '{literal}' cannot be both true and false.", nameof(FalseLiterals));
            }
        }
    }
}
