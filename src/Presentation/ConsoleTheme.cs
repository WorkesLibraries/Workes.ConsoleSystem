using System;
using System.Collections.Generic;

namespace Workes.ConsoleSystem.Presentation;

/// <summary>
/// Maps semantic style identifiers to console styles.
/// </summary>
public sealed class ConsoleTheme
{
    private readonly IReadOnlyDictionary<string, ConsoleStyle> _styles;

    /// <summary>
    /// Initializes a new empty theme.
    /// </summary>
    public ConsoleTheme()
        : this(new Dictionary<string, ConsoleStyle>())
    {
    }

    /// <summary>
    /// Initializes a new theme from a style map.
    /// </summary>
    public ConsoleTheme(IDictionary<string, ConsoleStyle> styles)
    {
        if (styles is null)
        {
            throw new ArgumentNullException(nameof(styles));
        }

        var copiedStyles = new Dictionary<string, ConsoleStyle>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, ConsoleStyle> style in styles)
        {
            if (string.IsNullOrWhiteSpace(style.Key))
            {
                throw new ArgumentException("Theme style identifiers cannot be empty.", nameof(styles));
            }

            if (style.Value is null)
            {
                throw new ArgumentException("Theme style values cannot be null.", nameof(styles));
            }

            copiedStyles.Add(style.Key, style.Value);
        }

        _styles = copiedStyles;
    }

    /// <summary>
    /// Gets the configured styles.
    /// </summary>
    public IReadOnlyDictionary<string, ConsoleStyle> Styles => _styles;

    /// <summary>
    /// Attempts to get a style by identifier.
    /// </summary>
    public bool TryGetStyle(string styleId, out ConsoleStyle? style)
    {
        if (string.IsNullOrWhiteSpace(styleId))
        {
            throw new ArgumentException("Style identifiers cannot be empty.", nameof(styleId));
        }

        return _styles.TryGetValue(styleId, out style);
    }

    /// <summary>
    /// Validates all theme styles against a format model.
    /// </summary>
    public void ValidateAgainst(ConsoleFormatModel model)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        foreach (ConsoleStyle style in _styles.Values)
        {
            style.ValidateAgainst(model);
        }
    }
}
