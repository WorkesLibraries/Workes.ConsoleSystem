# API reference

This page maps the public API and records the behavior that is easiest to miss when using the focused guides. All types belong to `Workes.ConsoleSystem`; the namespaces below group them by role. ContentSystem types are implementation details and are not part of the public API.

## Main workflow

Create one `ConsoleManager` per console instance. Configure it once, register definitions, and use its methods to log, inspect input, execute commands, and record custom entries. The manager coordinates the visible history and the separate command input history.

| Operation | API | Behavior |
| --- | --- | --- |
| Inspect without executing | `ParseCommand(string)` then `ValidateCommand(BoundCommand)` | Neither method invokes a handler or changes either history. |
| Suggest input completions | `GetAutocomplete(string, int)` | Returns candidates and replacement range; blocked commands are hidden by default. |
| Execute input | `TryExecuteCommand(string, out CommandResult)` | Returns `false` and a structured failure for parse, validation, or handler failures. |
| Execute input and throw on failure | `ExecuteCommand(string)` | Returns a successful result; throws `ConsoleOperationException` with the structured failure otherwise. |
| Execute a parsed command | `TryExecuteCommand(BoundCommand, out CommandResult)` or `ExecuteCommand(BoundCommand)` | Skips parsing but still records the original input, validates, invokes the handler, and records the outcome. |
| Execute asynchronously | `TryExecuteCommandAsync(...)` or `ExecuteCommandAsync(...)` | Accepts either input or `BoundCommand`; the try form returns `Task<CommandResult>`, and the execute form throws for command failures. Both run synchronous or asynchronous handlers. |
| Register definitions | `RegisterCommand(CommandDefinition)` or `RegisterCommands(IEnumerable<CommandDefinition>)` | Batch registration validates every definition before adding any of them. |
| Register optional help | `RegisterHelpCommand(HelpCommandDefinition)` | Creates a normal registered command bound to this manager's current registry and caller access checks. |
| Add a custom visible entry | `RecordEntry(IConsoleEntry)` | Adds the entry to the same bounded history as logs and command entries. |
| Add command navigation input | `RecordCommandInput(string)` | Returns `false` for blank input or a suppressed consecutive duplicate. |
| Write logs | `LogTrace`, `LogDebug`, `LogInformation`, `LogWarning`, `LogError`, `LogCritical` | Each method accepts either `string` or `ConsoleText` and records a `LogEntry`. |

Null inputs to manager methods that require an object throw `ArgumentNullException`. Invalid command definitions, duplicate command paths, and conflicting flag/option names are rejected during build or registration. The manager has no unregister or replacement operation in this release.

## Public types by namespace

### `Workes.ConsoleSystem.Core`

- `ConsoleManager`: root for configuration, logging, histories, registration, parsing, validation, autocomplete, and sync/async execution. Use its constructor overload accepting `IConsolePermissionProvider` for caller-specific access checks; the three-argument overload also accepts an `ICommandDispatcher`. `Options` returns a detached configuration snapshot.
- `ConsoleFailure`: structured failure data (`Kind`, stable `Code`, `Message`, optional `Component`, `Source`, and nested `Cause`). Use codes or kinds for program logic, not message text.
- `ConsoleFailureKind` and `ConsoleFailureCodes`: failure categories and built-in stable codes, including permission and availability rejections.
- `ConsoleSystemException` and `ConsoleOperationException`: exceptions for expected-success APIs that fail. Both expose `Failure`; `ConsoleOperationException` is used by `ExecuteCommand`. A handler exception is retained as `InnerException` and in the failure cause chain.

### `Workes.ConsoleSystem.Configuration`

- `ConsoleManagerOptions`: root settings with `CommandParsing`, `Autocomplete`, `History`, and `Execution` sections.
- `CommandParsingOptions` and `BooleanLiteralOptions`: tokenization, name matching, option syntax, interleaving, and accepted boolean literals.
- `HistoryOptions` and `CommandHistoryDuplicatePolicy`: independent visible and command-input capacities, plus consecutive duplicate handling.
- `CommandExecutionOptions`: default command input echo.
- `CommandAutocompleteOptions` and `CommandPathCompletionMode`: control full-path or dot-segment suggestions and whether blocked commands appear.
- `OptionValueStyle`: `SpaceSeparated`, `EqualSeparated`, or `AnySeparated` option values.

