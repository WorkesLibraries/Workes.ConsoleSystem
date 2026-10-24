using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Entries;

namespace Workes.ConsoleSystem.Tests.Commands;

public sealed class AsyncCommandExecutionTests
{
    [Test]
    public async Task ExecuteCommandAsync_SimpleAndTypedHandlers_ReturnTheirResults()
    {
        var console = new ConsoleManager();
        var expected = CommandResult.Success(CommandOutput.Inline("done"));
        RestartState? captured = null;
        console.RegisterCommand(new CommandBuilder("simple")
            .ExecuteAsync(async context =>
            {
                await Task.Yield();
                return expected;
            })
            .Build());
        console.RegisterCommand(new CommandBuilder("typed")
            .Argument<RestartState>(state => state.Reason, "reason", "reason")
            .ExecuteAsync<RestartState>(async (context, state) =>
            {
                await Task.Yield();
                captured = state;
                return CommandResult.Success();
            })
            .Build());

        CommandResult simpleResult = await console.ExecuteCommandAsync("simple");
        CommandResult typedResult = await console.TryExecuteCommandAsync("typed maintenance");

        Assert.That(simpleResult, Is.SameAs(expected));
        Assert.That(typedResult.IsSuccess, Is.True);
        Assert.That(captured!.Reason, Is.EqualTo("maintenance"));
    }

    [Test]
    public async Task ExecuteCommandAsync_TaskOnlyHandler_ProducesSuccessAndSupportsBoundCommand()
    {
        bool invoked = false;
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("task")
            .ExecuteAsync(async context =>
            {
                await Task.Yield();
                invoked = true;
            })
            .Build());
        BoundCommand command = console.ParseCommand("task").Command!;

        CommandResult result = await console.ExecuteCommandAsync(command);
        CommandResult tryResult = await console.TryExecuteCommandAsync(command);

