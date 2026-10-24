using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Entries;
using Workes.ConsoleSystem.History;
using Workes.ConsoleSystem.Logging;
using Workes.ConsoleSystem.Presentation;
using Workes.ConsoleSystem.Permissions;
using Workes.ContentSystem.Core;

namespace Workes.ConsoleSystem.Core;

/// <summary>
/// Provides the root configuration and coordination point for one console instance.
/// </summary>
public sealed class ConsoleManager
{
    private readonly ConsoleManagerOptions _options;
    private readonly IConsolePermissionProvider? _permissionProvider;
    private readonly ICommandDispatcher? _dispatcher;
    private readonly StringComparer _commandPathComparer;
    private readonly ConsoleLog _log;

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
        : this(options, permissionProvider: null)
    {
    }

    /// <summary>
    /// Initializes a new instance with options and the permission provider for this console's caller.
    /// </summary>
    /// <param name="options">The setup options to snapshot for this console instance.</param>
    /// <param name="permissionProvider">The optional caller permission provider.</param>
    public ConsoleManager(ConsoleManagerOptions options, IConsolePermissionProvider? permissionProvider)
        : this(options, permissionProvider, dispatcher: null)
    {
    }

    /// <summary>
    /// Initializes a new instance with options, the permission provider, and an optional host dispatcher.
    /// </summary>
    /// <param name="options">The setup options to snapshot for this console instance.</param>
    /// <param name="permissionProvider">The optional caller permission provider.</param>
    /// <param name="dispatcher">The optional host dispatcher used explicitly by command handlers.</param>
    public ConsoleManager(
        ConsoleManagerOptions options,
        IConsolePermissionProvider? permissionProvider,
        ICommandDispatcher? dispatcher)
    {
        _options = ConsoleManagerOptions.CreateSnapshot(options);
        _permissionProvider = permissionProvider;
        _dispatcher = dispatcher;
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
        _log = new ConsoleLog(History);
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
    /// Occurs after the shared console history changes.
    /// </summary>
    public event EventHandler<ConsoleHistoryChangedEventArgs>? ConsoleHistoryChanged
    {
        add => History.Changed += value;
        remove => History.Changed -= value;
    }

    /// <summary>
    /// Occurs after command input history changes.
    /// </summary>
    public event EventHandler<CommandHistoryChangedEventArgs>? CommandHistoryChanged
    {
        add => CommandHistory.Changed += value;
        remove => CommandHistory.Changed -= value;
    }

    /// <summary>
    /// Occurs when a command reports progress and when a command that reported progress finishes.
    /// </summary>
    /// <remarks>Notifications are synchronous and run on the thread that reports progress or completes the command.</remarks>
    public event EventHandler<CommandProgressChangedEventArgs>? CommandProgressChanged;

    /// <summary>
    /// Gets the registered command definitions. Registration remains owned by this manager.
    /// </summary>
    public CommandSystem Commands { get; }

    /// <summary>
    /// Gets schema-driven autocomplete candidates for command input at the specified cursor position.
    /// </summary>
    /// <param name="input">The current command input.</param>
    /// <param name="cursorIndex">The zero-based cursor index in the input string.</param>
    /// <returns>The matching candidates and the input range they replace.</returns>
    /// <exception cref="ArgumentNullException">The input is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The cursor index is outside the input string.</exception>
    public CommandAutocompleteResult GetAutocomplete(string input, int cursorIndex)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        if (cursorIndex < 0 || cursorIndex > input.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(cursorIndex), "Cursor index must be inside the input.");
        }

        var visibleCommands = new List<CommandDefinition>();
        var accessResults = new Dictionary<CommandDefinition, CommandAvailabilityResult>();
        foreach (CommandDefinition definition in Commands.Definitions)
        {
            CommandAvailabilityResult access = GetCommandAccessStatus(
                definition,
                new CommandAvailabilityContext(input, cursorIndex, definition, command: null));
            accessResults.Add(definition, access);
            if (_options.Autocomplete.ShowUnavailableCommands || access.IsAvailable)
            {
                visibleCommands.Add(definition);
            }
        }

