using System;

namespace Workes.ConsoleSystem.Presentation;

/// <summary>
/// Provides the model and theme used when formatting console text.
/// </summary>
public sealed class ConsoleFormattingContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleFormattingContext"/> class.
    /// </summary>
    public ConsoleFormattingContext(ConsoleFormatModel model, ConsoleTheme theme)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        Theme = theme ?? throw new ArgumentNullException(nameof(theme));
        Theme.ValidateAgainst(Model);
    }

    /// <summary>
    /// Gets the active format model.
    /// </summary>
    public ConsoleFormatModel Model { get; }

    /// <summary>
    /// Gets the active theme.
    /// </summary>
    public ConsoleTheme Theme { get; }
}
