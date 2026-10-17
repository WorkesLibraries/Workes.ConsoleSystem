# Command parsing

`ConsoleManager.ParseCommand(input)` matches input against registered definitions and returns a `CommandParseResult`. Parsing does not invoke a handler, add history entries, or change command history.

On success, `CommandParseResult.Command` is a `BoundCommand` with the original input, matched definition, typed state, and read-only argument/flag/option dictionaries. Use `GetState<TState>()` to retrieve typed state. On failure, inspect `Failure`; `SourceOffset` and `SourceLength` identify the relevant input span when one is available.

## Positional values and named members

Positional values are required and remain in the order declared by the command schema. Named flags and options can be reordered in either parsing mode.

With the default `AllowInterleavedOptions = false`, all positional arguments must appear before the first flag or option:

```text
move Alex spawn --safe --reason carefully
```

With `AllowInterleavedOptions = true`, flags and options may appear before, between, or after positional values, while each positional value still binds to the next argument in schema order:

```text
move --reason carefully Alex --safe spawn
```

In both examples, `Alex` binds to the first positional parameter and `spawn` to the second. A quoted token that starts with the configured flag/option prefix is treated as a value, not as a named member.

## Option value syntax

The default prefix is `--`. A flag is written by itself, such as `--safe`. A named option consumes a following value by default, such as `--reason carefully`. `OptionValueStyle` can instead require equals syntax (`--reason=carefully`) or allow either form:

- `SpaceSeparated`: `--reason carefully`
- `EqualSeparated`: `--reason=carefully`
- `AnySeparated`: either form

`FlagAndOptionPrefix` can be customized. Command paths and names are matched case-insensitively by default. `AllowQuotedStrings` defaults to `true`; set `IsCaseSensitive` to `true` to match paths and named members exactly, including configured boolean literals.

Boolean option values use `BooleanLiterals.TrueLiterals` and `FalseLiterals`, which default to `true` and `false`. A flag is presence-based and does not accept an explicit value.

Parsing also converts supported primitive, nullable, and enum values into their declared types. Missing required arguments, unknown names, repeated named members, extra positional values, malformed quotes, or values that cannot be converted produce structured failures instead of invoking the command.

See [Manager and configuration](MANAGER_AND_CONFIGURATION.md) for defaults and [validation](COMMAND_VALIDATION.md) for what happens after binding.