        return CommandAutocomplete.GetAutocomplete(
            input,
            cursorIndex,
            visibleCommands,
            _options.CommandParsing,
            _options.Autocomplete,
            accessResults);
    }

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
    /// Attempts to execute command input asynchronously and returns the structured result for command failures.
    /// </summary>
    /// <param name="input">The command input to execute.</param>
    /// <param name="cancellationToken">The token that cancels this execution.</param>
    /// <returns>The command result; expected command failures are returned rather than thrown.</returns>
    public async Task<CommandResult> TryExecuteCommandAsync(string input, CancellationToken cancellationToken = default)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        return (await ExecuteCommandAsyncCore(input, cancellationToken).ConfigureAwait(false)).Result;
    }

    /// <summary>
    /// Attempts to execute a previously parsed command asynchronously.
    /// </summary>
    /// <param name="command">The bound command to execute.</param>
    /// <param name="cancellationToken">The token that cancels this execution.</param>
    /// <returns>The command result; expected command failures are returned rather than thrown.</returns>
    public async Task<CommandResult> TryExecuteCommandAsync(BoundCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        return (await ExecuteBoundCommandAsync(command, recordCommandInput: true, cancellationToken).ConfigureAwait(false)).Result;
    }

    /// <summary>
    /// Executes command input asynchronously, throwing when the command fails.
    /// </summary>
    /// <param name="input">The command input to execute.</param>
    /// <param name="cancellationToken">The token that cancels this execution.</param>
    /// <returns>The successful command result.</returns>
    /// <exception cref="ConsoleOperationException">The command was rejected, failed, or was cancelled.</exception>
    public async Task<CommandResult> ExecuteCommandAsync(string input, CancellationToken cancellationToken = default)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        CommandExecutionOutcome outcome = await ExecuteCommandAsyncCore(input, cancellationToken).ConfigureAwait(false);
        ThrowIfFailed(outcome);
        return outcome.Result;
    }

    /// <summary>
    /// Executes a previously parsed command asynchronously, throwing when the command fails.
    /// </summary>
    /// <param name="command">The bound command to execute.</param>
    /// <param name="cancellationToken">The token that cancels this execution.</param>
    /// <returns>The successful command result.</returns>
    /// <exception cref="ConsoleOperationException">The command was rejected, failed, or was cancelled.</exception>
    public async Task<CommandResult> ExecuteCommandAsync(BoundCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        CommandExecutionOutcome outcome = await ExecuteBoundCommandAsync(
            command,
            recordCommandInput: true,
            cancellationToken).ConfigureAwait(false);
        ThrowIfFailed(outcome);
        return outcome.Result;
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

    /// <summary>Registers a configurable help command for this manager's live command registry.</summary>
    /// <param name="help">The help command configuration to register.</param>
    /// <returns>The generated command definition registered with this manager.</returns>
    public CommandDefinition RegisterHelpCommand(HelpCommandDefinition help)
    {
        if (help is null)
        {
            throw new ArgumentNullException(nameof(help));
        }

        CommandDefinition command = (GetHelpStateShape(help), help) switch
        {
            (0, _) => BuildHelpCommand<HelpStateEmpty>(help),
            (1, _) => BuildHelpCommand<HelpStateQuery>(help),
            (2, _) => BuildHelpCommand<HelpStateSearch>(help),
            (3, _) => BuildHelpCommand<HelpStateQuerySearch>(help),
            (4, _) => BuildHelpCommand<HelpStateCommand>(help),
            (5, _) => BuildHelpCommand<HelpStateQueryCommand>(help),
            (6, _) => BuildHelpCommand<HelpStateSearchCommand>(help),
            (7, _) => BuildHelpCommand<HelpStateAll>(help),
            _ => throw new InvalidOperationException("Unknown help command state shape.")
        };
        return RegisterCommand(command);
    }

    private static int GetHelpStateShape(HelpCommandDefinition help)
    {
        bool positional = help.SearchPositionalEnabled || help.CommandHelpPositionalEnabled;
        return (positional ? 1 : 0)
            | (help.SearchOptionEnabled ? 2 : 0)
            | (help.CommandHelpOptionEnabled ? 4 : 0);
    }

    private CommandDefinition BuildHelpCommand<TState>(HelpCommandDefinition help)
        where TState : HelpCommandState
    {
        var builder = new CommandBuilder(help.Path)
            .Description("Shows help for registered commands.");

        if (help.SearchPositionalEnabled || help.CommandHelpPositionalEnabled)
        {
            builder.Argument<TState>(state => state.Query, "help-query", "query")
                .Optional();
        }

        if (help.SearchOptionEnabled)
        {
            builder.Option<TState>(
                state => state.SearchQuery,
                "help-search-query",
                help.SearchOptionName!,
                help.SearchOptionAliases.ToArray());
        }

        if (help.CommandHelpOptionEnabled)
        {
            builder.Option<TState>(
                state => state.CommandQuery,
                "help-command-query",
                help.CommandHelpOptionName!,
                help.CommandHelpOptionAliases.ToArray());
        }

        bool hasPositional = help.SearchPositionalEnabled || help.CommandHelpPositionalEnabled;
        if (hasPositional && (help.SearchOptionEnabled || help.CommandHelpOptionEnabled)
            || help.SearchOptionEnabled && help.CommandHelpOptionEnabled)
        {
            builder.Constraint<TState>(
                "help-query-source-exclusive",
                "Use one help query source and one help mode at a time.",
                state =>
                {
                    int suppliedSources = (state.Query is null ? 0 : 1)
                        + (state.SearchQuery is null ? 0 : 1)
                        + (state.CommandQuery is null ? 0 : 1);
                    return suppliedSources <= 1;
                });
        }

        return builder
            .Execute<TState>((context, state) => ExecuteHelpCommand(help, context, state))
            .Build();
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
    public void LogTrace(string message) => _log.Trace(message);

    /// <summary>
    /// Adds trace log content to the shared console history.
    /// </summary>
    /// <param name="content">The log content.</param>
    public void LogTrace(ConsoleText content) => _log.Trace(content);

    /// <summary>
    /// Adds a debug log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void LogDebug(string message) => _log.Debug(message);

    /// <summary>
    /// Adds debug log content to the shared console history.
    /// </summary>
    /// <param name="content">The log content.</param>
    public void LogDebug(ConsoleText content) => _log.Debug(content);

    /// <summary>
    /// Adds an informational log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void LogInformation(string message) => _log.Information(message);

    /// <summary>
    /// Adds informational log content to the shared console history.
    /// </summary>
    /// <param name="content">The log content.</param>
    public void LogInformation(ConsoleText content) => _log.Information(content);

    /// <summary>
    /// Adds a warning log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void LogWarning(string message) => _log.Warning(message);

    /// <summary>
    /// Adds warning log content to the shared console history.
    /// </summary>
    /// <param name="content">The log content.</param>
    public void LogWarning(ConsoleText content) => _log.Warning(content);

    /// <summary>
    /// Adds an error log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void LogError(string message) => _log.Error(message);

    /// <summary>
    /// Adds error log content to the shared console history.
    /// </summary>
    /// <param name="content">The log content.</param>
    public void LogError(ConsoleText content) => _log.Error(content);

    /// <summary>
    /// Adds a critical log message to the shared console history.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void LogCritical(string message) => _log.Critical(message);

    /// <summary>
    /// Adds critical log content to the shared console history.
    /// </summary>
    /// <param name="content">The log content.</param>
    public void LogCritical(ConsoleText content) => _log.Critical(content);

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
        return InvokeHandler(command, CancellationToken.None);
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

        ConsoleFailure? accessFailure = ValidateCommandAccess(
            command,
            new CommandAvailabilityContext(command.Input, cursorIndex: null, command.Definition, command));
        if (accessFailure is not null)
        {
            return Fail(accessFailure);
        }

        CommandValidationResult validation = ValidateCommand(command);
        if (!validation.IsSuccess)
        {
            return Fail(validation.Failure!);
        }

        if (command.Definition.IsAsyncHandler)
        {
            return Fail(new ConsoleFailure(
                ConsoleFailureKind.CommandExecution,
                ConsoleFailureCodes.CommandRequiresAsyncExecution,
                $"Command '{command.Definition.Path}' requires asynchronous execution.",
                source: command.Definition.Path));
        }

        CommandExecutionOutcome outcome = InvokeHandler(command);
        if (!outcome.Result.IsSuccess)
        {
            AppendFailure(outcome.Result.Failure!);
            NotifyProgressCompleted(outcome);
            return outcome;
        }

        AppendSuccessOutputs(command.Definition, outcome.Result);
        NotifyProgressCompleted(outcome);
        return outcome;
    }

    private async Task<CommandExecutionOutcome> ExecuteCommandAsyncCore(string input, CancellationToken cancellationToken)
    {
        RecordCommandInput(input);
        CommandParseResult parse = ParseCommand(input);
        if (!parse.IsSuccess)
        {
            AppendEcho(input, null);
            return Fail(parse.Failure!);
        }

        return await ExecuteBoundCommandAsync(parse.Command!, recordCommandInput: false, cancellationToken).ConfigureAwait(false);
    }

    private async Task<CommandExecutionOutcome> ExecuteBoundCommandAsync(
        BoundCommand command,
        bool recordCommandInput,
        CancellationToken cancellationToken)
    {
        if (recordCommandInput)
        {
            RecordCommandInput(command.Input);
        }

        AppendEcho(command.Input, command.Definition);

        if (cancellationToken.IsCancellationRequested)
        {
            return Fail(CreateCancellationFailure(command.Definition));
        }

        ConsoleFailure? accessFailure = ValidateCommandAccess(
            command,
            new CommandAvailabilityContext(command.Input, cursorIndex: null, command.Definition, command));
        if (accessFailure is not null)
        {
            return Fail(accessFailure);
        }

        CommandValidationResult validation = ValidateCommand(command);
        if (!validation.IsSuccess)
        {
            return Fail(validation.Failure!);
        }

        CommandExecutionOutcome outcome = command.Definition.IsAsyncHandler
            ? await InvokeAsyncHandler(command, cancellationToken).ConfigureAwait(false)
            : InvokeHandler(command, cancellationToken);

        if (!outcome.Result.IsSuccess)
        {
            AppendFailure(outcome.Result.Failure!);
            NotifyProgressCompleted(outcome);
            return outcome;
        }

        AppendSuccessOutputs(command.Definition, outcome.Result);
        NotifyProgressCompleted(outcome);
        return outcome;
    }

    private async Task<CommandExecutionOutcome> InvokeAsyncHandler(BoundCommand command, CancellationToken cancellationToken)
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        CommandContext? context = null;

        try
        {
            object? progressState = command.Definition.ProgressStateFactory?.Invoke();
            context = CreateCommandContext(command, timestamp, cancellationToken, progressState);
            object? taskObject = command.Definition.StateType is null
                ? command.Definition.Handler.DynamicInvoke(context)
                : command.Definition.Handler.DynamicInvoke(context, command.State);

            if (taskObject is Task<CommandResult> resultTask)
            {
                CommandResult? result = await resultTask.ConfigureAwait(false);
                return result is null
                    ? MissingAsyncHandlerResult(command.Definition, context)
                    : new CommandExecutionOutcome(result, null, context);
            }

            if (taskObject is Task task)
            {
                await task.ConfigureAwait(false);
                return new CommandExecutionOutcome(CommandResult.Success(), null, context);
            }

            return MissingAsyncHandlerResult(command.Definition, context);
        }
        catch (Exception exception)
        {
            Exception causeException = UnwrapHandlerException(exception);
            if (causeException is OperationCanceledException && cancellationToken.IsCancellationRequested)
            {
                return new CommandExecutionOutcome(
                    CommandResult.Failed(CreateCancellationFailure(command.Definition, causeException)),
                    causeException,
                    context);
            }

            CommandExecutionOutcome failed = CreateHandlerFailure(command.Definition, causeException);
            return new CommandExecutionOutcome(failed.Result, failed.HandlerException, context);
        }
        finally
        {
            context?.Complete();
        }
    }

    private CommandExecutionOutcome InvokeHandler(BoundCommand command, CancellationToken cancellationToken)
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        CommandContext? context = null;

        try
        {
            object? progressState = command.Definition.ProgressStateFactory?.Invoke();
            context = CreateCommandContext(command, timestamp, cancellationToken, progressState);
            object? result = command.Definition.StateType is null
                ? ((Func<CommandContext, CommandResult>)command.Definition.Handler)(context)
                : command.Definition.Handler.DynamicInvoke(context, command.State);

            if (result is CommandResult commandResult)
            {
                return new CommandExecutionOutcome(commandResult, null, context);
            }

            return MissingHandlerResult(command.Definition, "returned no command result", context);
        }
        catch (Exception exception)
        {
            CommandExecutionOutcome failed = CreateHandlerFailure(command.Definition, UnwrapHandlerException(exception));
            return new CommandExecutionOutcome(failed.Result, failed.HandlerException, context);
        }
        finally
        {
            context?.Complete();
        }
    }

    private CommandContext CreateCommandContext(
        BoundCommand command,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken,
        object? progressState)
    {
        return new CommandContext(
            command.Input,
            command.Definition,
            command,
            timestamp,
            cancellationToken,
            _dispatcher,
            progressState,
            output => History.RecordEntry(new CommandOutputEntry(DateTimeOffset.UtcNow, output)),
            ReportCommandProgress);
    }

    private void ReportCommandProgress(CommandContext context, string message)
    {
        CommandProgressChanged?.Invoke(this, new CommandProgressChangedEventArgs(
            CommandProgressEventKind.Updated,
            context,
            message,
            context.ProgressState,
            outcome: null));
    }

    private void NotifyProgressCompleted(CommandExecutionOutcome execution)
    {
        CommandContext? context = execution.Context;
        if (context is null || !context.HasReportedProgress)
        {
            return;
        }

        CommandProgressOutcome outcome = execution.Result.IsSuccess
            ? CommandProgressOutcome.Succeeded
            : execution.Result.Failure?.Code == ConsoleFailureCodes.CommandExecutionCancelled
                ? CommandProgressOutcome.Cancelled
                : CommandProgressOutcome.Failed;

        CommandProgressChanged?.Invoke(this, new CommandProgressChangedEventArgs(
            CommandProgressEventKind.Completed,
            context,
            message: null,
            context.ProgressState,
            outcome));
    }

    private static CommandExecutionOutcome MissingAsyncHandlerResult(CommandDefinition definition, CommandContext context)
    {
        return MissingHandlerResult(definition, "returned no task or command result", context);
    }

    private static CommandExecutionOutcome MissingHandlerResult(
        CommandDefinition definition,
        string reason,
        CommandContext? context = null)
    {
        return new CommandExecutionOutcome(
            CommandResult.Failed(ConsoleFailures.CommandExecution(
                $"Command '{definition.Path}' {reason}.",
                definition.Path)),
            null,
            context);
    }

    private static CommandExecutionOutcome CreateHandlerFailure(CommandDefinition definition, Exception exception)
    {
        ConsoleFailure cause = ConsoleFailure.FromException(
            exception,
            ConsoleFailureKind.CommandExecution,
            ConsoleFailureCodes.CommandExecutionRejected);
        ConsoleFailure failure = ConsoleFailures.CommandExecution(
            $"Command '{definition.Path}' failed during execution.",
            definition.Path,
            cause);
        return new CommandExecutionOutcome(CommandResult.Failed(failure), exception);
    }

    private static Exception UnwrapHandlerException(Exception exception)
    {
        while (exception is TargetInvocationException { InnerException: not null } invocationException)
        {
            exception = invocationException.InnerException;
        }

        return exception;
    }

    private static ConsoleFailure CreateCancellationFailure(CommandDefinition definition, Exception? exception = null)
    {
        ConsoleFailure? cause = exception is null
            ? null
            : ConsoleFailure.FromException(
                exception,
                ConsoleFailureKind.CommandExecution,
                ConsoleFailureCodes.CommandExecutionRejected);
        return new ConsoleFailure(
            ConsoleFailureKind.CommandExecution,
            ConsoleFailureCodes.CommandExecutionCancelled,
            $"Command '{definition.Path}' was cancelled.",
            source: definition.Path,
            cause: cause);
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

    private ConsoleFailure? ValidateCommandAccess(BoundCommand command, CommandAvailabilityContext context)
    {
        return GetCommandAccessFailure(command.Definition, context);
    }

    private ConsoleFailure? GetCommandAccessFailure(CommandDefinition definition, CommandAvailabilityContext context)
    {
        try
        {
            string? permissionFailure = GetPermissionFailureMessage(definition);
            if (permissionFailure is not null)
            {
                return new ConsoleFailure(
                    ConsoleFailureKind.CommandAuthorization,
                    ConsoleFailureCodes.CommandPermissionDenied,
                    permissionFailure,
                    source: definition.Path);
            }

            CommandAvailabilityResult availability = GetCommandAvailability(definition, context);
            return availability.IsAvailable
                ? null
                : new ConsoleFailure(
                    ConsoleFailureKind.CommandAvailability,
                    ConsoleFailureCodes.CommandUnavailable,
                    availability.Reason!,
                    source: definition.Path);
        }
        catch (Exception exception)
        {
            ConsoleFailure cause = ConsoleFailure.FromException(exception);
            return ConsoleFailure.Wrap(
                ConsoleFailureKind.Extension,
                ConsoleFailureCodes.ExtensionRejected,
                $"An access check for command '{definition.Path}' failed.",
                cause,
                component: definition.Path,
                source: definition.Path);
        }
    }

    private CommandResult ExecuteHelpCommand(
        HelpCommandDefinition help,
        CommandContext commandContext,
        HelpCommandState state)
    {
        string? positionalQuery = NormalizeHelpQuery(state.Query);
        string? searchOptionQuery = NormalizeHelpQuery(state.SearchQuery);
        string? commandOptionQuery = NormalizeHelpQuery(state.CommandQuery);

        if (searchOptionQuery is not null)
        {
            return FormatHelpMatches(help, searchOptionQuery, search: true, commandContext);
        }

        if (commandOptionQuery is not null)
        {
            CommandDefinition? definition = FindCommand(commandOptionQuery);
            return definition is null
                ? ResultFromLines(GetHelpError(help, HelpCommandErrorKind.UnknownCommand, commandOptionQuery, null, null, Array.Empty<CommandDefinition>()))
                : FormatSpecificHelp(help, definition, commandOptionQuery, commandContext);
        }

        if (positionalQuery is null)
        {
            return FormatHelpMatches(help, searchTerm: null, search: false, commandContext);
        }

        if (help.CommandHelpPositionalEnabled)
        {
            CommandDefinition? exact = FindCommand(positionalQuery);
            if (exact is not null)
            {
                ConsoleFailure? failure = GetCommandAccessFailure(
                    exact,
                    new CommandAvailabilityContext(commandContext.Input, cursorIndex: null, exact, command: null));
                HelpCommandAccessDecision decision = failure is null
                    ? HelpCommandAccessDecision.Show()
                    : GetHelpAccessDecision(help, exact, positionalQuery, new[] { exact }, commandContext, failure);
                if (decision.Kind == HelpCommandAccessDecisionKind.Show)
                {
                    return FormatSpecificHelp(help, exact, positionalQuery, commandContext, failure, decision);
                }

                if (decision.Kind == HelpCommandAccessDecisionKind.Respond)
                {
                    return ResultFromLines(decision.ResponseLines);
                }
                if (decision.Kind == HelpCommandAccessDecisionKind.UseConfiguredError)
                    return ResultFromLines(GetHelpError(help, GetAccessErrorKind(failure!), positionalQuery, exact, failure, new[] { exact }));
                if (decision.Kind == HelpCommandAccessDecisionKind.Hide && !help.SearchPositionalEnabled)
                    return ResultFromLines(GetHelpError(help, HelpCommandErrorKind.UnknownCommand, positionalQuery, null, null, Array.Empty<CommandDefinition>()));
            }
            else if (help.CommandHelpPositionalEnabled && !help.SearchPositionalEnabled)
            {
                return ResultFromLines(GetHelpError(help, HelpCommandErrorKind.UnknownCommand, positionalQuery, null, null, Array.Empty<CommandDefinition>()));
            }
        }

        return help.SearchPositionalEnabled
            ? FormatHelpMatches(help, positionalQuery, search: true, commandContext)
            : CommandResult.Success();
    }

    private CommandResult FormatHelpMatches(
        HelpCommandDefinition help,
        string? searchTerm,
        bool search,
        CommandContext commandContext)
    {
        var definitions = new List<CommandDefinition>();
        foreach (CommandDefinition definition in Commands.Definitions)
        {
            if (!search || help.SearchMatcher(definition, searchTerm!, _options.CommandParsing.IsCaseSensitive))
            {
                definitions.Add(definition);
            }
        }

        if (help.ResultComparer is not null)
            definitions = definitions.OrderBy(definition => definition, help.ResultComparer).ToList();
        else if (help.ResultOrder == HelpCommandResultOrder.Alphabetical)
            definitions = definitions.OrderBy(definition => definition.Path, StringComparer.OrdinalIgnoreCase).ToList();

        IReadOnlyList<CommandDefinition> candidates = definitions.AsReadOnly();
        var decisions = new Dictionary<CommandDefinition, (ConsoleFailure? Failure, HelpCommandAccessDecision Decision)>();
        var visible = new List<CommandDefinition>();
        foreach (CommandDefinition definition in definitions)
        {
            ConsoleFailure? failure = GetCommandAccessFailure(
                definition,
                new CommandAvailabilityContext(commandContext.Input, cursorIndex: null, definition, command: null));
            HelpCommandAccessDecision decision = failure is null
                ? HelpCommandAccessDecision.Show()
                : GetHelpAccessDecision(help, definition, searchTerm, candidates, commandContext, failure);
            decisions.Add(definition, (failure, decision));
            if (decision.Kind == HelpCommandAccessDecisionKind.Show)
            {
                visible.Add(definition);
            }
        }

        var visibleReadOnly = visible.AsReadOnly();
        var outputs = new List<CommandOutput>();
        foreach (CommandDefinition definition in definitions)
        {
            (ConsoleFailure? failure, HelpCommandAccessDecision decision) = decisions[definition];
            if (decision.Kind == HelpCommandAccessDecisionKind.Hide)
            {
                continue;
            }

            if (decision.Kind == HelpCommandAccessDecisionKind.Respond)
            {
                AddBlock(outputs, decision.ResponseLines);
                continue;
            }
            if (decision.Kind == HelpCommandAccessDecisionKind.UseConfiguredError)
            {
                AddBlock(outputs, GetHelpError(help, GetAccessErrorKind(failure!), searchTerm, definition, failure, candidates));
                continue;
            }

            var context = CreateHelpContext(definition, searchTerm, failure, visibleReadOnly);
            List<string> lines = (search ? help.SearchFormatter : null) is Func<CommandHelpContext, List<string>> formatter
                ? formatter(context)
                : help.Formatter(context);
            if (lines is null)
            {
                throw new InvalidOperationException("A help formatter returned null.");
            }
            AddBlock(outputs, lines);
        }

        if (outputs.Count == 0 && search && help.ErrorsEnabled)
            AddBlock(outputs, GetHelpError(help, HelpCommandErrorKind.NoSearchResults, searchTerm, null, null, candidates));

        return CommandResult.Success(outputs.ToArray());
    }

    private CommandResult FormatSpecificHelp(
        HelpCommandDefinition help,
        CommandDefinition definition,
        string? searchTerm,
        CommandContext commandContext,
        ConsoleFailure? knownFailure = null,
        HelpCommandAccessDecision? knownDecision = null)
    {
        ConsoleFailure? failure;
        HelpCommandAccessDecision decision;
        if (knownDecision is not null)
        {
            failure = knownFailure;
            decision = knownDecision;
        }
        else
        {
            failure = GetCommandAccessFailure(
                definition,
                new CommandAvailabilityContext(commandContext.Input, cursorIndex: null, definition, command: null));
            decision = failure is null
                ? HelpCommandAccessDecision.Show()
                : GetHelpAccessDecision(help, definition, searchTerm, new[] { definition }, commandContext, failure);
        }

        if (decision.Kind == HelpCommandAccessDecisionKind.Hide)
        {
            return ResultFromLines(GetHelpError(help, HelpCommandErrorKind.UnknownCommand, searchTerm, null, null, Array.Empty<CommandDefinition>()));
        }

        if (decision.Kind == HelpCommandAccessDecisionKind.Respond)
        {
            return ResultFromLines(decision.ResponseLines);
        }

        if (decision.Kind == HelpCommandAccessDecisionKind.UseConfiguredError)
            return ResultFromLines(GetHelpError(help, GetAccessErrorKind(failure!), searchTerm, definition, failure, new[] { definition }));

        var matches = Array.AsReadOnly(new[] { definition });
        var context = CreateHelpContext(definition, searchTerm, failure, matches);
        List<string> lines = (help.CommandHelpFormatter ?? help.Formatter)(context)
            ?? throw new InvalidOperationException("A help formatter returned null.");
        return ResultFromLines(lines);
    }

    private CommandHelpContext CreateHelpContext(CommandDefinition definition, string? searchTerm, ConsoleFailure? failure, IReadOnlyList<CommandDefinition> matches)
    {
        string? syntax = null;
        if (definition.Syntax is not null) syntax = definition.Syntax;
        else if (definition.SyntaxFormatter is not null)
        {
            syntax = definition.SyntaxFormatter(new CommandSyntaxContext(definition, CommandParsingOptions.CreateSnapshot(_options.CommandParsing)));
            if (syntax is null) throw new InvalidOperationException("A command syntax formatter returned null.");
        }
        return new CommandHelpContext(definition, searchTerm, failure, matches, syntax, CommandParsingOptions.CreateSnapshot(_options.CommandParsing));
    }

    private static HelpCommandErrorKind GetAccessErrorKind(ConsoleFailure failure) =>
        failure.Kind == ConsoleFailureKind.CommandAuthorization ? HelpCommandErrorKind.PermissionDenied : HelpCommandErrorKind.Unavailable;

    private static IReadOnlyList<string> GetHelpError(HelpCommandDefinition help, HelpCommandErrorKind kind, string? query, CommandDefinition? command, ConsoleFailure? failure, IReadOnlyList<CommandDefinition> matches)
    {
        if (!help.ErrorsEnabled) return Array.Empty<string>();
        var context = new HelpCommandErrorContext(kind, query, command, failure, matches);
        HelpCommandErrorFormatter formatter = kind switch
        {
            HelpCommandErrorKind.UnknownCommand => help.UnknownCommand,
            HelpCommandErrorKind.NoSearchResults => help.NoSearchResults,
            HelpCommandErrorKind.PermissionDenied => help.PermissionDenied,
            _ => help.Unavailable
        };
        string? message = formatter(context);
        return string.IsNullOrEmpty(message) ? Array.Empty<string>() : new[] { message };
    }

    private HelpCommandAccessDecision GetHelpAccessDecision(
        HelpCommandDefinition help,
        CommandDefinition definition,
        string? searchTerm,
        IReadOnlyList<CommandDefinition> candidates,
        CommandContext commandContext,
        ConsoleFailure? knownFailure = null)
    {
        ConsoleFailure? failure = knownFailure ?? GetCommandAccessFailure(
            definition,
            new CommandAvailabilityContext(commandContext.Input, cursorIndex: null, definition, command: null));
        if (failure is null)
        {
            return HelpCommandAccessDecision.Show();
        }

        var accessContext = new CommandHelpAccessContext(definition, searchTerm, failure, candidates);
        return help.AccessPolicy(accessContext)
            ?? throw new InvalidOperationException("The help command access policy returned null.");
    }

    private CommandDefinition? FindCommand(string path)
    {
        foreach (CommandDefinition definition in Commands.Definitions)
        {
            if (_commandPathComparer.Equals(definition.Path, path))
            {
                return definition;
            }
        }

        return null;
    }

    private static string? NormalizeHelpQuery(string? query)
    {
        return string.IsNullOrWhiteSpace(query) ? null : query;
    }

    private static CommandResult ResultFromLines(IReadOnlyList<string> lines)
    {
        var outputs = new List<CommandOutput>();
        AddBlock(outputs, lines);
        return CommandResult.Success(outputs.ToArray());
    }

    private static void AddBlock(List<CommandOutput> outputs, IEnumerable<string> lines)
    {
        if (lines is null)
        {
            throw new InvalidOperationException("A help formatter or access policy returned null lines.");
        }

        var copy = new List<string>();
        foreach (string line in lines)
        {
            copy.Add(line ?? throw new InvalidOperationException("Help output lines cannot contain null."));
        }

        if (copy.Count > 0)
        {
            outputs.Add(CommandOutput.Block(string.Join(Environment.NewLine, copy)));
        }
    }

    // The command binder creates state from the exact constructor parameters represented by the enabled schema members.
    // These small shapes keep disabled positional and option forms out of the registered command definition.
    private abstract class HelpCommandState
    {
        protected HelpCommandState()
        {
        }

        public string? Query { get; protected set; }
        public string? SearchQuery { get; protected set; }
        public string? CommandQuery { get; protected set; }
    }

    private sealed class HelpStateEmpty : HelpCommandState
    {
        public HelpStateEmpty() { }
    }

    private sealed class HelpStateQuery : HelpCommandState
    {
        public HelpStateQuery(string? Query)
        {
            this.Query = Query;
        }
    }

    private sealed class HelpStateSearch : HelpCommandState
    {
        public HelpStateSearch(string? SearchQuery)
        {
            this.SearchQuery = SearchQuery;
        }
    }

    private sealed class HelpStateCommand : HelpCommandState
    {
        public HelpStateCommand(string? CommandQuery)
        {
            this.CommandQuery = CommandQuery;
        }
    }

    private sealed class HelpStateQuerySearch : HelpCommandState
    {
        public HelpStateQuerySearch(string? Query, string? SearchQuery)
        {
            this.Query = Query;
            this.SearchQuery = SearchQuery;
        }
    }

    private sealed class HelpStateQueryCommand : HelpCommandState
    {
        public HelpStateQueryCommand(string? Query, string? CommandQuery)
        {
            this.Query = Query;
            this.CommandQuery = CommandQuery;
        }
    }

    private sealed class HelpStateSearchCommand : HelpCommandState
    {
        public HelpStateSearchCommand(string? SearchQuery, string? CommandQuery)
        {
            this.SearchQuery = SearchQuery;
            this.CommandQuery = CommandQuery;
        }
    }

    private sealed class HelpStateAll : HelpCommandState
    {
        public HelpStateAll(string? Query, string? SearchQuery, string? CommandQuery)
        {
            this.Query = Query;
            this.SearchQuery = SearchQuery;
            this.CommandQuery = CommandQuery;
        }
    }

    private CommandAvailabilityResult GetCommandAccessStatus(CommandDefinition definition, CommandAvailabilityContext context)
    {
        string? permissionFailure = GetPermissionFailureMessage(definition);
        if (permissionFailure is not null)
        {
            return CommandAvailabilityResult.Unavailable(permissionFailure);
        }

        return GetCommandAvailability(definition, context);
    }

    private string? GetPermissionFailureMessage(CommandDefinition definition)
    {
        foreach (string permission in definition.RequiredPermissions)
        {
            if (_permissionProvider is null || !_permissionProvider.HasPermission(permission))
            {
                return $"Command '{definition.Path}' requires permission '{permission}'.";
            }
        }

        foreach (IReadOnlyList<string> alternatives in definition.RequiredAnyPermissionGroups)
        {
            bool granted = false;
            if (_permissionProvider is not null)
            {
                foreach (string permission in alternatives)
                {
                    if (_permissionProvider.HasPermission(permission))
                    {
                        granted = true;
                        break;
                    }
                }
            }

            if (!granted)
            {
                return $"Command '{definition.Path}' requires one of these permissions: {string.Join(", ", alternatives)}.";
            }
        }

        return null;
    }

    private CommandAvailabilityResult GetCommandAvailability(CommandDefinition definition, CommandAvailabilityContext context)
    {
        foreach (Func<CommandAvailabilityContext, CommandAvailabilityResult> check in definition.AvailabilityChecks)
        {
            CommandAvailabilityResult? result = check(context);
            if (result is null)
            {
                throw new InvalidOperationException($"Availability check for command '{definition.Path}' returned null.");
            }

            if (!result.IsAvailable)
            {
                return result;
            }
        }

        return CommandAvailabilityResult.Available();
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
        public CommandExecutionOutcome(CommandResult result, Exception? handlerException, CommandContext? context = null)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
            HandlerException = handlerException;
            Context = context;
        }

        public CommandResult Result { get; }

        public Exception? HandlerException { get; }

        public CommandContext? Context { get; }
    }
}
