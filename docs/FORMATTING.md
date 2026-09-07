# Formatting

Formatting is optional. A default `ConsoleManager` stores plain text and semantic text, but does not parse markup or format output for any engine. Formatting is enabled automatically when a formatter is configured through formatting options.

Enable formatting when you want package-managed markup, themes, and engine-specific output strings:

```csharp
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;

var console = new ConsoleManager(new ConsoleManagerOptions
{
    Formatting = ConsoleFormattingOptions.UnityRichText()
});
```

Godot projects can use:

```csharp
var console = new ConsoleManager(new ConsoleManagerOptions
{
    Formatting = ConsoleFormattingOptions.GodotBbCode()
});
```

## Plain Text

Formatting is string-aware through `ConsoleManager`. When formatting is disabled, strings are stored literally. When formatting is enabled, known markup tags are parsed:

```csharp
console.LogInformation("<style=Success>Console ready.</style>");
```

Use `ConsoleText.Plain(...)` when text must stay literal even if the manager has formatting enabled:

```csharp
var text = ConsoleText.Plain("<b>not bold</b>", defaultStyle: "Information");

Console.WriteLine(text.PlainText); // <b>not bold</b>
```

Use `ConsoleText.EscapeMarkup(...)` when literal text must be inserted inside markup.

## Markup

Markup parsing is manager-owned through normal string APIs:

```csharp
var text = console.CreateText(
    "Gave <style=Player>Workes</style> <style=Amount>7</style> <style=Item>wood</style>",
    defaultStyle: "Success");
```

If formatting is disabled, the same call returns literal plain text.

The standard markup profile supports:

- `<style=Amount>7</style>`
- `<color=#4ade80>Success</color>`
- `<b>bold</b>`
- `<i>italic</i>`
- `<u>underline</u>`

Tags can be nested:

```xml
<style=Success><color=#4ade80><b>Saved</b></color></style>
```

Direct colors are literal foreground-color overrides. They do not look up a theme style.

Supported color formats:

- `#RGB`
- `#RRGGBB`
- `#RRGGBBAA`

Use `&lt;`, `&gt;`, and `&amp;` for literal `<`, `>`, and `&` inside markup text.

The parser is lenient for ordinary angle-bracket text. Unknown tags and non-tag angle-bracket text remain literal:

```csharp
console.LogInformation("Use <something> here");
console.LogInformation("value < 10");
console.LogInformation("List<string>");
```

Known malformed markup throws `FormatException`, such as unclosed `<b>` tags, mismatched known tags, empty style IDs, or invalid color values.

## Themes

`ConsoleTheme` maps semantic style IDs to `ConsoleStyle` values for the active format model:

```csharp
var theme = new ConsoleTheme(new Dictionary<string, ConsoleStyle>
{
    ["Success"] = ConsoleStyle.Standard(
        foregroundColor: ConsoleColor.FromHex("#4ade80"),
        bold: true),
    ["Amount"] = ConsoleStyle.Standard(
        foregroundColor: ConsoleColor.FromHex("#facc15")),
    ["Player"] = ConsoleStyle.Standard(
        foregroundColor: ConsoleColor.FromHex("#22d3ee"),
        bold: true)
});
```

The built-in standard model supports foreground color, bold, italic, and underline. Custom models can define different string-based formatting attributes for custom formatters.

## Formatting Text

Use the manager to format text through the configured formatter:

```csharp
string rendered = console.Format(console.CreateText("<style=Success><b>Saved</b></style>"));
```

Unity formatting emits Unity rich text. Godot formatting emits BBCode. Plain text, semantic entries, and command output remain the stored source of truth.

String-authored command output can be formatted through the manager too:

```csharp
var output = CommandOutput.Inline("<style=Success><b>Saved</b></style>");
string rendered = console.Format(output);
```

## Logs And Inputs

Log entries and command input entries expose semantic content while preserving plain text convenience properties:

```csharp
console.LogInformation("<style=Success>Console ready.</style>");

if (console.History.Entries[0] is LogEntry log)
{
    Console.WriteLine(log.Message);
    Console.WriteLine(log.Content.PlainText);
}
```

`CommandInputEntry.Input`, `LogEntry.Message`, and command output `PlainText` continue to return plain text.
