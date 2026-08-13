namespace Workes.ConsoleSystem.Configuration;

/// <summary>
/// Configures presentation extension points for future formatting and theming behavior.
/// </summary>
public sealed class PresentationOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PresentationOptions"/> class.
    /// </summary>
    public PresentationOptions()
    {
    }

    private PresentationOptions(PresentationOptions source)
    {
        Theme = source.Theme;
        Formatter = source.Formatter;
    }

    /// <summary>
    /// Gets or sets the optional presentation theme object used by future formatting behavior.
    /// </summary>
    public object? Theme { get; set; }

    /// <summary>
    /// Gets or sets the optional presentation formatter object used by future formatting behavior.
    /// </summary>
    public object? Formatter { get; set; }

    internal static PresentationOptions CreateSnapshot(PresentationOptions? source)
    {
        return source is null ? new PresentationOptions() : new PresentationOptions(source);
    }
}
