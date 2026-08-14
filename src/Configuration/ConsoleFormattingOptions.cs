using System;
using Workes.ConsoleSystem.Presentation;

namespace Workes.ConsoleSystem.Configuration;

/// <summary>
/// Configures the optional console formatting subsystem.
/// </summary>
public sealed class ConsoleFormattingOptions
{
    /// <summary>
    /// Initializes a new disabled formatting options instance.
    /// </summary>
    public ConsoleFormattingOptions()
    {
    }

    private ConsoleFormattingOptions(ConsoleFormattingOptions source)
    {
        Model = source.Model;
        MarkupProfile = source.MarkupProfile;
        Theme = source.Theme;
        Formatter = source.Formatter;
    }

    private ConsoleFormattingOptions(
        ConsoleFormatModel? model,
        ConsoleMarkupProfile? markupProfile,
        ConsoleTheme? theme,
        IConsoleTextFormatter? formatter)
    {
        Model = model;
        MarkupProfile = markupProfile;
        Theme = theme;
        Formatter = formatter;
    }

    /// <summary>
    /// Gets disabled formatting options.
    /// </summary>
    public static ConsoleFormattingOptions Disabled => new ConsoleFormattingOptions();

    /// <summary>
    /// Gets a value indicating whether formatting is enabled.
    /// </summary>
    public bool IsEnabled => Formatter is not null;

    /// <summary>
    /// Gets or sets the active format model.
    /// </summary>
    public ConsoleFormatModel? Model { get; set; }

    /// <summary>
    /// Gets or sets the markup profile.
    /// </summary>
    public ConsoleMarkupProfile? MarkupProfile { get; set; }

    /// <summary>
    /// Gets or sets the active theme.
    /// </summary>
    public ConsoleTheme? Theme { get; set; }

    /// <summary>
    /// Gets or sets the active formatter.
    /// </summary>
    public IConsoleTextFormatter? Formatter { get; set; }

    /// <summary>
    /// Creates options for Unity rich text formatting.
    /// </summary>
    public static ConsoleFormattingOptions UnityRichText(ConsoleTheme? theme = null)
    {
        return Standard(new UnityRichTextConsoleFormatter(), theme);
    }

    /// <summary>
    /// Creates options for Godot BBCode formatting.
    /// </summary>
    public static ConsoleFormattingOptions GodotBbCode(ConsoleTheme? theme = null)
    {
        return Standard(new GodotBbCodeConsoleFormatter(), theme);
    }

    /// <summary>
    /// Creates enabled options for the standard model and markup profile.
    /// </summary>
    public static ConsoleFormattingOptions Standard(IConsoleTextFormatter formatter, ConsoleTheme? theme = null)
    {
        return new ConsoleFormattingOptions(
            ConsoleFormatModel.Standard,
            ConsoleMarkupProfile.Standard,
            theme ?? new ConsoleTheme(),
            formatter ?? throw new ArgumentNullException(nameof(formatter)));
    }

    /// <summary>
    /// Creates enabled options for a custom formatting setup.
    /// </summary>
    public static ConsoleFormattingOptions Custom(
        ConsoleFormatModel model,
        ConsoleMarkupProfile markupProfile,
        ConsoleTheme theme,
        IConsoleTextFormatter formatter)
    {
        return new ConsoleFormattingOptions(
            model ?? throw new ArgumentNullException(nameof(model)),
            markupProfile ?? throw new ArgumentNullException(nameof(markupProfile)),
            theme ?? throw new ArgumentNullException(nameof(theme)),
            formatter ?? throw new ArgumentNullException(nameof(formatter)));
    }

    internal static ConsoleFormattingOptions CreateSnapshot(ConsoleFormattingOptions? source)
    {
        var snapshot = source is null ? new ConsoleFormattingOptions() : new ConsoleFormattingOptions(source);
        if (!snapshot.IsEnabled)
        {
            return new ConsoleFormattingOptions();
        }

        snapshot.Model ??= ConsoleFormatModel.Standard;
        snapshot.MarkupProfile ??= ConsoleMarkupProfile.Standard;
        snapshot.Theme ??= new ConsoleTheme();
        if (snapshot.Formatter is null)
        {
            throw new ArgumentException("Enabled formatting options require a formatter.", nameof(Formatter));
        }

        snapshot.Theme.ValidateAgainst(snapshot.Model);
        snapshot.MarkupProfile.ValidateAgainst(snapshot.Model);
        return snapshot;
    }

    internal ConsoleFormattingContext CreateContext()
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException("The console formatting system is disabled.");
        }

        return new ConsoleFormattingContext(Model!, Theme!);
    }
}
