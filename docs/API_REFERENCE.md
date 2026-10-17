# API reference

This page maps the public API and records the behavior that is easiest to miss when using the focused guides. All types belong to `Workes.ConsoleSystem`; the namespaces below group them by role. ContentSystem types are implementation details and are not part of the public API.

## Main workflow

Create one `ConsoleManager` per console instance. Configure it once, register definitions, and use its methods to log, inspect input, execute commands, and record custom entries. The manager coordinates the visible history and the separate command input history.

| Operation | API | Behavior |
| --- | --- | --- |
| Inspect without executing | `ParseCommand(string)` then `ValidateCommand(BoundCommand)` | Neither method invokes a handler or changes either history. |
| Suggest input completions | `GetAutocomplete(string, int)` | Returns candidates and the replacement range; on current `main`, added after released 0.2.0. |
| Execute input | `TryExecuteCommand(string, out CommandResult)` | Returns `false` and a structured failure for parse, validation, or handler failures. |
| Execute input and throw on failure | `ExecuteCommand(string)` | Returns a successful result; throws `ConsoleOperationException` with the structured failure otherwise. |
| Execute a parsed command | `TryExecuteCommand(BoundCommand, out CommandResult)` or `ExecuteCommand(BoundCommand)` | Skips parsing but still records the original input, validates, invokes the handler, and records the outcome. |
| Register definitions | `RegisterCommand(CommandDefinition)` or `RegisterCommands(IEnumerable<CommandDefinition>)` | Batch registration validates every definition before adding any of them. |
| Add a custom visible entry | `RecordEntry(IConsoleEntry)` | Adds the entry to the same bounded history as logs and command entries. |
| Add command navigation input | `RecordCommandInput(string)` | Returns `false` for blank input or a suppressed consecutive duplicate. |
| Write logs | `LogTrace`, `LogDebug`, `LogInformation`, `LogWarning`, `LogError`, `LogCritical` | Each method accepts either `string` or `ConsoleText` and records a `LogEntry`. |

Null inputs to manager methods that require an object throw `ArgumentNullException`. Invalid command definitions, duplicate command paths, and conflicting flag/option names are rejected during build or registration. The manager has no unregister or replacement operation in this release.

## Public types by namespace

### `Workes.ConsoleSystem.Core`

- `ConsoleManager`: root for configuration, logging, histories, registration, parsing, validation, autocomplete, and execution. `Options` returns a detached snapshot. `GetAutocomplete(string, int)` is on current `main`; it was added after the released 0.2.0 package.
- `ConsoleFailure`: structured failure data (`Kind`, stable `Code`, `Message`, optional `Component`, `Source`, and nested `Cause`). Use codes or kinds for program logic, not message text.
- `ConsoleFailureKind`, `ConsoleFailureCodes`, and `ConsoleFailures`: failure categories, built-in stable codes, and helper factories.
- `ConsoleSystemException` and `ConsoleOperationException`: exceptions for expected-success APIs that fail. Both expose `Failure`; `ConsoleOperationException` is used by `ExecuteCommand`. A handler exception is retained as `InnerException` and in the failure cause chain.

### `Workes.ConsoleSystem.Configuration`

- `ConsoleManagerOptions`: root settings with `CommandParsing`, `Autocomplete`, `History`, and `Execution` sections.
- `CommandParsingOptions` and `BooleanLiteralOptions`: tokenization, name matching, option syntax, interleaving, and accepted boolean literals.
- `HistoryOptions` and `CommandHistoryDuplicatePolicy`: independent visible and command-input capacities, plus consecutive duplicate handling.
- `CommandExecutionOptions`: default command input echo.
- `CommandAutocompleteOptions` and `CommandPathCompletionMode`: control full-path or dot-segment suggestions on current `main`; the released 0.2.0 package exposes the settings but no completion operation.
- `OptionValueStyle`: `SpaceSeparated`, `EqualSeparated`, or `AnySeparated` option values.

