# Command permissions and availability

Command access checks are optional. Commands without permission requirements work with a plain `ConsoleManager`. A command can require caller permissions, be available only under runtime conditions, or use both checks.

This guide documents the command access APIs introduced in version 0.4.0. They are included in the 0.6.0 package alongside the redesigned command value model and configurable help.

## Configure the caller's permissions

Pass a manager-scoped permission provider when creating the console. The provider represents the caller associated with that console. `ConsolePermissionSet` supplies direct grants, reusable role grants, explicit denials, and runtime updates:

```csharp
var permissions = new ConsolePermissionSet()
    .Allow("admin.kick")
    .AddRole(new ConsoleRole("moderator", "chat.mute", "chat.kick"))
    .Deny("chat.kick");

var console = new ConsoleManager(new ConsoleManagerOptions(), permissions);
```

Permission names use dot-separated non-empty segments and are matched case-insensitively. Grant and denial rules can use a terminal `.*` wildcard: `admin.*` matches `admin.kick` and `admin.ban`, but not `admin`. A matching denial overrides direct and role grants. Role grants compose with direct grants; roles do not carry denials.

`Allow`, `Deny`, `RemoveGrant`, `RemoveDenial`, `AddRole`, and `RemoveRole` update a `ConsolePermissionSet` at runtime. A custom caller policy can implement `IConsolePermissionProvider` instead. The provider reference belongs to the manager and is evaluated when execution or autocomplete is requested; it is not copied into `ConsoleManager.Options`.

If a command has permission requirements and no provider was supplied, the command is denied. A manager with no permission-restricted commands needs no provider.

## Declare requirements and availability

Every `RequirePermission(...)` call adds a required permission, so multiple calls combine with AND. `RequireAnyPermission(...)` adds a group where at least one permission must be granted. Multiple groups combine with AND:

```csharp
console.RegisterCommand(new CommandBuilder("server.restart")
    .RequirePermission("server.restart")
    .RequireAnyPermission("server.admin", "server.operator")
    .AvailableWhen(context => server.IsReady
        ? CommandAvailabilityResult.Available()
        : CommandAvailabilityResult.Unavailable("The server is still starting."))
    .Execute(_ => CommandResult.Success())
    .Build());
```

Availability is separate from authorization: permissions answer whether this caller may run the command; availability answers whether runtime conditions currently allow it. Each `AvailableWhen(...)` check must return `CommandAvailabilityResult.Available()` for execution to continue. Checks can return `Unavailable(reason)` to provide a useful message. When called during execution, `CommandAvailabilityContext.Command` contains the parsed command. During autocomplete, it is `null`; use the context's `Input`, `CursorIndex`, and `Definition` or the application's current state.

## Execution behavior

The manager parses the input and records command input history and enabled echo as usual. It then checks permissions and availability before normal command validation and before invoking the handler. A blocked command returns a structured failure from `TryExecuteCommand` and records a `CommandFailureEntry`; `ExecuteCommand` throws `ConsoleOperationException` carrying the same failure.

Permission failures use `ConsoleFailureKind.CommandAuthorization` and `ConsoleFailureCodes.CommandPermissionDenied`. Availability failures use `ConsoleFailureKind.CommandAvailability` and `ConsoleFailureCodes.CommandUnavailable`. Exceptions from a permission provider or availability check become structured extension failures during execution. Parsing and `ValidateCommand(...)` remain inspection operations and do not run access checks.

## Autocomplete visibility

By default, `GetAutocomplete(...)` omits commands the current caller cannot run or that are currently unavailable. Set `Autocomplete.ShowUnavailableCommands` to `true` to include blocked command path suggestions. Such candidates expose `IsAvailable` and `UnavailableReason` so the host can display a locked state or explanation. Autocomplete does not offer member/value candidates after a blocked command path has been selected.

Permission providers and availability checks run synchronously during autocomplete. Exceptions propagate to the caller, like exceptions from value candidate providers. Keep those checks fast and side-effect free.

See [Command autocomplete](COMMAND_AUTOCOMPLETE.md), [Manager and configuration](MANAGER_AND_CONFIGURATION.md), and [Command execution](COMMAND_EXECUTION.md) for related behavior.
