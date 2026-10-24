using System.Collections.Concurrent;
using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Entries;
using Workes.ConsoleSystem.History;

namespace Workes.ConsoleSystem.Tests.Commands;

public sealed class CommandProgressTests
{
    [Test]
    public void SynchronousCommand_WritesOutputImmediatelyAndReportsTerminalStateAfterResult()
    {
        var console = new ConsoleManager();
        var events = new List<CommandProgressChangedEventArgs>();
        var states = new List<ProgressState>();
        CommandContext? captured = null;
        console.CommandProgressChanged += (_, args) =>
        {
            events.Add(args);
            if (args.Kind == CommandProgressEventKind.Updated)
            {
                states.Add((ProgressState)args.ProgressState!);
                Assert.That(console.History.Entries.OfType<CommandOutputEntry>().Select(entry => entry.PlainText),
                    Is.EqualTo(new[] { "Starting" }));
            }
            else
            {
                Assert.That(args.Outcome, Is.EqualTo(CommandProgressOutcome.Succeeded));
                Assert.That(console.History.Entries.OfType<CommandOutputEntry>().Select(entry => entry.PlainText),
                    Is.EqualTo(new[] { "Starting", "Finished" }));
            }
        };

        console.RegisterCommand(new CommandBuilder("sync.progress")
            .ProgressState(() => new ProgressState())
            .Execute(context =>
            {
                captured = context;
                var state = context.GetProgressState<ProgressState>();
                state.Percent = 0.5f;
                context.WriteOutput(CommandOutput.Inline("Starting"));
                context.ReportProgress("Working");
                return CommandResult.Success(CommandOutput.Block("Finished"));
            })
            .Build());

        CommandResult result = console.ExecuteCommand("sync.progress");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(events.Select(args => args.Kind), Is.EqualTo(new[]
        {
            CommandProgressEventKind.Updated,
            CommandProgressEventKind.Completed
        }));
        Assert.That(events[0].Context, Is.SameAs(captured));
        Assert.That(events[0].Message, Is.EqualTo("Working"));
        Assert.That(events[1].Message, Is.Null);
        Assert.That(events[1].ProgressState, Is.SameAs(states[0]));
        Assert.That(states[0].Percent, Is.EqualTo(0.5f));
        Assert.That(console.History.Entries.OfType<CommandOutputEntry>().First().Output.Kind, Is.EqualTo(CommandOutputKind.Inline));
        Assert.That(console.History.Entries.OfType<CommandOutputEntry>().Last().Output.Kind, Is.EqualTo(CommandOutputKind.Block));
    }

    [Test]
    public async Task AsyncCommand_StreamsBeforeCompletionAndSupportsMessageOnlyProgress()
    {
        var console = new ConsoleManager();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource<CommandResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var events = new ConcurrentQueue<CommandProgressChangedEventArgs>();
        console.CommandProgressChanged += (_, args) => events.Enqueue(args);
        console.RegisterCommand(new CommandBuilder("async.progress")
            .ExecuteAsync(async context =>
            {
                context.WriteOutput(CommandOutput.Inline("Connected"));
                context.ReportProgress("Loading");
                started.SetResult();
                return await finish.Task;
            })
            .Build());

        Task<CommandResult> execution = console.TryExecuteCommandAsync("async.progress");
        await started.Task;

        Assert.That(console.History.Entries.OfType<CommandOutputEntry>().Select(entry => entry.PlainText),
            Is.EqualTo(new[] { "Connected" }));
        Assert.That(events.Select(args => args.Kind), Is.EqualTo(new[] { CommandProgressEventKind.Updated }));
        Assert.That(events.Single().ProgressState, Is.Null);
        Assert.That(events.Single().Message, Is.EqualTo("Loading"));

        finish.SetResult(CommandResult.Success(CommandOutput.Inline("Done")));
        Assert.That((await execution).IsSuccess, Is.True);
        Assert.That(events.Select(args => args.Kind), Is.EqualTo(new[]
        {
            CommandProgressEventKind.Updated,
            CommandProgressEventKind.Completed
        }));
        Assert.That(events.Last().Outcome, Is.EqualTo(CommandProgressOutcome.Succeeded));
        Assert.That(console.History.Entries.OfType<CommandOutputEntry>().Select(entry => entry.PlainText),
            Is.EqualTo(new[] { "Connected", "Done" }));
    }

