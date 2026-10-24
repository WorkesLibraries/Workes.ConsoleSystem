using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Entries;
using Workes.ConsoleSystem.Permissions;

namespace Workes.ConsoleSystem.Tests.Commands;

public sealed class CommandAccessTests
{
    [Test]
    public void UnrestrictedCommand_RemainsUsableWithoutPermissionProvider()
    {
        bool invoked = false;
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("status")
            .Execute(_ =>
            {
                invoked = true;
                return CommandResult.Success();
            })
            .Build());

        Assert.That(console.TryExecuteCommand("status", out CommandResult result), Is.True);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(invoked, Is.True);
    }

    [Test]
    public void RequiredPermission_FailsClosedWithoutProviderAndRecordsFailure()
    {
        bool invoked = false;
        var console = new ConsoleManager();
        console.RegisterCommand(ProtectedCommand("admin.kick", () => invoked = true));

        Assert.That(console.TryExecuteCommand("admin.kick", out CommandResult result), Is.False);
        Assert.That(result.Failure!.Kind, Is.EqualTo(ConsoleFailureKind.CommandAuthorization));
        Assert.That(result.Failure.Code, Is.EqualTo(ConsoleFailureCodes.CommandPermissionDenied));
        Assert.That(invoked, Is.False);
        Assert.That(console.CommandHistory.Entries, Is.EqualTo(new[] { "admin.kick" }));
        Assert.That(console.History.Entries[0], Is.TypeOf<CommandInputEntry>());
        Assert.That(console.History.Entries[^1], Is.TypeOf<CommandFailureEntry>());
    }

    [Test]
    public void PermissionProvider_AllowsGrantedCommand()
    {
        bool invoked = false;
        var permissions = new ConsolePermissionSet().Allow("admin.kick");
        var console = new ConsoleManager(new ConsoleManagerOptions(), permissions);
        console.RegisterCommand(ProtectedCommand("admin.kick", () => invoked = true));

        Assert.That(console.TryExecuteCommand("admin.kick", out CommandResult result), Is.True);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(invoked, Is.True);
    }

    [Test]
    public void PermissionSetChanges_AffectTheExistingManager()
    {
        var permissions = new ConsolePermissionSet();
        var console = new ConsoleManager(new ConsoleManagerOptions(), permissions);
        console.RegisterCommand(ProtectedCommand("admin.kick", () => { }));

        Assert.That(console.TryExecuteCommand("admin.kick", out _), Is.False);
        permissions.Allow("admin.kick");
        Assert.That(console.TryExecuteCommand("admin.kick", out _), Is.True);
    }

    [Test]
    public void RequirePermission_RequiresEveryDeclaredPermission()
    {
        var permissions = new ConsolePermissionSet().Allow("admin.kick");
        var console = new ConsoleManager(new ConsoleManagerOptions(), permissions);
        console.RegisterCommand(new CommandBuilder("admin.promote")
            .RequirePermission("admin.kick")
            .RequirePermission("admin.promote")
            .Execute(_ => CommandResult.Success())
            .Build());

        Assert.That(console.TryExecuteCommand("admin.promote", out CommandResult result), Is.False);
        Assert.That(result.Failure!.Code, Is.EqualTo(ConsoleFailureCodes.CommandPermissionDenied));
    }

    [Test]
    public void RequireAnyPermission_AcceptsOneMatchingGrant()
    {
        var permissions = new ConsolePermissionSet().Allow("chat.kick");
        var console = new ConsoleManager(new ConsoleManagerOptions(), permissions);
        console.RegisterCommand(new CommandBuilder("chat.moderate")
            .RequireAnyPermission("chat.mute", "chat.kick")
            .Execute(_ => CommandResult.Success())
            .Build());

        Assert.That(console.TryExecuteCommand("chat.moderate", out CommandResult result), Is.True);
    }

    [Test]
    public void RoleGrant_AllowsCommandAndDirectDenyBlocksIt()
    {
        var permissions = new ConsolePermissionSet()
            .AddRole(new ConsoleRole("moderator", "chat.*"))
            .Deny("chat.ban");
        var console = new ConsoleManager(new ConsoleManagerOptions(), permissions);
        console.RegisterCommand(new CommandBuilder("chat.mute")
            .RequirePermission("chat.mute")
            .Execute(_ => CommandResult.Success())
            .Build());
        console.RegisterCommand(new CommandBuilder("chat.ban")
            .RequirePermission("chat.ban")
            .Execute(_ => CommandResult.Success())
            .Build());

        Assert.That(console.TryExecuteCommand("chat.mute", out _), Is.True);
        Assert.That(console.TryExecuteCommand("chat.ban", out CommandResult denied), Is.False);
        Assert.That(denied.Failure!.Code, Is.EqualTo(ConsoleFailureCodes.CommandPermissionDenied));
    }

    [Test]
    public void AvailabilityCheck_BlocksExecutionAndProvidesReason()
    {
        BoundCommand? observed = null;
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("server.restart")
            .AvailableWhen(context =>
            {
                observed = context.Command;
                return CommandAvailabilityResult.Unavailable("The server is still starting.");
            })
            .Execute(_ => CommandResult.Success())
            .Build());

        Assert.That(console.TryExecuteCommand("server.restart", out CommandResult result), Is.False);
        Assert.That(result.Failure!.Kind, Is.EqualTo(ConsoleFailureKind.CommandAvailability));
        Assert.That(result.Failure.Code, Is.EqualTo(ConsoleFailureCodes.CommandUnavailable));
        Assert.That(result.Failure.Message, Is.EqualTo("The server is still starting."));
        Assert.That(observed, Is.Not.Null);
    }

    [Test]
    public void AvailabilityCheckException_BecomesStructuredExtensionFailure()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("server.restart")
            .AvailableWhen(_ => throw new InvalidOperationException("availability failed"))
            .Execute(_ => CommandResult.Success())
            .Build());

        Assert.That(console.TryExecuteCommand("server.restart", out CommandResult result), Is.False);
        Assert.That(result.Failure!.Kind, Is.EqualTo(ConsoleFailureKind.Extension));
        Assert.That(result.Failure.Cause!.Message, Is.EqualTo("availability failed"));
    }

    [Test]
    public void PermissionProviderException_BecomesStructuredExtensionFailure()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions(), new ThrowingPermissionProvider());
        console.RegisterCommand(ProtectedCommand("admin.kick", () => { }));

        Assert.That(console.TryExecuteCommand("admin.kick", out CommandResult result), Is.False);
        Assert.That(result.Failure!.Kind, Is.EqualTo(ConsoleFailureKind.Extension));
        Assert.That(result.Failure.Cause!.Message, Is.EqualTo("permission provider failed"));
    }

    [Test]
    public void Autocomplete_HidesBlockedCommandsByDefault()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(ProtectedCommand("admin.kick", () => { }));
        console.RegisterCommand(new CommandBuilder("status")
            .Execute(_ => CommandResult.Success())
            .Build());

        var result = console.GetAutocomplete(string.Empty, 0);

        Assert.That(result.Candidates.Select(candidate => candidate.Text), Is.EqualTo(new[] { "status" }));
    }

    [Test]
    public void Autocomplete_CanShowBlockedCommandsWithReason()
    {
        var options = new ConsoleManagerOptions
        {
            Autocomplete = new CommandAutocompleteOptions { ShowUnavailableCommands = true }
        };
        var console = new ConsoleManager(options);
        console.RegisterCommand(ProtectedCommand("admin.kick", () => { }));

        var result = console.GetAutocomplete("admin", 5);
        CommandAutocompleteCandidate candidate = result.Candidates.Single();

        Assert.That(candidate.Text, Is.EqualTo("admin.kick"));
        Assert.That(candidate.IsAvailable, Is.False);
        Assert.That(candidate.UnavailableReason, Does.Contain("admin.kick"));
    }

    [Test]
    public void Autocomplete_AvailableCommandWithPermissionGrant_IsVisible()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions(), new ConsolePermissionSet().Allow("admin.kick"));
        console.RegisterCommand(ProtectedCommand("admin.kick", () => { }));

        var candidate = console.GetAutocomplete("admin", 5).Candidates.Single();

        Assert.That(candidate.IsAvailable, Is.True);
    }

    [Test]
    public void Autocomplete_RejectsInvalidCursorBeforeCallingAccessProviders()
    {
        bool called = false;
        var permissions = new ThrowingPermissionProvider();
        var console = new ConsoleManager(new ConsoleManagerOptions(), permissions);
        console.RegisterCommand(new CommandBuilder("admin.kick")
            .RequirePermission("admin.kick")
            .AvailableWhen(_ =>
            {
                called = true;
                return CommandAvailabilityResult.Available();
            })
            .Execute(_ => CommandResult.Success())
            .Build());

        Assert.Throws<ArgumentOutOfRangeException>(() => console.GetAutocomplete("admin", 6));
        Assert.That(called, Is.False);
    }

    [Test]
    public void Autocomplete_DoesNotSuggestMembersForBlockedCommand()
    {
        var options = new ConsoleManagerOptions
        {
            Autocomplete = new CommandAutocompleteOptions { ShowUnavailableCommands = true }
        };
        var console = new ConsoleManager(options);
        console.RegisterCommand(new CommandBuilder("admin.kick")
            .RequirePermission("admin.kick")
            .Flag<KickState>(state => state.Silent, "silent", "silent")
            .Execute<KickState>((_, _) => CommandResult.Success())
            .Build());

        var result = console.GetAutocomplete("admin.kick --", "admin.kick --".Length);

        Assert.That(result.Candidates, Is.Empty);
    }

    [Test]
    public void AvailabilityCheck_IsAppliedToAutocompleteAndCarriesReason()
    {
        var options = new ConsoleManagerOptions
        {
            Autocomplete = new CommandAutocompleteOptions { ShowUnavailableCommands = true }
        };
        var console = new ConsoleManager(options);
        console.RegisterCommand(new CommandBuilder("server.restart")
            .AvailableWhen(context => context.CursorIndex is null
                ? CommandAvailabilityResult.Available()
                : CommandAvailabilityResult.Unavailable("Server is starting."))
            .Execute(_ => CommandResult.Success())
            .Build());

        var candidate = console.GetAutocomplete("server", 6).Candidates.Single();

        Assert.That(candidate.IsAvailable, Is.False);
        Assert.That(candidate.UnavailableReason, Is.EqualTo("Server is starting."));
    }

    [Test]
    public void PermissionSetAndOptionsArePassedAsConsoleDependencies()
    {
        var provider = new ConsolePermissionSet().Allow("admin.kick");
        var options = new ConsoleManagerOptions { Autocomplete = new CommandAutocompleteOptions { ShowUnavailableCommands = true } };
        var console = new ConsoleManager(options, provider);
        console.RegisterCommand(ProtectedCommand("admin.kick", () => { }));
        options.Autocomplete.ShowUnavailableCommands = false;

        Assert.That(console.Options.Autocomplete.ShowUnavailableCommands, Is.True);
        Assert.That(console.TryExecuteCommand("admin.kick", out _), Is.True);
    }

    private static CommandDefinition ProtectedCommand(string permission, Action onExecute)
    {
        return new CommandBuilder(permission)
            .RequirePermission(permission)
            .Execute(_ =>
            {
                onExecute();
                return CommandResult.Success();
            })
            .Build();
    }

    private sealed record KickState(bool Silent);

    private sealed class ThrowingPermissionProvider : IConsolePermissionProvider
    {
        public bool HasPermission(string permission) => throw new InvalidOperationException("permission provider failed");
    }
}
