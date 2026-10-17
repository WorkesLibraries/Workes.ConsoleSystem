using System;
using System.Collections.Generic;
using System.Reflection;
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;

namespace Workes.ConsoleSystem.Tests.Core;

public sealed class ConsoleManagerTests
{
    [Test]
    public void Constructor_ExposesDefaultResolvedOptions()
    {
        var console = new ConsoleManager();
        ConsoleManagerOptions options = console.Options;

        Assert.That(options.CommandParsing.OptionValueStyle, Is.EqualTo(OptionValueStyle.SpaceSeparated));
        Assert.That(options.CommandParsing.FlagAndOptionPrefix, Is.EqualTo("--"));
        Assert.That(options.CommandParsing.IsCaseSensitive, Is.False);
        Assert.That(options.CommandParsing.AllowQuotedStrings, Is.True);
        Assert.That(options.CommandParsing.AllowFlagsAndOptionsInAnyOrder, Is.True);
        Assert.That(options.CommandParsing.BooleanLiterals.TrueLiterals, Is.EqualTo(new[] { "true" }));
        Assert.That(options.CommandParsing.BooleanLiterals.FalseLiterals, Is.EqualTo(new[] { "false" }));
        Assert.That(options.Autocomplete.PathCompletionMode, Is.EqualTo(CommandPathCompletionMode.FullPath));
        Assert.That(options.History.ConsoleHistoryCapacity, Is.EqualTo(200));
        Assert.That(options.History.CommandHistoryCapacity, Is.EqualTo(100));
        Assert.That(options.History.CommandHistoryDuplicatePolicy, Is.EqualTo(CommandHistoryDuplicatePolicy.RejectConsecutive));
        Assert.That(options.Execution.EchoInput, Is.True);
    }