Manager construction snapshots and validates the full options graph. Null nested sections resolve to defaults. Capacities must be positive; enum settings must be defined values; boolean literal arrays must be non-empty, contain no blank or duplicate literals, and must not overlap under the configured case comparison; the option prefix must be non-blank and contain no whitespace. Literal arrays are copied into the snapshot.

### `Workes.ConsoleSystem.Commands`

- Definition: `CommandBuilder`, `CommandDefinition`, `CommandArgumentDefinition`, `CommandFlagDefinition`, `CommandOptionDefinition`, `CommandConstraintDefinition`, `CommandSuccessOutputDefinition`, and `CommandEchoInputDefinition`.
- Autocomplete: `CommandAutocompleteContext`, `CommandAutocompleteCandidate`, `CommandAutocompleteCandidateKind`, and `CommandAutocompleteResult`.
- Parsing and validation: `BoundCommand`, `CommandParseResult`, and `CommandValidationResult`.
- Handler execution: `CommandContext` and `CommandResult`.
- Output: `CommandOutput`, `CommandOutputBuilder`, and `CommandOutputKind` (`Inline` or `Block`).
- Registered-command view: `CommandSystem`, available as `ConsoleManager.Commands`; its `Definitions` property is a read-only view.

Use `CommandBuilder` for definitions and `CommandResult.Success(...)` / `Failed(...)` for handler outcomes. There is no public constructor for `CommandDefinition` or `CommandResult`. The builder requires an execution handler before `Build()` succeeds. On current `main`, `ValueCandidates(...)` attaches an autocomplete provider to the latest positional argument or option, while `SuggestAllowedValues()` opts the latest option into suggestions from its `AllowedValues` set. Explicit providers take precedence.

For typed commands, all bindings and the handler must use the same reference type. Binding expressions must select public instance properties. The state type must expose a public constructor whose parameter names match bound property names; matching follows the configured case-sensitivity setting. Constructor parameters receive values by name, so their order need not match schema order. Immutable records are a convenient state shape. Bind every value the handler needs to a constructor-backed property.

The parsed `BoundCommand` exposes read-only dictionaries of argument, flag, and option values keyed by canonical schema name. Omitted flags are `false`. Omitted options receive the configured default, or the target type's default (`null` for reference/nullable types and the zero value for non-nullable value types). A supplied option value is converted before validation.

### `Workes.ConsoleSystem.Entries`

- `IConsoleEntry`: custom entry contract with a `DateTimeOffset Timestamp`.
- `LogEntry`: `Level`, semantic `Content`, and derived plain `Message`.
- `CommandInputEntry`: semantic `Content` and derived plain `Input`.
- `CommandOutputEntry`: the `CommandOutput`, plus derived `PlainText`.
- `CommandFailureEntry`: structured `Failure`, semantic `Content`, and derived plain `Message`.

Entries are retained in the visible history as the same objects that were recorded. Treat custom entries as immutable after recording. `ConsoleHistory.Entries` returns a detached collection snapshot; the entry objects themselves are not cloned.

### `Workes.ConsoleSystem.History`

- `ConsoleHistory`: bounded visible entries, oldest-first detached `Entries`, `Capacity`, and `Clear()`.
- `CommandHistory`: bounded submitted strings, oldest-first detached `Entries`, `Capacity`, and `Clear()`.

`CommandHistory` has a public parameterless constructor for an independent empty history. Its add operation is manager-owned, so use `ConsoleManager.CommandHistory` when you want command execution or `RecordCommandInput` to populate it. Command input history trims only for duplicate comparison; it retains the submitted string unchanged. `RejectConsecutive` compares the trimmed newest and submitted values using ordinal case-insensitive comparison. When full, either history drops its oldest item to retain a new one.

### `Workes.ConsoleSystem.Presentation`

- `ConsoleText`: immutable semantic text, created with `Plain(...)` or `Build(...)`.
- `ConsoleTextBuilder`: appends literal `Text(...)` and semantic `Value(...)` segments, then creates `ConsoleText` with `Build()`.
- `ConsoleTextSegment`: a text value with optional `SemanticId` and associated `Data`.