Manager construction snapshots and validates the full options graph. Null nested sections resolve to defaults. Capacities must be positive; enum settings must be defined values; boolean literal arrays must be non-empty, contain no blank or duplicate literals, and must not overlap under the configured case comparison; the option prefix must be non-blank and contain no whitespace. Literal arrays are copied into the snapshot.

### `Workes.ConsoleSystem.Commands`

- Definition: `CommandBuilder`, `CommandDefinition`, `CommandArgumentDefinition`, `CommandFlagDefinition`, `CommandOptionDefinition`, `CommandConstraintDefinition`, `CommandSuccessOutputDefinition`, and `CommandEchoInputDefinition`.
- Access: `CommandAvailabilityContext` and `CommandAvailabilityResult`.
- Help: `HelpCommandBuilder`, `HelpCommandDefinition`, `CommandHelpContext`, `CommandHelpAccessContext`, and the help access policy/decision types.
- Autocomplete: `CommandAutocompleteContext`, `CommandAutocompleteCandidate`, `CommandAutocompleteCandidateKind`, and `CommandAutocompleteResult`.
- Parsing and validation: `BoundCommand`, `CommandParseResult`, and `CommandValidationResult`.
- Handler execution: `CommandContext`, `CommandResult`, and `ICommandDispatcher`.
- Output: `CommandOutput`, `CommandOutputBuilder`, and `CommandOutputKind` (`Inline` or `Block`).
- Registered-command view: `CommandSystem`, available as `ConsoleManager.Commands`; its `Definitions` property is a read-only view.

Use `CommandBuilder` and choose either `Execute(...)` or `ExecuteAsync(...)`; a definition cannot combine synchronous and asynchronous handlers. Task-only async handlers produce a successful empty result when they complete. Use `CommandResult.Success(...)` / `Failed(...)` for explicit outcomes. There is no public constructor for `CommandDefinition` or `CommandResult`. The builder requires an execution handler before `Build()` succeeds. Every argument, flag, and option has a required logical ID as well as its canonical input name. IDs use ordinal comparison; one optional positional argument may share an ID with an option when both bind the same property and type. `Defaults((id, value), ...)` configures resolved defaults by ID. `ValueCandidates(...)` and `SuggestAllowedValues()` apply to the latest positional argument or option; explicit providers take precedence. `RequirePermission(...)`, `RequireAnyPermission(...)`, and `AvailableWhen(...)` add optional access rules.

For typed commands, all bindings and the handler must use the same reference type. Binding expressions must select public instance properties. The state type must expose a public constructor whose parameter names match bound property names; matching follows the configured case-sensitivity setting. Constructor parameters receive values by name, so their order need not match schema order. Immutable records are a convenient state shape. Bind every value the handler needs to a constructor-backed property.

The parsed `BoundCommand` exposes `Values`, a read-only dictionary of every resolved logical value keyed by ID, including defaults and `default(T)`. `Arguments`, `Flags`, and `Options` are source views keyed by canonical name and contain only explicitly supplied inputs. Non-flag values resolve from explicit input, then `Defaults(...)`, then the target type's default (`null` for reference/nullable types and the zero value for non-nullable value types). Flags resolve to `false` when absent and `true` when present. Binding-specific ranges, allowed values, and autocomplete candidates stay on each argument or option binding; state constraints run against the fully resolved state.

### `Workes.ConsoleSystem.Entries`

- `IConsoleEntry`: custom entry contract with a `DateTimeOffset Timestamp`.
- `LogEntry`: `Level`, semantic `Content`, derived plain `Message`, and optional `Source` and `Exception` metadata. The exception reference is retained with the history entry.
- `CommandInputEntry`: semantic `Content` and derived plain `Input`.
- `CommandOutputEntry`: the `CommandOutput`, plus derived `PlainText`.
- `CommandFailureEntry`: structured `Failure`, semantic `Content`, and derived plain `Message`.

Entries are retained in the visible history as the same objects that were recorded. Treat custom entries as immutable after recording. `ConsoleHistory.Entries` returns a detached collection snapshot; the entry objects themselves are not cloned.

### `Workes.ConsoleSystem.History`

- `ConsoleHistory`: bounded visible entries, oldest-first detached `Entries`, `Capacity`, and `Clear()`.
- `CommandHistory`: bounded submitted strings, oldest-first detached `Entries`, `Capacity`, and `Clear()`.
- `HistoryChangeKind`, `ConsoleHistoryChangedEventArgs`, and `CommandHistoryChangedEventArgs`: ConsoleSystem-owned synchronous notifications for added, dropped, and cleared retained items.

