using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Presentation;

namespace Workes.ConsoleSystem.Tests.Core;

public sealed class ConsoleSystemTests
{
    [Test]
    public void Constructor_CreatesCoreComponents()
    {
        var console = new ConsoleManager();

        Assert.That(console.History, Is.Not.Null);
        Assert.That(console.CommandHistory, Is.Not.Null);
        Assert.That(console.Log, Is.Not.Null);
        Assert.That(console.Commands, Is.Not.Null);
    }

    [Test]
    public void Constructor_InitialHistoriesAreEmpty()
    {
        var console = new ConsoleManager();

        Assert.That(console.History.Entries, Is.Empty);
        Assert.That(console.CommandHistory.Entries, Is.Empty);
    }

    [Test]
    public void Constructor_ExposesDefaultResolvedOptions()
    {
        var console = new ConsoleManager();

        Assert.That(console.Options.CommandParsing, Is.Not.Null);
        Assert.That(console.Options.CommandParsing.OptionValueStyle, Is.EqualTo(OptionValueStyle.SpaceSeparated));
        Assert.That(console.Options.CommandParsing.FlagAndOptionPrefix, Is.EqualTo("--"));
        Assert.That(console.Options.CommandParsing.IsCaseSensitive, Is.False);
        Assert.That(console.Options.CommandParsing.AllowQuotedStrings, Is.True);
        Assert.That(console.Options.CommandParsing.AllowFlagsAndOptionsInAnyOrder, Is.True);
        Assert.That(console.Options.CommandParsing.BooleanLiterals.TrueLiterals, Is.EqualTo(new[] { "true" }));
        Assert.That(console.Options.CommandParsing.BooleanLiterals.FalseLiterals, Is.EqualTo(new[] { "false" }));
        Assert.That(console.Options.Autocomplete, Is.Not.Null);
        Assert.That(console.Options.Autocomplete.PathCompletionMode, Is.EqualTo(CommandPathCompletionMode.FullPath));
        Assert.That(console.Options.History, Is.Not.Null);
        Assert.That(console.Options.History.ConsoleHistoryCapacity, Is.EqualTo(200));
        Assert.That(console.Options.History.CommandHistoryCapacity, Is.EqualTo(100));
        Assert.That(console.Options.History.CommandHistoryDuplicatePolicy, Is.EqualTo(CommandHistoryDuplicatePolicy.RejectConsecutive));
        Assert.That(console.Options.Formatting, Is.Not.Null);
        Assert.That(console.Options.Formatting.IsEnabled, Is.False);
        Assert.That(console.Options.Formatting.Model, Is.Null);
        Assert.That(console.Options.Formatting.MarkupProfile, Is.Null);
        Assert.That(console.Options.Formatting.Theme, Is.Null);
        Assert.That(console.Options.Formatting.Formatter, Is.Null);
    }

