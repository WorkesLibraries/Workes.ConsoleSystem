# Command parsing

`ConsoleManager.ParseCommand(input)` matches input against registered definitions and returns a `CommandParseResult`. Parsing does not invoke a handler, add history entries, or change command history.

On success, `CommandParseResult.Command` is a `BoundCommand` with the original input, matched definition, typed state, and read-only value dictionaries. `Values` contains every resolved logical state value keyed by ID. `Arguments`, `Flags`, and `Options` contain only values explicitly supplied through that input binding and are keyed by canonical name. Use `GetState<TState>()` to retrieve typed state. On failure, inspect `Failure`; `SourceOffset` and `SourceLength` identify the relevant input span when available.

## Positional values and named members

Required positional values bind in declaration order. A command may add one optional positional value with `.Optional()`; it must be the last positional argument. If omitted, parsing continues to named flags and options. The optional argument may share its ID with one option targeting the same state property and type, so callers may use either input form. Supplying both forms for the same ID fails.

With the default `AllowInterleavedOptions = false`, supplied positional values precede named members. An omitted optional trailing value does not block a named member:

```text
player.teleport Alex --destination nether
```

With `AllowInterleavedOptions = true`, flags and options may appear before, between, or after positional values, while positional values still bind in their declared order:

```text
player.teleport --destination nether Alex
```

In both modes, named flags and options can be reordered. A quoted token that starts with the configured flag/option prefix is treated as a value, not as a named member.

## Value resolution

For a non-flag ID, an explicit supplied value wins over its configured `Defaults(...)` entry. If neither exists, the value resolves to `default(T)`—`null` for reference or nullable types and the zero value for non-nullable value types. Flags resolve to `true` when present and `false` when absent; flag defaults are invalid. Defaults for required positional values, unknown IDs, duplicate ID defaults, and incompatible default values are rejected while building the command.

The `Arguments`, `Flags`, and `Options` source views do not include defaults or type defaults. Use `Values` or the typed state when resolved values are needed.

## Option value syntax

The default prefix is `--`. A flag is written by itself, such as `--safe`. A named option consumes a following value by default, such as `--destination nether`. `OptionValueStyle` can instead require equals syntax (`--destination=nether`) or allow either form:

- `SpaceSeparated`: `--destination nether`
- `EqualSeparated`: `--destination=nether`
- `AnySeparated`: either form

`FlagAndOptionPrefix` can be customized. Command paths and names are matched case-insensitively by default. `AllowQuotedStrings` defaults to `true`; set `IsCaseSensitive` to `true` to match paths, names, and boolean literals exactly.

Boolean option values use `BooleanLiterals.TrueLiterals` and `FalseLiterals`, which default to `true` and `false`. A flag is presence-based and does not accept an explicit value.

Parsing converts supported primitive, nullable, and enum values into their declared types. Missing required arguments, unknown names, repeated named members, repeated input sources for one ID, extra positional values, malformed quotes, or values that cannot be converted produce structured failures instead of invoking the command.

See [Manager and configuration](MANAGER_AND_CONFIGURATION.md) for defaults and [validation](COMMAND_VALIDATION.md) for what happens after binding.