`PlainText` concatenates segment text. An explicit segment semantic ID takes precedence over the containing text's default ID. The package does not interpret or render IDs or data.

### `Workes.ConsoleSystem.Logging`

- `LogLevel`: `Trace`, `Debug`, `Information`, `Warning`, `Error`, and `Critical`.

There is no public logger object. Every level is recorded; the host can filter entries after reading history.

## Command parsing details

The first token is the complete command path. Paths may contain dot-separated segments, but cannot be blank, contain whitespace, or have empty segments. Matching is case-insensitive by default. Parsing requires a complete registered path; there is no command-path fallback. Current `main` additionally provides a separate autocomplete API for command paths.

Positional arguments are required and bind in schema order. With `AllowInterleavedOptions = false`, they must all appear before the first named member. With it enabled, flags and options may occur between positional values, while positional values still bind in their original order. Named members may be reordered in either mode. A flag is presence-based (`--safe`) and cannot take a value. An option consumes a following token or uses `=` according to `OptionValueStyle`.

When quoted strings are enabled, a token may be enclosed in matching single or double quotes. Backslash escapes the active quote or another backslash. Quoted tokens are treated as values even when their text starts with the configured flag/option prefix. This parser does not support shell expansion, concatenated quoted/unquoted fragments, or escape sequences beyond those two cases.

Strings, booleans, enums, nullable forms of supported types, and values accepted by invariant-culture `Convert.ChangeType` can be bound. Boolean literals default to `true` and `false`; enum names and boolean literals follow the configured case comparison. A flag cannot be written as `--safe=true`.

`CommandParseResult.Failure` describes a parse or binding rejection. `SourceOffset` and `SourceLength` are zero-based character coordinates into the original input when a specific span is available; they may be null for failures without a useful span. `CommandValidationResult.Failure` describes an option range, allowed-value, or custom constraint rejection. Constraint predicates run in declaration order after option checks; a thrown predicate becomes structured failure data and prevents handler execution.

## Failure handling

Use `TryExecuteCommand` when rejection is an expected branch. The result exposes `IsSuccess`, optional `Failure`, and `Outputs`. A failed handler result may contain outputs, but those outputs are not written to visible history. For successful execution, declared command success outputs are recorded first, followed by the outputs in the handler's result.

Use `ExecuteCommand` when success is expected. A rejected command throws `ConsoleOperationException`, whose `Failure` is the same structured result available from the try-based API. Null method arguments and invalid setup/programming inputs remain standard .NET argument or invalid-operation exceptions; they are not converted into command failures.

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

The complete constant list is in `ConsoleFailureCodes`. Messages are for people and may change; branch on `Kind` and `Code`. Failures created for thrown handlers include the original exception details through nested causes. See [Command outputs and failures](COMMAND_OUTPUTS_AND_FAILURES.md) for construction examples.

## Scope of version 0.2.0

The released 0.2.0 package provides synchronous command handlers, retained in-memory histories, semantic text data, and pull-based history reads. Current `main` also implements stateless autocomplete for a future package release. The package does not include history change events, persistence, asynchronous handlers, UI controls, markup parsing, or a renderer. The host owns presentation, input widgets, filtering, persistence, and thread coordination.

## Focused guides

- [Manager and configuration](MANAGER_AND_CONFIGURATION.md)
- [Logging](LOGGING.md)
- [Entries and semantic text](ENTRIES_AND_TEXT.md)
- [Console and command histories](HISTORY.md)
- [Command registration](COMMAND_REGISTRATION.md)
- [Command parsing](COMMAND_PARSING.md)
- [Command validation](COMMAND_VALIDATION.md)
- [Command autocomplete](COMMAND_AUTOCOMPLETE.md) (current `main`; not included in released 0.2.0)
- [Command execution](COMMAND_EXECUTION.md)
- [Command outputs and failures](COMMAND_OUTPUTS_AND_FAILURES.md)
- [Quick Start](QUICK_START.md)