    [Test]
    public void Constructor_AppliesPartialOptions()
    {
        var theme = new ConsoleTheme();
        var formatter = new PlainTextConsoleFormatter();
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            CommandParsing = new CommandParsingOptions
            {
                OptionValueStyle = OptionValueStyle.AnySeparated,
                FlagAndOptionPrefix = "-",
                IsCaseSensitive = true,
                AllowQuotedStrings = false,
                AllowFlagsAndOptionsInAnyOrder = false,
                BooleanLiterals = new BooleanLiteralOptions
                {
                    TrueLiterals = new[] { "yes", "on" },
                    FalseLiterals = new[] { "no", "off" }
                }
            },
            Autocomplete = new CommandAutocompleteOptions
            {
                PathCompletionMode = CommandPathCompletionMode.DotSegment
            },
            History = new HistoryOptions
            {
                ConsoleHistoryCapacity = 50,
                CommandHistoryCapacity = 25,
                CommandHistoryDuplicatePolicy = CommandHistoryDuplicatePolicy.Allow
            },
            Formatting = ConsoleFormattingOptions.Standard(formatter, theme)
        });

        Assert.That(console.Options.CommandParsing.OptionValueStyle, Is.EqualTo(OptionValueStyle.AnySeparated));
        Assert.That(console.Options.CommandParsing.FlagAndOptionPrefix, Is.EqualTo("-"));
        Assert.That(console.Options.CommandParsing.IsCaseSensitive, Is.True);
        Assert.That(console.Options.CommandParsing.AllowQuotedStrings, Is.False);
        Assert.That(console.Options.CommandParsing.AllowFlagsAndOptionsInAnyOrder, Is.False);
        Assert.That(console.Options.CommandParsing.BooleanLiterals.TrueLiterals, Is.EqualTo(new[] { "yes", "on" }));
        Assert.That(console.Options.CommandParsing.BooleanLiterals.FalseLiterals, Is.EqualTo(new[] { "no", "off" }));
        Assert.That(console.Options.Autocomplete.PathCompletionMode, Is.EqualTo(CommandPathCompletionMode.DotSegment));
        Assert.That(console.Options.History.ConsoleHistoryCapacity, Is.EqualTo(50));
        Assert.That(console.Options.History.CommandHistoryCapacity, Is.EqualTo(25));
        Assert.That(console.Options.History.CommandHistoryDuplicatePolicy, Is.EqualTo(CommandHistoryDuplicatePolicy.Allow));
        Assert.That(console.Options.Formatting.IsEnabled, Is.True);
        Assert.That(console.Options.Formatting.Model, Is.SameAs(ConsoleFormatModel.Standard));
        Assert.That(console.Options.Formatting.MarkupProfile, Is.SameAs(ConsoleMarkupProfile.Standard));
        Assert.That(console.Options.Formatting.Theme, Is.SameAs(theme));
        Assert.That(console.Options.Formatting.Formatter, Is.SameAs(formatter));
        Assert.That(console.Options.Execution.EchoInput, Is.True);
        Assert.That(console.Options.Execution.EchoInputDefaultStyle, Is.Null);
    }

    [Test]
    public void Constructor_SnapshotsOptions()
    {
        var options = new ConsoleManagerOptions
        {
            CommandParsing = new CommandParsingOptions
            {
                OptionValueStyle = OptionValueStyle.EqualSeparated,
                BooleanLiterals = new BooleanLiteralOptions
                {
                    TrueLiterals = new[] { "yes" },
                    FalseLiterals = new[] { "no" }
                }
            },
            History = new HistoryOptions
            {
                ConsoleHistoryCapacity = 20,
                CommandHistoryCapacity = 10
            },
            Autocomplete = new CommandAutocompleteOptions
            {
                PathCompletionMode = CommandPathCompletionMode.DotSegment
            }
        };

        var console = new ConsoleManager(options);

        options.CommandParsing.OptionValueStyle = OptionValueStyle.AnySeparated;
        options.CommandParsing.BooleanLiterals.TrueLiterals[0] = "on";
        options.Autocomplete.PathCompletionMode = CommandPathCompletionMode.FullPath;
        options.History.ConsoleHistoryCapacity = 30;
        console.Options.CommandParsing.BooleanLiterals.FalseLiterals[0] = "off";
        console.Options.History.CommandHistoryCapacity = 40;

        Assert.That(console.Options.CommandParsing.OptionValueStyle, Is.EqualTo(OptionValueStyle.EqualSeparated));
        Assert.That(console.Options.CommandParsing.BooleanLiterals.TrueLiterals, Is.EqualTo(new[] { "yes" }));
        Assert.That(console.Options.CommandParsing.BooleanLiterals.FalseLiterals, Is.EqualTo(new[] { "no" }));
        Assert.That(console.Options.Autocomplete.PathCompletionMode, Is.EqualTo(CommandPathCompletionMode.DotSegment));
        Assert.That(console.Options.History.ConsoleHistoryCapacity, Is.EqualTo(20));
        Assert.That(console.Options.History.CommandHistoryCapacity, Is.EqualTo(10));
    }

    [Test]
    public void Constructor_NullNestedOptionsResolveToDefaults()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            CommandParsing = null!,
            Autocomplete = null!,
            History = null!,
            Formatting = null!,
            Execution = null!
        });

        Assert.That(console.Options.CommandParsing.OptionValueStyle, Is.EqualTo(OptionValueStyle.SpaceSeparated));
        Assert.That(console.Options.CommandParsing.BooleanLiterals.TrueLiterals, Is.EqualTo(new[] { "true" }));
        Assert.That(console.Options.CommandParsing.BooleanLiterals.FalseLiterals, Is.EqualTo(new[] { "false" }));
        Assert.That(console.Options.Autocomplete.PathCompletionMode, Is.EqualTo(CommandPathCompletionMode.FullPath));
        Assert.That(console.Options.History.ConsoleHistoryCapacity, Is.EqualTo(200));
        Assert.That(console.Options.History.CommandHistoryCapacity, Is.EqualTo(100));
        Assert.That(console.Options.Formatting.IsEnabled, Is.False);
        Assert.That(console.Options.Formatting.Theme, Is.Null);
        Assert.That(console.Options.Formatting.Formatter, Is.Null);
        Assert.That(console.Options.Execution.EchoInput, Is.True);
    }

    [Test]
    public void Constructor_NullOptionsThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new ConsoleManager(null!));
    }

    [TestCase(0, 100)]
    [TestCase(-1, 100)]
    [TestCase(200, 0)]
    [TestCase(200, -1)]
    public void Constructor_InvalidCapacitiesThrow(int consoleHistoryCapacity, int commandHistoryCapacity)
    {
        var options = new ConsoleManagerOptions
        {
            History = new HistoryOptions
            {
                ConsoleHistoryCapacity = consoleHistoryCapacity,
                CommandHistoryCapacity = commandHistoryCapacity
            }
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => new ConsoleManager(options));
    }

    [Test]
    public void Constructor_InvalidCommandHistoryDuplicatePolicyThrows()
    {
        var options = new ConsoleManagerOptions
        {
            History = new HistoryOptions
            {
                CommandHistoryDuplicatePolicy = (CommandHistoryDuplicatePolicy)999
            }
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => new ConsoleManager(options));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("- -")]
    public void Constructor_InvalidFlagAndOptionPrefixThrows(string? prefix)
    {
        var options = new ConsoleManagerOptions
        {
            CommandParsing = new CommandParsingOptions
            {
                FlagAndOptionPrefix = prefix!
            }
        };

        Assert.That(() => new ConsoleManager(options), Throws.InstanceOf<Exception>());
    }

    [Test]
    public void Constructor_NullBooleanLiteralsResolveToDefaults()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            CommandParsing = new CommandParsingOptions
            {
                BooleanLiterals = null!
            }
        });

        Assert.That(console.Options.CommandParsing.BooleanLiterals.TrueLiterals, Is.EqualTo(new[] { "true" }));
        Assert.That(console.Options.CommandParsing.BooleanLiterals.FalseLiterals, Is.EqualTo(new[] { "false" }));
    }

    [Test]
    public void Constructor_InvalidBooleanLiteralsThrow()
    {
        var options = new ConsoleManagerOptions
        {
            CommandParsing = new CommandParsingOptions
            {
                BooleanLiterals = new BooleanLiteralOptions
                {
                    TrueLiterals = new[] { "yes" },
                    FalseLiterals = new[] { "YES" }
                }
            }
        };

        Assert.Throws<ArgumentException>(() => new ConsoleManager(options));
    }

    [Test]
    public void Constructor_InvalidPathCompletionModeThrows()
    {
        var options = new ConsoleManagerOptions
        {
            Autocomplete = new CommandAutocompleteOptions
            {
                PathCompletionMode = (CommandPathCompletionMode)999
            }
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => new ConsoleManager(options));
    }

    [Test]
    public void Constructor_AppliesHistoryOptionsToHistories()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            History = new HistoryOptions
            {
                ConsoleHistoryCapacity = 2,
                CommandHistoryCapacity = 3
            }
        });

        Assert.That(console.History.Capacity, Is.EqualTo(2));
        Assert.That(console.CommandHistory.Capacity, Is.EqualTo(3));
    }

    [Test]
    public void Constructor_AppliesExecutionOptions()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            Execution = new CommandExecutionOptions
            {
                EchoInput = false,
                EchoInputDefaultStyle = "CommandInput"
            }
        });

        Assert.That(console.Options.Execution.EchoInput, Is.False);
        Assert.That(console.Options.Execution.EchoInputDefaultStyle, Is.EqualTo("CommandInput"));
    }

    [Test]
    public void Constructor_FormattingWithoutFormatterResolvesToDisabled()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            Formatting = new ConsoleFormattingOptions
            {
                Theme = new ConsoleTheme()
            }
        });

        Assert.That(console.Options.Formatting.IsEnabled, Is.False);
        Assert.That(console.Options.Formatting.Theme, Is.Null);
    }

    [Test]
    public void Constructor_UnityFormattingPresetAppliesStandardModelProfileThemeAndFormatter()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            Formatting = ConsoleFormattingOptions.UnityRichText()
        });

        Assert.That(console.Options.Formatting.IsEnabled, Is.True);
        Assert.That(console.Options.Formatting.Model, Is.SameAs(ConsoleFormatModel.Standard));
        Assert.That(console.Options.Formatting.MarkupProfile, Is.SameAs(ConsoleMarkupProfile.Standard));
        Assert.That(console.Options.Formatting.Theme, Is.Not.Null);
        Assert.That(console.Options.Formatting.Formatter, Is.TypeOf<UnityRichTextConsoleFormatter>());
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_InvalidEchoInputStyleThrows(string style)
    {
        Assert.Throws<ArgumentException>(() => new ConsoleManager(new ConsoleManagerOptions
        {
            Execution = new CommandExecutionOptions
            {
                EchoInputDefaultStyle = style
            }
        }));
    }
}
