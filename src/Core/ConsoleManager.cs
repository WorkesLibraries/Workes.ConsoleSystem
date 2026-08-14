using System;
using System.Collections.Generic;
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.History;
using Workes.ConsoleSystem.Logging;

namespace Workes.ConsoleSystem.Core;

/// <summary>
/// Coordinates the shared console history, logging facade, command input history, and command system.
/// </summary>
/// <remarks>
/// Normal applications are expected to create one instance during startup and retain it for the
/// lifetime of the game, though the type does not enforce singleton usage.
/// </remarks>
public sealed class ConsoleManager
{
    private readonly ConsoleManagerOptions _options;
    private readonly StringComparer _commandPathComparer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleManager"/> class.
    /// </summary>
    public ConsoleManager()
        : this(new ConsoleManagerOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleManager"/> class.
    /// </summary>
    /// <param name="options">The manager options.</param>
    public ConsoleManager(ConsoleManagerOptions options)
    {
        _options = ConsoleManagerOptions.CreateSnapshot(options);
        _commandPathComparer = _options.CommandParsing.IsCaseSensitive
            ? StringComparer.Ordinal
            : StringComparer.OrdinalIgnoreCase;

        History = new ConsoleHistory(_options.History.ConsoleHistoryCapacity);
        CommandHistory = new CommandHistory(
            _options.History.CommandHistoryCapacity,
            _options.History.CommandHistoryDuplicatePolicy);
        Log = new ConsoleLog(History);
        Commands = new CommandSystem();
    }

    /// <summary>
    /// Gets the resolved manager options.
    /// </summary>
    public ConsoleManagerOptions Options => ConsoleManagerOptions.CreateSnapshot(_options);

    /// <summary>
    /// Gets the shared chronological console history.
    /// </summary>
    public ConsoleHistory History { get; }

    /// <summary>
    /// Gets the interactive command input history used by console UIs.
    /// </summary>
    public CommandHistory CommandHistory { get; }

    /// <summary>
    /// Gets the developer-facing logging facade.
    /// </summary>
    public ConsoleLog Log { get; }

    /// <summary>
    /// Gets the command registry and future execution surface.
    /// </summary>
    public CommandSystem Commands { get; }

    /// <summary>
    /// Parses command input against the registered command schemas without executing the command.
    /// </summary>
    /// <param name="input">The command input.</param>
    /// <returns>The parse result.</returns>
    public CommandParseResult ParseCommand(string input)
    {
        return CommandParser.Parse(input, Commands.Definitions, _options.CommandParsing);
    }

    /// <summary>
    /// Registers a command definition.
    /// </summary>
    /// <param name="command">The command definition.</param>
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
    /// <param name="commands">The command definitions.</param>
    /// <returns>The registered command definitions.</returns>
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
    /// Adds submitted command input to command history.
    /// </summary>
    /// <param name="input">The submitted command input.</param>
    /// <returns><c>true</c> when the input was retained; otherwise, <c>false</c>.</returns>
    public bool RecordCommandInput(string input)
    {
        return CommandHistory.Add(input);
    }

    /// <summary>
    /// Adds a trace log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void LogTrace(string message)
    {
        Log.Trace(message);
    }

    /// <summary>
    /// Adds a debug log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void LogDebug(string message)
    {
        Log.Debug(message);
    }

    /// <summary>
    /// Adds an informational log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void LogInformation(string message)
    {
        Log.Information(message);
    }

    /// <summary>
    /// Adds a warning log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void LogWarning(string message)
    {
        Log.Warning(message);
    }

    /// <summary>
    /// Adds an error log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void LogError(string message)
    {
        Log.Error(message);
    }

    /// <summary>
    /// Adds a critical log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void LogCritical(string message)
    {
        Log.Critical(message);
    }

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