`CommandHistory` has a public parameterless constructor for an independent empty history. Its add operation is manager-owned, so use `ConsoleManager.CommandHistory` when you want command execution or `RecordCommandInput` to populate it. Command input history trims only for duplicate comparison; it retains the submitted string unchanged. `RejectConsecutive` compares the trimmed newest and submitted values using ordinal case-insensitive comparison. When full, either history drops its oldest item to retain a new one.

Both history objects expose `Changed`; `ConsoleManager.ConsoleHistoryChanged` and `ConsoleManager.CommandHistoryChanged` are forwarding convenience events. Events run synchronously after mutation, and their argument collections are read-only snapshots. Reads, writes, clears, and notifications are synchronized for overlapping command execution. Notifications run on the writing thread. See [History change hooks](HISTORY_CHANGE_HOOKS.md) for detailed payload and timing behavior.

### `Workes.ConsoleSystem.Presentation`

- `ConsoleText`: immutable semantic text, created with `Plain(...)` or `Build(...)`.
- `ConsoleTextBuilder`: appends literal `Text(...)` and semantic `Value(...)` segments, then creates `ConsoleText` with `Build()`.
- `ConsoleTextSegment`: a text value with optional `SemanticId` and associated `Data`.

`PlainText` concatenates segment text. An explicit segment semantic ID takes precedence over the containing text's default ID. The package does not interpret or render IDs or data.

### `Workes.ConsoleSystem.Logging`

- `LogLevel`: `Trace`, `Debug`, `Information`, `Warning`, `Error`, and `Critical`.

There is no public logger object. Every level is recorded; the host can filter entries after reading history.

### `Workes.ConsoleSystem.Permissions`

- `IConsolePermissionProvider`: supplies the manager-scoped caller's permission decisions.
- `ConsolePermissionSet`: mutable direct grant/denial set with role composition; explicit denials override grants.
- `ConsoleRole`: named, reusable immutable grant collection.

Permission rules accept exact dot-separated paths or a terminal `.*` wildcard. Matching is case-insensitive. Commands with no permission requirements do not need a provider; commands with requirements are denied when the manager has no provider.

## Command parsing details

The first token is the complete command path. Paths may contain dot-separated segments, but cannot be blank, contain whitespace, or have empty segments. Matching is case-insensitive by default. Parsing requires a complete registered path; there is no command-path fallback. The separate autocomplete API can suggest registered command paths without executing them.

Required positional arguments bind in schema order. A command may have one optional trailing positional argument. With `AllowInterleavedOptions = false`, supplied positional values precede named members; omitting the optional argument does not prevent named members. With it enabled, flags and options may occur between or before positional values, while positional values still bind in their original order. One optional positional argument and one option may share an ID to offer alternate input sources, but providing both fails. A flag is presence-based (`--safe`) and cannot take a value. An option consumes a following token or uses `=` according to `OptionValueStyle`.

When quoted strings are enabled, a token may be enclosed in matching single or double quotes. Backslash escapes the active quote or another backslash. Quoted tokens are treated as values even when their text starts with the configured flag/option prefix. This parser does not support shell expansion, concatenated quoted/unquoted fragments, or escape sequences beyond those two cases.

Strings, booleans, enums, nullable forms of supported types, and values accepted by invariant-culture `Convert.ChangeType` can be bound. Boolean literals default to `true` and `false`; enum names and boolean literals follow the configured case comparison. A flag cannot be written as `--safe=true`.

`CommandParseResult.Failure` describes a parse or binding rejection. `SourceOffset` and `SourceLength` are zero-based character coordinates into the original input when a specific span is available; they may be null for failures without a useful span. `CommandValidationResult.Failure` describes an option range, allowed-value, or custom constraint rejection. Constraint predicates run in declaration order after option checks; a thrown predicate becomes structured failure data and prevents handler execution.

## Failure handling

Use `TryExecuteCommand` or `TryExecuteCommandAsync` when rejection is an expected branch. The result exposes `IsSuccess`, optional `Failure`, and `Outputs`. A failed handler result may contain outputs, but those outputs are not written to visible history. For successful execution, declared command success outputs are recorded first, followed by the outputs in the handler's result.

