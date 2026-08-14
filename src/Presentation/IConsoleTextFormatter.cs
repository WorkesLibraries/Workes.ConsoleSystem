namespace Workes.ConsoleSystem.Presentation;

/// <summary>
/// Formats semantic console text.
/// </summary>
public interface IConsoleTextFormatter
{
    /// <summary>
    /// Formats console text.
    /// </summary>
    string Format(ConsoleText text, ConsoleFormattingContext context);
}
