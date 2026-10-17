using System;
using System.Collections.Generic;
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Entries;
using Workes.ConsoleSystem.History;
using Workes.ConsoleSystem.Logging;
using Workes.ConsoleSystem.Presentation;
using Workes.ContentSystem.Core;

namespace Workes.ConsoleSystem.Core;

/// <summary>
/// Provides the root configuration and coordination point for one console instance.
/// </summary>
public sealed class ConsoleManager
{
    private readonly ConsoleManagerOptions _options;
    private readonly StringComparer _commandPathComparer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleManager"/> class with default options.
    /// </summary>
    public ConsoleManager()
        : this(new ConsoleManagerOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleManager"/> class.
    /// </summary>
    /// <param name="options">The setup options to snapshot for this console instance.</param>
    public ConsoleManager(ConsoleManagerOptions options)
    {
        _options = ConsoleManagerOptions.CreateSnapshot(options);
        _commandPathComparer = _options.CommandParsing.IsCaseSensitive
            ? StringComparer.Ordinal
            : StringComparer.OrdinalIgnoreCase;

        var structure = new ContentSequenceStructure(
            ContentOverflowPolicy.DropOldest(_options.History.ConsoleHistoryCapacity),
            ContentSequenceReadOrder.OldestFirst);
        ContentSequence = new ContentSequenceManager(structure);
        History = new ConsoleHistory(ContentSequence, _options.History.ConsoleHistoryCapacity);
        CommandHistory = new CommandHistory(
            _options.History.CommandHistoryCapacity,
            _options.History.CommandHistoryDuplicatePolicy);
        Log = new ConsoleLog(History);
        Commands = new CommandSystem();
    }

    /// <summary>
    /// Gets a detached snapshot of the resolved options for this console instance.
    /// </summary>
    public ConsoleManagerOptions Options => ConsoleManagerOptions.CreateSnapshot(_options);

    /// <summary>
    /// Gets the shared chronological console history.
    /// </summary>
    public ConsoleHistory History { get; }

    /// <summary>
    /// Gets the retained command input strings used by console UI navigation.
    /// </summary>
    public CommandHistory CommandHistory { get; }

    /// <summary>
    /// Gets the developer-facing logging facade used by this console manager.
    /// </summary>
    public ConsoleLog Log { get; }

    /// <summary>
    /// Gets the registered command definitions. Registration remains owned by this manager.
    /// </summary>
    public CommandSystem Commands { get; }

    /// <summary>
    /// Parses command input against registered command definitions without executing handlers.
    /// </summary>
    /// <param name="input">The command input to parse.</param>
    /// <returns>The parsed command or structured parse failure.</returns>
    public CommandParseResult ParseCommand(string input)
    {
        return CommandParser.Parse(input, Commands.Definitions, _options.CommandParsing);
    }

    /// <summary>
    /// Validates a parsed command against its configured ranges, allowed values, and custom constraints.
    /// </summary>
    /// <remarks>Validation does not execute the command or change console history.</remarks>
    /// <param name="command">The parsed command to validate.</param>
    /// <returns>The validation result.</returns>
    public CommandValidationResult ValidateCommand(BoundCommand command)
    {
        return CommandValidator.Validate(command);
    }

    /// <summary>
    /// Records a console entry in the shared visible history.
    /// </summary>
    /// <param name="entry">The console entry to record.</param>
    public void RecordEntry(IConsoleEntry entry)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        History.RecordEntry(entry);
    }

    /// <summary>
    /// Records a submitted command input for console UI navigation.
    /// </summary>
    /// <param name="input">The original submitted command input.</param>
    /// <returns><see langword="true"/> when the input was retained; otherwise, <see langword="false"/>.</returns>
    public bool RecordCommandInput(string input) => CommandHistory.Add(input);

    /// <summary>
    /// Registers one command definition.
    /// </summary>
    /// <param name="command">The command definition to register.</param>
    /// <returns>The registered command definition.</returns>
    public CommandDefinition RegisterCommand(CommandDefinition command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        ValidateCommandMembers(command);
        ValidateCommandIsNotDuplicate(command.Path, Commands.Definitions);
        Commands.Add(command);
        return command;
    }

    /// <summary>
    /// Registers multiple command definitions atomically.
    /// </summary>
    /// <param name="commands">The command definitions to register.</param>
    /// <returns>The registered definitions in their original order.</returns>
    public IReadOnlyList<CommandDefinition> RegisterCommands(IEnumerable<CommandDefinition> commands)
    {
        if (commands is null)
        {
            throw new ArgumentNullException(nameof(commands));
        }

        var commandList = new List<CommandDefinition>();
        foreach (CommandDefinition command in commands)
        {
            if (command is null)
            {
                throw new ArgumentException("Command batches cannot contain null commands.", nameof(commands));
            }

            commandList.Add(command);
        }

        ValidateBatch(commandList);

        foreach (CommandDefinition command in commandList)
        {
            Commands.Add(command);
        }

        return commandList.AsReadOnly();
    }