Use `ExecuteCommand` or `ExecuteCommandAsync` when success is expected. A rejected command throws `ConsoleOperationException`, whose `Failure` is the same structured result available from the try-based API. Null method arguments and invalid setup/programming inputs remain standard .NET argument or invalid-operation exceptions; they are not converted into command failures.

Async methods accept an optional `CancellationToken`; it is also exposed to the handler through `CommandContext.CancellationToken`. Cancellation is cooperative and reported with `CommandExecutionCancelled` only when the execution token caused an `OperationCanceledException`. `ICommandDispatcher` is an optional host service for specific thread-bound callbacks; it does not schedule the handler itself. See [Command execution](COMMAND_EXECUTION.md) for ordering and examples.

Built-in failure codes use the `workes.console.` prefix. Common command codes include:

| Code constant | Meaning |
| --- | --- |
| `CommandInputEmpty` | Input is blank. |
| `CommandUnknown` | No registered path matched. |
| `CommandArgumentMissing` / `CommandArgumentUnexpected` | Positional input is missing or extra. |
| `CommandMemberUnknown` / `CommandMemberDuplicate` | A named flag/option is unknown or repeated. |
| `CommandOptionValueMissing` / `CommandOptionValueSyntaxInvalid` | An option has no value or uses disallowed syntax. |
| `CommandValueInvalid` / `CommandStateBindingFailed` | A value cannot be converted or the typed state cannot be constructed. |
| `CommandConstraintRejected` | An option rule or state constraint rejected the command. |
| `CommandExecutionRejected` | The handler returned failure or threw. |
| `CommandRequiresAsyncExecution` | A synchronous manager method was used for an async-only command. |
| `CommandExecutionCancelled` | The execution cancellation token cancelled the command. |
| `CommandPermissionDenied` | A required caller permission was not granted (introduced in 0.4.0). |
| `CommandUnavailable` | A runtime availability check blocked the command (introduced in 0.4.0). |

The complete constant list is in `ConsoleFailureCodes`. Messages are for people and may change; branch on `Kind` and `Code`. Failures created for thrown handlers include the original exception details through nested causes. See [Command outputs and failures](COMMAND_OUTPUTS_AND_FAILURES.md) for construction examples.

## Scope of the 0.9.0 package

The 0.9.0 package provides synchronous and asynchronous command handlers, async manager execution, cooperative cancellation, optional host callback dispatch, synchronized in-memory histories, streamed output, transient progress notifications, semantic text, stateless autocomplete, optional history change events, command permissions and availability checks, the redesigned logical command value model, an opt-in configurable help command, and optional source/exception metadata for log entries. It does not include history persistence, UI controls, markup parsing, a renderer, or platform-specific logging adapters. The host owns presentation, input widgets, filtering, persistence, and UI thread coordination.

## Streaming output and progress introduced in 0.8.0

`CommandContext.WriteOutput(CommandOutput)` immediately records a standard output entry. `CommandBuilder.ProgressState<T>(Func<T>)` optionally creates a fresh reference-type state for each execution, which a handler reads with `GetProgressState<T>()` and publishes with `ReportProgress(string)`. The manager's `CommandProgressChanged` event reports updates and, after final history writes, a terminal outcome when at least one update was reported. See [Command execution](COMMAND_EXECUTION.md) and [History change hooks](HISTORY_CHANGE_HOOKS.md) for examples and ordering.

## Focused guides

- [Manager and configuration](MANAGER_AND_CONFIGURATION.md)
- [Logging](LOGGING.md)
- [Adapter author guide](ADAPTER_AUTHOR_GUIDE.md)
- [Entries and semantic text](ENTRIES_AND_TEXT.md)
- [Console and command histories](HISTORY.md)
- [History change hooks](HISTORY_CHANGE_HOOKS.md)
- [Command registration](COMMAND_REGISTRATION.md)
- [Command parsing](COMMAND_PARSING.md)
- [Command validation](COMMAND_VALIDATION.md)
- [Command autocomplete](COMMAND_AUTOCOMPLETE.md)
- [Command permissions and availability](COMMAND_ACCESS.md) (introduced in 0.4.0)
- [Command execution](COMMAND_EXECUTION.md)
- [Configurable help command](HELP_COMMAND.md)
- [Command execution](COMMAND_EXECUTION.md)
- [Command outputs and failures](COMMAND_OUTPUTS_AND_FAILURES.md)
- [Quick Start](QUICK_START.md)
