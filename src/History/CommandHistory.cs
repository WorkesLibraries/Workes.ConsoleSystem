using System.Collections.Generic;

namespace Workes.ConsoleSystem.History;

/// <summary>
/// Represents previously submitted interactive command strings for UI navigation.
/// </summary>
public sealed class CommandHistory
{
    private readonly List<string> _entries = new List<string>();

    /// <summary>
    /// Gets the submitted command strings.
    /// </summary>
    public IReadOnlyList<string> Entries => _entries;

    internal void Add(string input)
    {
        _entries.Add(input);
    }
}