    [Test]
    public void Constructor_SnapshotsNestedOptionsAndLiteralArrays()
    {
        var options = new ConsoleManagerOptions
        {
            CommandParsing = new CommandParsingOptions
            {
                OptionValueStyle = OptionValueStyle.EqualSeparated,
                FlagAndOptionPrefix = "-",
                IsCaseSensitive = true,
                AllowQuotedStrings = false,
                AllowFlagsAndOptionsInAnyOrder = false,
                BooleanLiterals = new BooleanLiteralOptions
                {
                    TrueLiterals = new[] { "yes" },
                    FalseLiterals = new[] { "no" }
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
            Execution = new CommandExecutionOptions
            {
                EchoInput = false
            }
        };

        var console = new ConsoleManager(options);
        options.CommandParsing.BooleanLiterals.TrueLiterals[0] = "on";
        options.CommandParsing.BooleanLiterals.FalseLiterals[0] = "off";
        options.History.ConsoleHistoryCapacity = 500;
        options.Execution.EchoInput = true;

        ConsoleManagerOptions exposed = console.Options;
        exposed.CommandParsing.BooleanLiterals.TrueLiterals[0] = "mutated";
        exposed.History.CommandHistoryCapacity = 500;

        ConsoleManagerOptions resolved = console.Options;
        Assert.That(resolved.CommandParsing.OptionValueStyle, Is.EqualTo(OptionValueStyle.EqualSeparated));
        Assert.That(resolved.CommandParsing.FlagAndOptionPrefix, Is.EqualTo("-"));
        Assert.That(resolved.CommandParsing.IsCaseSensitive, Is.True);
        Assert.That(resolved.CommandParsing.AllowQuotedStrings, Is.False);
        Assert.That(resolved.CommandParsing.AllowFlagsAndOptionsInAnyOrder, Is.False);
        Assert.That(resolved.CommandParsing.BooleanLiterals.TrueLiterals, Is.EqualTo(new[] { "yes" }));
        Assert.That(resolved.CommandParsing.BooleanLiterals.FalseLiterals, Is.EqualTo(new[] { "no" }));
        Assert.That(resolved.Autocomplete.PathCompletionMode, Is.EqualTo(CommandPathCompletionMode.DotSegment));
        Assert.That(resolved.History.ConsoleHistoryCapacity, Is.EqualTo(50));
        Assert.That(resolved.History.CommandHistoryCapacity, Is.EqualTo(25));
        Assert.That(resolved.History.CommandHistoryDuplicatePolicy, Is.EqualTo(CommandHistoryDuplicatePolicy.Allow));
        Assert.That(resolved.Execution.EchoInput, Is.False);
    }

    [Test]
    public void Constructor_NullNestedOptionsResolveToDefaults()
    {
        var console = new ConsoleManager(new ConsoleManagerOptions
        {
            CommandParsing = null!,
            Autocomplete = null!,
            History = null!,
            Execution = null!
        });

        Assert.That(console.Options.CommandParsing.OptionValueStyle, Is.EqualTo(OptionValueStyle.SpaceSeparated));
        Assert.That(console.Options.Autocomplete.PathCompletionMode, Is.EqualTo(CommandPathCompletionMode.FullPath));
        Assert.That(console.Options.History.ConsoleHistoryCapacity, Is.EqualTo(200));
        Assert.That(console.Options.History.CommandHistoryCapacity, Is.EqualTo(100));
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
    public void Constructor_InvalidHistoryCapacitiesThrow(int consoleCapacity, int commandCapacity)
    {
        var options = new ConsoleManagerOptions
        {
            History = new HistoryOptions
            {
                ConsoleHistoryCapacity = consoleCapacity,
                CommandHistoryCapacity = commandCapacity
            }
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => new ConsoleManager(options));
    }

    [Test]
    public void Constructor_InvalidAutocompleteModeThrows()
    {
        var options = new ConsoleManagerOptions
        {
            Autocomplete = new CommandAutocompleteOptions
            {
                PathCompletionMode = (CommandPathCompletionMode)int.MaxValue
            }
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => new ConsoleManager(options));
    }

    [Test]
    public void Constructor_InvalidHistoryDuplicatePolicyThrows()
    {
        var options = new ConsoleManagerOptions
        {
            History = new HistoryOptions
            {
                CommandHistoryDuplicatePolicy = (CommandHistoryDuplicatePolicy)int.MaxValue
            }
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => new ConsoleManager(options));
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("-- ")]
    public void Constructor_InvalidFlagAndOptionPrefixThrows(string prefix)
    {
        var options = new ConsoleManagerOptions
        {
            CommandParsing = new CommandParsingOptions
            {
                FlagAndOptionPrefix = prefix
            }
        };

        Assert.Throws<ArgumentException>(() => new ConsoleManager(options));
    }

    [Test]
    public void Constructor_OverlappingBooleanLiteralsThrow()
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
    public void Constructor_DuplicateBooleanLiteralsThrow()
    {
        var options = new ConsoleManagerOptions
        {
            CommandParsing = new CommandParsingOptions
            {
                BooleanLiterals = new BooleanLiteralOptions
                {
                    TrueLiterals = new[] { "yes", "YES" },
                    FalseLiterals = new[] { "no" }
                }
            }
        };

        Assert.Throws<ArgumentException>(() => new ConsoleManager(options));
    }

    [TestCase(new string[0], new[] { "false" })]
    [TestCase(new[] { "true" }, new string[0])]
    [TestCase(new[] { " " }, new[] { "false" })]
    public void Constructor_InvalidBooleanLiteralListsThrow(string[] trueLiterals, string[] falseLiterals)
    {
        var options = new ConsoleManagerOptions
        {
            CommandParsing = new CommandParsingOptions
            {
                BooleanLiterals = new BooleanLiteralOptions
                {
                    TrueLiterals = trueLiterals,
                    FalseLiterals = falseLiterals
                }
            }
        };

        Assert.Throws<ArgumentException>(() => new ConsoleManager(options));
    }

    [Test]
    public void Constructor_NullBooleanLiteralListThrows()
    {
        var options = new ConsoleManagerOptions
        {
            CommandParsing = new CommandParsingOptions
            {
                BooleanLiterals = new BooleanLiteralOptions
                {
                    TrueLiterals = null!
                }
            }
        };

        Assert.Throws<ArgumentNullException>(() => new ConsoleManager(options));
    }

    [Test]
    public void Constructor_NullFlagAndOptionPrefixThrows()
    {
        var options = new ConsoleManagerOptions
        {
            CommandParsing = new CommandParsingOptions
            {
                FlagAndOptionPrefix = null!
            }
        };

        Assert.Throws<ArgumentNullException>(() => new ConsoleManager(options));
    }

    [Test]
    public void PublicApi_DoesNotExposeContentSystemTypes()
    {
        var publicTypes = new List<Type> { typeof(ConsoleManager) };
        foreach (ConstructorInfo constructor in typeof(ConsoleManager).GetConstructors())
        {
            foreach (ParameterInfo parameter in constructor.GetParameters())
            {
                publicTypes.Add(parameter.ParameterType);
            }
        }

        foreach (PropertyInfo property in typeof(ConsoleManager).GetProperties())
        {
            publicTypes.Add(property.PropertyType);
        }

        foreach (Type type in publicTypes)
        {
            Assert.That(ContainsContentSystemType(type), Is.False, $"Public API exposes {type.FullName}.");
        }
    }

    private static bool ContainsContentSystemType(Type type)
    {
        if (type.HasElementType && type.GetElementType() is Type elementType && ContainsContentSystemType(elementType))
        {
            return true;
        }

        if (type.Namespace?.StartsWith("Workes.ContentSystem", StringComparison.Ordinal) == true
            || string.Equals(type.Assembly.GetName().Name, "Workes.ContentSystem", StringComparison.Ordinal))
        {
            return true;
        }

        if (type.IsGenericType)
        {
            foreach (Type argument in type.GetGenericArguments())
            {
                if (ContainsContentSystemType(argument))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