        Assert.That(invoked, Is.True);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(tryResult.IsSuccess, Is.True);
    }

    [Test]
    public async Task ExecuteCommandAsync_TypedTaskOnlyHandler_ProducesSuccess()
    {
        int receivedAmount = 0;
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("count")
            .Argument<AmountState>(state => state.Amount, "amount", "amount")
            .ExecuteAsync<AmountState>(async (context, state) =>
            {
                await Task.Yield();
                receivedAmount = state.Amount;
            })
            .Build());

        CommandResult result = await console.TryExecuteCommandAsync("count 7");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(receivedAmount, Is.EqualTo(7));
    }

    [Test]
    public async Task AsyncManager_ExecutesSynchronousHandler()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("sync")
            .Execute(context => CommandResult.Success())
            .Build());

        CommandResult result = await console.TryExecuteCommandAsync("sync");

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void SynchronousManager_RejectsAsyncOnlyHandlerWithoutInvokingIt()
    {
        bool invoked = false;
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("async")
            .ExecuteAsync(context =>
            {
                invoked = true;
                return Task.FromResult(CommandResult.Success());
            })
            .Build());

        bool succeeded = console.TryExecuteCommand("async", out CommandResult result);

        Assert.That(succeeded, Is.False);
        Assert.That(invoked, Is.False);
        Assert.That(result.Failure!.Code, Is.EqualTo(ConsoleFailureCodes.CommandRequiresAsyncExecution));
    }

    [Test]
    public async Task AsyncHandler_ReceivesCancellationTokenAndConfiguredDispatcher()
    {
        using var cancellation = new CancellationTokenSource();
        var dispatcher = new InlineDispatcher();
        var console = new ConsoleManager(new ConsoleManagerOptions(), null, dispatcher);
        bool actionCalled = false;
        int callbackResult = 0;
        console.RegisterCommand(new CommandBuilder("dispatch")
            .ExecuteAsync(async context =>
            {
                Assert.That(context.CancellationToken, Is.EqualTo(cancellation.Token));
                Assert.That(context.Dispatcher, Is.SameAs(dispatcher));
                await context.Dispatcher.InvokeAsync(() => actionCalled = true, context.CancellationToken);
                callbackResult = await context.Dispatcher.InvokeAsync(() => 42, context.CancellationToken);
                return CommandResult.Success();
            })
            .Build());

        CommandResult result = await console.ExecuteCommandAsync("dispatch", cancellation.Token);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(actionCalled, Is.True);
        Assert.That(callbackResult, Is.EqualTo(42));
    }

    [Test]
    public async Task MissingDispatcher_IsExplicitAndDispatcherExceptionBecomesHandlerFailure()
    {
        bool dispatcherWasMissing = false;
        var missingDispatcherConsole = new ConsoleManager();
        missingDispatcherConsole.RegisterCommand(new CommandBuilder("missing.dispatcher")
            .ExecuteAsync(context =>
            {
                dispatcherWasMissing = context.Dispatcher is null;
                return Task.FromResult(CommandResult.Success());
            })
            .Build());
        await missingDispatcherConsole.ExecuteCommandAsync("missing.dispatcher");

        var dispatchException = new InvalidOperationException("Host dispatch failed.");
        var failingDispatcher = new InlineDispatcher { Exception = dispatchException };
        var failingConsole = new ConsoleManager(new ConsoleManagerOptions(), null, failingDispatcher);
        failingConsole.RegisterCommand(new CommandBuilder("dispatcher.failure")
            .ExecuteAsync(async context =>
            {
                await context.Dispatcher!.InvokeAsync(() => { });
                return CommandResult.Success();
            })
            .Build());

        CommandResult failure = await failingConsole.TryExecuteCommandAsync("dispatcher.failure");

        Assert.That(dispatcherWasMissing, Is.True);
        Assert.That(failure.Failure!.Code, Is.EqualTo(ConsoleFailureCodes.CommandExecutionRejected));
        Assert.That(failure.Failure.Cause!.Message, Is.EqualTo(dispatchException.Message));
    }

    [Test]
    public async Task AsyncValidationFailureDoesNotInvokeHandler()
    {
        bool invoked = false;
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("positive")
            .Argument<AmountState>(state => state.Amount, "amount", "amount")
            .Constraint<AmountState>("positive", "Amount must be positive.", state => state.Amount > 0)
            .ExecuteAsync<AmountState>((context, state) =>
            {
                invoked = true;
                return Task.FromResult(CommandResult.Success());
            })
            .Build());

        CommandResult result = await console.TryExecuteCommandAsync("positive 0");

        Assert.That(invoked, Is.False);
        Assert.That(result.Failure!.Code, Is.EqualTo(ConsoleFailureCodes.CommandConstraintRejected));
    }

    [Test]
    public async Task PreCancelledExecution_RecordsInputAndEchoWithoutCallingHandler()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        bool invoked = false;
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            Execution = new CommandExecutionOptions { EchoInput = true }
        });
        console.RegisterCommand(new CommandBuilder("async")
            .ExecuteAsync(context =>
            {
                invoked = true;
                return Task.FromResult(CommandResult.Success());
            })
            .Build());

        CommandResult result = await console.TryExecuteCommandAsync("async", cancellation.Token);

        Assert.That(invoked, Is.False);
        Assert.That(result.Failure!.Code, Is.EqualTo(ConsoleFailureCodes.CommandExecutionCancelled));
        Assert.That(console.CommandHistory.Entries, Is.EqualTo(new[] { "async" }));
        Assert.That(console.History.Entries[0], Is.TypeOf<CommandInputEntry>());
        Assert.That(console.History.Entries[1], Is.TypeOf<CommandFailureEntry>());
    }

    [Test]
    public async Task CancellationDuringHandler_IsStructuredAndPreservesException()
    {
        using var cancellation = new CancellationTokenSource();
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("wait")
            .ExecuteAsync(async context =>
            {
                await Task.Delay(Timeout.Infinite, context.CancellationToken);
                return CommandResult.Success();
            })
            .Build());

        Task<CommandResult> execution = console.TryExecuteCommandAsync("wait", cancellation.Token);
        cancellation.Cancel();
        CommandResult result = await execution;

        Assert.That(result.Failure!.Code, Is.EqualTo(ConsoleFailureCodes.CommandExecutionCancelled));
        Assert.That(result.Failure.Cause, Is.Not.Null);
        Assert.That(console.History.Entries.Last(), Is.TypeOf<CommandFailureEntry>());
    }

    [Test]
    public async Task ExecuteCommandAsync_CancellationThrowsWithOriginalInnerException()
    {
        using var cancellation = new CancellationTokenSource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("wait")
            .ExecuteAsync(async context =>
            {
                started.SetResult();
                await Task.Delay(Timeout.Infinite, context.CancellationToken);
                return CommandResult.Success();
            })
            .Build());

        Task<CommandResult> execution = console.ExecuteCommandAsync("wait", cancellation.Token);
        await started.Task;
        cancellation.Cancel();

        ConsoleOperationException exception = Assert.ThrowsAsync<ConsoleOperationException>(async () => await execution)!;

        Assert.That(exception.Failure.Code, Is.EqualTo(ConsoleFailureCodes.CommandExecutionCancelled));
        Assert.That(exception.InnerException, Is.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public void UnrelatedOperationCanceledException_IsHandlerFailureWithInnerException()
    {
        var original = new OperationCanceledException("unrelated");
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("cancel")
            .ExecuteAsync(context => Task.FromException<CommandResult>(original))
            .Build());

        ConsoleOperationException exception = Assert.ThrowsAsync<ConsoleOperationException>(
            async () => await console.ExecuteCommandAsync("cancel"))!;

        Assert.That(exception.Failure.Code, Is.EqualTo(ConsoleFailureCodes.CommandExecutionRejected));
        Assert.That(exception.InnerException, Is.SameAs(original));
    }

    [Test]
    public async Task HandlerMayIgnoreCancellationAndReturnSuccess()
    {
        using var cancellation = new CancellationTokenSource();
        var completion = new TaskCompletionSource<CommandResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("ignore")
            .ExecuteAsync(context => completion.Task)
            .Build());

        Task<CommandResult> execution = console.TryExecuteCommandAsync("ignore", cancellation.Token);
        cancellation.Cancel();
        completion.SetResult(CommandResult.Success());

        Assert.That((await execution).IsSuccess, Is.True);
    }

    [Test]
    public async Task ConcurrentExecutions_RecordSubmissionAndCompletionOrderSafely()
    {
        var firstCompletion = new TaskCompletionSource<CommandResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondCompletion = new TaskCompletionSource<CommandResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("first")
            .ExecuteAsync(context => firstCompletion.Task)
            .Build());
        console.RegisterCommand(new CommandBuilder("second")
            .ExecuteAsync(context => secondCompletion.Task)
            .Build());

        Task<CommandResult> first = console.TryExecuteCommandAsync("first");
        Task<CommandResult> second = console.TryExecuteCommandAsync("second");
        Assert.That(console.CommandHistory.Entries, Is.EqualTo(new[] { "first", "second" }));
        Assert.That(console.History.Entries.OfType<CommandInputEntry>().Select(entry => entry.Input), Is.EqualTo(new[] { "first", "second" }));

        secondCompletion.SetResult(CommandResult.Success(CommandOutput.Inline("second done")));
        await second;
        firstCompletion.SetResult(CommandResult.Success(CommandOutput.Inline("first done")));
        await first;

        string[] output = console.History.Entries
            .OfType<CommandOutputEntry>()
            .Select(entry => entry.PlainText)
            .ToArray();
        Assert.That(output, Is.EqualTo(new[] { "second done", "first done" }));
    }

    [Test]
    public void AsyncAndSynchronousHandlersCannotBeCombinedOnOneDefinition()
    {
        var builder = new CommandBuilder("mixed")
            .Execute(context => CommandResult.Success());

        Assert.Throws<InvalidOperationException>(() => builder.ExecuteAsync(
            context => Task.FromResult(CommandResult.Success())));

        var typedBuilder = new CommandBuilder("typed.mixed")
            .Execute(context => CommandResult.Success());
        Assert.Throws<InvalidOperationException>(() => typedBuilder.ExecuteAsync<AmountState>(
            (context, state) => Task.FromResult(CommandResult.Success())));
        Assert.That(typedBuilder.Build().StateType, Is.Null);
    }

    [Test]
    public void SynchronousHandlerReceivesNoCancellationAndNoDispatcherByDefault()
    {
        CommandContext? captured = null;
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("sync.context")
            .Execute(context =>
            {
                captured = context;
                return CommandResult.Success();
            })
            .Build());

        console.ExecuteCommand("sync.context");

        Assert.That(captured!.CancellationToken, Is.EqualTo(CancellationToken.None));
        Assert.That(captured.Dispatcher, Is.Null);
    }

    [Test]
    public async Task ParallelCompletionsKeepBothHistoryCollectionsConsistent()
    {
        const int commandCount = 48;
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            History = new HistoryOptions
            {
                ConsoleHistoryCapacity = commandCount * 2,
                CommandHistoryCapacity = commandCount
            },
            Execution = new CommandExecutionOptions { EchoInput = true }
        });
        int consoleNotifications = 0;
        int commandNotifications = 0;
        console.ConsoleHistoryChanged += (_, _) => Interlocked.Increment(ref consoleNotifications);
        console.CommandHistoryChanged += (_, _) => Interlocked.Increment(ref commandNotifications);
        for (int i = 0; i < commandCount; i++)
        {
            int captured = i;
            console.RegisterCommand(new CommandBuilder($"cmd{captured}")
                .ExecuteAsync(async context =>
                {
                    await Task.Yield();
                    return CommandResult.Success(CommandOutput.Inline(captured.ToString()));
                })
                .Build());
        }

        Task<CommandResult>[] executions = Enumerable.Range(0, commandCount)
            .Select(index => console.TryExecuteCommandAsync($"cmd{index}"))
            .ToArray();
        await Task.WhenAll(executions);

        Assert.That(console.CommandHistory.Entries, Has.Count.EqualTo(commandCount));
        Assert.That(console.History.Entries, Has.Count.EqualTo(commandCount * 2));
        Assert.That(console.History.Entries.OfType<CommandInputEntry>().ToArray(), Has.Length.EqualTo(commandCount));
        Assert.That(console.History.Entries.OfType<CommandOutputEntry>().ToArray(), Has.Length.EqualTo(commandCount));
        Assert.That(consoleNotifications, Is.EqualTo(commandCount * 2));
        Assert.That(commandNotifications, Is.EqualTo(commandCount));
    }

    private sealed class InlineDispatcher : ICommandDispatcher
    {
        public Exception? Exception { get; set; }

        public Task InvokeAsync(Action action, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Exception is not null) throw Exception;
            action();
            return Task.CompletedTask;
        }

        public Task<TResult> InvokeAsync<TResult>(Func<TResult> callback, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Exception is not null) throw Exception;
            return Task.FromResult(callback());
        }
    }

    private sealed record AmountState(int Amount);

    private sealed record RestartState(string Reason);
}