    /// <summary>
    /// Adds a trace log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void LogTrace(string message) => Log.Trace(message);

    /// <summary>
    /// Adds trace log content to the shared console history.
    /// </summary>
    /// <param name="content">The log content.</param>
    public void LogTrace(ConsoleText content) => Log.Trace(content);

    /// <summary>
    /// Adds a debug log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void LogDebug(string message) => Log.Debug(message);

    /// <summary>
    /// Adds debug log content to the shared console history.
    /// </summary>
    /// <param name="content">The log content.</param>
    public void LogDebug(ConsoleText content) => Log.Debug(content);

    /// <summary>
    /// Adds an informational log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void LogInformation(string message) => Log.Information(message);

    /// <summary>
    /// Adds informational log content to the shared console history.
    /// </summary>
    /// <param name="content">The log content.</param>
    public void LogInformation(ConsoleText content) => Log.Information(content);

    /// <summary>
    /// Adds a warning log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void LogWarning(string message) => Log.Warning(message);

    /// <summary>
    /// Adds warning log content to the shared console history.
    /// </summary>
    /// <param name="content">The log content.</param>
    public void LogWarning(ConsoleText content) => Log.Warning(content);

    /// <summary>
    /// Adds an error log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void LogError(string message) => Log.Error(message);

    /// <summary>
    /// Adds error log content to the shared console history.
    /// </summary>
    /// <param name="content">The log content.</param>
    public void LogError(ConsoleText content) => Log.Error(content);

    /// <summary>
    /// Adds a critical log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void LogCritical(string message) => Log.Critical(message);

    /// <summary>
    /// Adds critical log content to the shared console history.
    /// </summary>
    /// <param name="content">The log content.</param>
    public void LogCritical(ConsoleText content) => Log.Critical(content);

    /// <summary>
    /// Gets the internal store used by the console history facade.
    /// </summary>
    internal ContentSequenceManager ContentSequence { get; }

    private void ValidateBatch(IReadOnlyList<CommandDefinition> commands)
    {
        foreach (CommandDefinition command in commands)
        {
            ValidateCommandMembers(command);
            ValidateCommandIsNotDuplicate(command.Path, Commands.Definitions);
        }

        var paths = new HashSet<string>(_commandPathComparer);
        foreach (CommandDefinition command in commands)
        {
            if (!paths.Add(command.Path))
            {
                throw new InvalidOperationException($"Duplicate command path '{command.Path}' in registration batch.");
            }
        }
    }

    private void ValidateCommandIsNotDuplicate(string path, IReadOnlyList<CommandDefinition> existingCommands)
    {
        foreach (CommandDefinition existingCommand in existingCommands)
        {
            if (_commandPathComparer.Equals(existingCommand.Path, path))
            {
                throw new InvalidOperationException($"A command with path '{path}' is already registered.");
            }
        }
    }

    private void ValidateCommandMembers(CommandDefinition command)
    {
        string prefix = _options.CommandParsing.FlagAndOptionPrefix;
        var seenNamedMembers = new HashSet<string>(_commandPathComparer);

        foreach (CommandFlagDefinition flag in command.Flags)
        {
            ValidateNameDoesNotIncludePrefix(flag.Name, prefix);
            AddUniqueCommandMemberName(seenNamedMembers, flag.Name);
            foreach (string alias in flag.Aliases)
            {
                ValidateNameDoesNotIncludePrefix(alias, prefix);
                AddUniqueCommandMemberName(seenNamedMembers, alias);
            }
        }

        foreach (CommandOptionDefinition option in command.Options)
        {
            ValidateNameDoesNotIncludePrefix(option.Name, prefix);
            AddUniqueCommandMemberName(seenNamedMembers, option.Name);
            foreach (string alias in option.Aliases)
            {
                ValidateNameDoesNotIncludePrefix(alias, prefix);
                AddUniqueCommandMemberName(seenNamedMembers, alias);
            }
        }
    }

    private static void ValidateNameDoesNotIncludePrefix(string name, string prefix)
    {
        if (name.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Flag and option schema names should not include the configured prefix '{prefix}'. Use '{name.Substring(prefix.Length)}' instead.");
        }
    }

    private static void AddUniqueCommandMemberName(HashSet<string> seenNamedMembers, string name)
    {
        if (!seenNamedMembers.Add(name))
        {
            throw new InvalidOperationException($"Duplicate flag or option name or alias '{name}' for the configured command parsing comparer.");
        }
    }
}
