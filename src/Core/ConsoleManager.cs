using System;
using System.Collections.Generic;
using System.Reflection;
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
    /// Executes command input through parsing, validation, and synchronous handler invocation.
    /// </summary>
    /// <param name="input">The command input to execute.</param>
    /// <returns>The successful command result.</returns>
    /// <exception cref="ConsoleOperationException">The command was rejected or failed during execution.</exception>
    public CommandResult ExecuteCommand(string input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        CommandExecutionOutcome outcome = ExecuteCommandCore(input);
        ThrowIfFailed(outcome);
        return outcome.Result;
    }

    /// <summary>
    /// Executes a previously parsed command through validation and synchronous handler invocation.
    /// </summary>
    /// <param name="command">The bound command to execute.</param>
    /// <returns>The successful command result.</returns>
    /// <exception cref="ConsoleOperationException">The command was rejected or failed during execution.</exception>
    public CommandResult ExecuteCommand(BoundCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        CommandExecutionOutcome outcome = ExecuteBoundCommand(command, recordCommandInput: true);
        ThrowIfFailed(outcome);
        return outcome.Result;
    }

    /// <summary>
    /// Attempts to execute command input and returns the structured result instead of throwing for command failures.
    /// </summary>
    /// <param name="input">The command input to execute.</param>
    /// <param name="result">Receives the command result.</param>
    /// <returns><see langword="true"/> when execution succeeds; otherwise, <see langword="false"/>.</returns>
    public bool TryExecuteCommand(string input, out CommandResult result)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        result = ExecuteCommandCore(input).Result;
        return result.IsSuccess;
    }

    /// <summary>
    /// Attempts to execute a previously parsed command and returns the structured result instead of throwing for command failures.
    /// </summary>
    /// <param name="command">The bound command to execute.</param>
    /// <param name="result">Receives the command result.</param>
    /// <returns><see langword="true"/> when execution succeeds; otherwise, <see langword="false"/>.</returns>
    public bool TryExecuteCommand(BoundCommand command, out CommandResult result)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        result = ExecuteBoundCommand(command, recordCommandInput: true).Result;
        return result.IsSuccess;
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

    private CommandExecutionOutcome InvokeHandler(BoundCommand command)
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        var context = new CommandContext(command.Input, command.Definition, command, timestamp);

        try
        {
            object? result = command.Definition.StateType is null
                ? ((Func<CommandContext, CommandResult>)command.Definition.Handler)(context)
                : command.Definition.Handler.DynamicInvoke(context, command.State);

            if (result is CommandResult commandResult)
            {
                return new CommandExecutionOutcome(commandResult, null);
            }

            return new CommandExecutionOutcome(
                CommandResult.Failed(ConsoleFailures.CommandExecution(
                    $"Command '{command.Definition.Path}' returned no command result.",
                    command.Definition.Path)),
                null);
        }
        catch (Exception exception)
        {
            Exception causeException = exception is TargetInvocationException { InnerException: not null } invocationException
                ? invocationException.InnerException
                : exception;
            ConsoleFailure cause = ConsoleFailure.FromException(
                causeException,
                ConsoleFailureKind.CommandExecution,
                ConsoleFailureCodes.CommandExecutionRejected);
            ConsoleFailure failure = ConsoleFailures.CommandExecution(
                $"Command '{command.Definition.Path}' failed during execution.",
                command.Definition.Path,
                cause);
            return new CommandExecutionOutcome(CommandResult.Failed(failure), causeException);
        }
    }

    private CommandExecutionOutcome ExecuteCommandCore(string input)
    {
        RecordCommandInput(input);
        CommandParseResult parse = ParseCommand(input);
        if (!parse.IsSuccess)
        {
            AppendEcho(input, null);
            return Fail(parse.Failure!);
        }

        return ExecuteBoundCommand(parse.Command!, recordCommandInput: false);
    }

    private CommandExecutionOutcome ExecuteBoundCommand(BoundCommand command, bool recordCommandInput)
    {
        if (recordCommandInput)
        {
            RecordCommandInput(command.Input);
        }

        AppendEcho(command.Input, command.Definition);

        CommandValidationResult validation = ValidateCommand(command);
        if (!validation.IsSuccess)
        {
            return Fail(validation.Failure!);
        }

        CommandExecutionOutcome outcome = InvokeHandler(command);
        if (!outcome.Result.IsSuccess)
        {
            AppendFailure(outcome.Result.Failure!);
            return outcome;
        }

        AppendSuccessOutputs(command.Definition, outcome.Result);
        return outcome;
    }

    private static void ThrowIfFailed(CommandExecutionOutcome outcome)
    {
        if (!outcome.Result.IsSuccess)
        {
            throw new ConsoleOperationException(outcome.Result.Failure!, outcome.HandlerException);
        }
    }

    private CommandExecutionOutcome Fail(ConsoleFailure failure)
    {
        CommandResult result = CommandResult.Failed(failure);
        AppendFailure(failure);
        return new CommandExecutionOutcome(result, null);
    }

    private void AppendFailure(ConsoleFailure failure)
    {
        History.RecordEntry(new CommandFailureEntry(DateTimeOffset.UtcNow, failure));
    }

    private void AppendSuccessOutputs(CommandDefinition command, CommandResult result)
    {
        foreach (CommandSuccessOutputDefinition output in command.SuccessOutputs)
        {
            History.RecordEntry(new CommandOutputEntry(DateTimeOffset.UtcNow, output.Output));
        }

        foreach (CommandOutput output in result.Outputs)
        {
            History.RecordEntry(new CommandOutputEntry(DateTimeOffset.UtcNow, output));
        }
    }

    private void AppendEcho(string input, CommandDefinition? command)
    {
        bool shouldEcho = command?.EchoInput.EchoInput ?? _options.Execution.EchoInput;
        if (shouldEcho && !string.IsNullOrWhiteSpace(input))
        {
            History.RecordEntry(new CommandInputEntry(DateTimeOffset.UtcNow, ConsoleText.Plain(input)));
        }
    }

    private readonly struct CommandExecutionOutcome
    {
        public CommandExecutionOutcome(CommandResult result, Exception? handlerException)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
            HandlerException = handlerException;
        }

        public CommandResult Result { get; }

        public Exception? HandlerException { get; }
    }
}