    [Test]
    public void StreamedOutput_UsesHistoryNotificationsAndNormalCapacityRetention()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            Execution = new CommandExecutionOptions { EchoInput = false },
            History = new HistoryOptions { ConsoleHistoryCapacity = 2 }
        });
        var changes = new List<ConsoleHistoryChangedEventArgs>();
        console.History.Changed += (_, args) => changes.Add(args);
        console.RegisterCommand(new CommandBuilder("stream.retention")
            .Execute(context =>
            {
                context.WriteOutput(CommandOutput.Inline("First"));
                context.WriteOutput(CommandOutput.Block("Second"));
                context.ReportProgress("Still running");
                Assert.That(console.History.Entries.OfType<CommandOutputEntry>().Select(entry => entry.PlainText),
                    Is.EqualTo(new[] { "First", "Second" }));
                context.WriteOutput(CommandOutput.Inline("Third"));
                return CommandResult.Success();
            })
            .Build());

        console.ExecuteCommand("stream.retention");

        Assert.That(changes.Select(change => change.AddedEntries.Single()).OfType<CommandOutputEntry>()
            .Select(entry => entry.Output.Kind),
            Is.EqualTo(new[] { CommandOutputKind.Inline, CommandOutputKind.Block, CommandOutputKind.Inline }));
        Assert.That(changes[2].DroppedEntries.OfType<CommandOutputEntry>().Single().PlainText, Is.EqualTo("First"));
        Assert.That(console.History.Entries.OfType<CommandOutputEntry>().Select(entry => entry.PlainText),
            Is.EqualTo(new[] { "Second", "Third" }));
    }

    [Test]
    public async Task ConcurrentInvocations_GetIndependentProgressStateInstances()
    {
        int factoryCalls = 0;
        var ready = new ConcurrentQueue<TaskCompletionSource>();
        var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reports = new ConcurrentQueue<object?>();
        var console = new ConsoleManager();
        console.CommandProgressChanged += (_, args) =>
        {
            if (args.Kind == CommandProgressEventKind.Updated)
            {
                reports.Enqueue(args.ProgressState);
            }
        };
        console.RegisterCommand(new CommandBuilder("parallel.progress")
            .ProgressState(() =>
            {
                Interlocked.Increment(ref factoryCalls);
                return new ProgressState();
            })
            .ExecuteAsync(async context =>
            {
                ProgressState state = context.GetProgressState<ProgressState>();
                state.Percent = reports.Count + 0.25f;
                var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                ready.Enqueue(signal);
                context.ReportProgress("Started");
                signal.SetResult();
                await finish.Task;
                return CommandResult.Success();
            })
            .Build());

        Task<CommandResult> first = console.TryExecuteCommandAsync("parallel.progress");
        Task<CommandResult> second = console.TryExecuteCommandAsync("parallel.progress");
        while (ready.Count < 2)
        {
            await Task.Yield();
        }

        object?[] stateInstances = reports.ToArray();
        Assert.That(factoryCalls, Is.EqualTo(2));
        Assert.That(stateInstances, Has.Length.EqualTo(2));
        Assert.That(stateInstances[0], Is.Not.SameAs(stateInstances[1]));

        finish.SetResult();
        Assert.That((await Task.WhenAll(first, second)).All(result => result.IsSuccess), Is.True);
    }

    [Test]
    public void FailureAfterStreaming_KeepsOutputAndSendsFailedTerminalNotification()
    {
        var console = new ConsoleManager();
        CommandProgressChangedEventArgs? terminal = null;
        IConsoleEntry? lastEntryAtTerminal = null;
        console.CommandProgressChanged += (_, args) =>
        {
            if (args.Kind == CommandProgressEventKind.Completed)
            {
                terminal = args;
                lastEntryAtTerminal = console.History.Entries.Last();
            }
        };
        console.RegisterCommand(new CommandBuilder("fail.after.output")
            .Execute(context =>
            {
                context.WriteOutput(CommandOutput.Inline("Partial work"));
                context.ReportProgress("Halfway");
                throw new InvalidOperationException("failed after partial output");
            })
            .Build());

        Assert.That(console.TryExecuteCommand("fail.after.output", out CommandResult result), Is.False);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(console.History.Entries.OfType<CommandOutputEntry>().Single().PlainText, Is.EqualTo("Partial work"));
        Assert.That(console.History.Entries.Last(), Is.TypeOf<CommandFailureEntry>());
        Assert.That(terminal!.Outcome, Is.EqualTo(CommandProgressOutcome.Failed));
        Assert.That(lastEntryAtTerminal, Is.TypeOf<CommandFailureEntry>());
    }

    [Test]
    public async Task CancellationAfterStreaming_KeepsOutputAndSendsCancelledTerminalNotification()
    {
        using var cancellation = new CancellationTokenSource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var console = new ConsoleManager();
        CommandProgressOutcome? terminalOutcome = null;
        console.CommandProgressChanged += (_, args) =>
        {
            if (args.Kind == CommandProgressEventKind.Completed)
            {
                terminalOutcome = args.Outcome;
            }
        };
        console.RegisterCommand(new CommandBuilder("cancel.after.output")
            .ExecuteAsync(async context =>
            {
                context.WriteOutput(CommandOutput.Inline("Started work"));
                context.ReportProgress("Running");
                started.SetResult();
                await Task.Delay(Timeout.Infinite, context.CancellationToken);
                return CommandResult.Success();
            })
            .Build());

        Task<CommandResult> execution = console.TryExecuteCommandAsync("cancel.after.output", cancellation.Token);
        await started.Task;
        cancellation.Cancel();
        CommandResult result = await execution;

        Assert.That(result.Failure!.Code, Is.EqualTo(ConsoleFailureCodes.CommandExecutionCancelled));
        Assert.That(terminalOutcome, Is.EqualTo(CommandProgressOutcome.Cancelled));
        Assert.That(console.History.Entries.OfType<CommandOutputEntry>().Single().PlainText, Is.EqualTo("Started work"));
        Assert.That(console.History.Entries.Last(), Is.TypeOf<CommandFailureEntry>());
    }

    [Test]
    public void NoProgressReport_ProducesNoProgressNotifications_AndContextCannotWriteAfterCompletion()
    {
        var console = new ConsoleManager();
        CommandContext? captured = null;
        int notifications = 0;
        console.CommandProgressChanged += (_, _) => notifications++;
        console.RegisterCommand(new CommandBuilder("no.progress")
            .Execute(context =>
            {
                captured = context;
                return CommandResult.Success();
            })
            .Build());

        console.ExecuteCommand("no.progress");

        Assert.That(notifications, Is.Zero);
        Assert.Throws<InvalidOperationException>(() => captured!.WriteOutput(CommandOutput.Inline("late")));
        Assert.Throws<InvalidOperationException>(() => captured!.ReportProgress("late"));
        Assert.Throws<InvalidOperationException>(() => captured!.GetProgressState<ProgressState>());
    }

    [Test]
    public void ProgressStateFactoryFailure_BecomesStructuredExecutionFailure()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("bad.progress.factory")
            .ProgressState<ProgressState>(() => throw new InvalidOperationException("factory failed"))
            .Execute(context => CommandResult.Success())
            .Build());

        Assert.That(console.TryExecuteCommand("bad.progress.factory", out CommandResult result), Is.False);
        Assert.That(result.Failure!.Code, Is.EqualTo(ConsoleFailureCodes.CommandExecutionRejected));
        Assert.That(result.Failure.Cause!.Message, Is.EqualTo("factory failed"));
        Assert.That(console.History.Entries.Last(), Is.TypeOf<CommandFailureEntry>());
    }

    [Test]
    public void ProgressSubscriberException_FollowsSynchronousEventBehavior()
    {
        var console = new ConsoleManager();
        console.CommandProgressChanged += (_, args) =>
        {
            if (args.Kind == CommandProgressEventKind.Updated)
            {
                throw new InvalidOperationException("subscriber failed");
            }
        };
        console.RegisterCommand(new CommandBuilder("progress.subscriber.failure")
            .Execute(context =>
            {
                context.ReportProgress("notify");
                return CommandResult.Success();
            })
            .Build());

        Assert.That(console.TryExecuteCommand("progress.subscriber.failure", out CommandResult result), Is.False);
        Assert.That(result.Failure!.Code, Is.EqualTo(ConsoleFailureCodes.CommandExecutionRejected));
        Assert.That(console.History.Entries.Last(), Is.TypeOf<CommandFailureEntry>());
    }

    private sealed class ProgressState
    {
        public float Percent { get; set; }
    }
}
