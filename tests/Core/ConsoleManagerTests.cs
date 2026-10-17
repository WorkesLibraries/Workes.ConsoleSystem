using System;
using System.Reflection;
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;
using Workes.ConsoleSystem.Entries;
using Workes.ContentSystem.Core;

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
        Assert.That(options.CommandParsing.AllowInterleavedOptions, Is.False);
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
                AllowInterleavedOptions = true,
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
        Assert.That(resolved.CommandParsing.AllowInterleavedOptions, Is.True);
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
    public void RecordEntry_StoresConsoleEntryThroughInternalAdapter()
    {
        var entry = new TestConsoleEntry(DateTimeOffset.UnixEpoch);
        var console = new ConsoleManager();

        console.RecordEntry(entry);
        entry.Timestamp = DateTimeOffset.UnixEpoch.AddSeconds(1);

        PropertyInfo sequenceProperty = typeof(ConsoleManager).GetProperty(
            "ContentSequence",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var sequence = (ContentSequenceManager)sequenceProperty.GetValue(console)!;
        Assert.That(sequence.Records, Has.Count.EqualTo(1));

        object storedAdapter = sequence.Records[0].Entry;
        PropertyInfo payloadProperty = storedAdapter.GetType().GetProperty(
            "ConsoleEntry",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        Assert.That(payloadProperty.GetValue(storedAdapter), Is.SameAs(entry));
        Assert.That(sequence.Records[0].Timestamp, Is.EqualTo(DateTimeOffset.UnixEpoch));
    }

    [Test]
    public void RecordEntry_NullEntryThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new ConsoleManager().RecordEntry(null!));
    }

    [Test]
    public void PublicApi_DoesNotExposeContentSystemTypes()
    {
        foreach (Type exportedType in typeof(ConsoleManager).Assembly.GetExportedTypes())
        {
            Assert.That(ContainsContentSystemType(exportedType), Is.False, $"Public API exposes {exportedType.FullName}.");
            Assert.That(ContainsContentSystemType(exportedType.BaseType ?? typeof(object)), Is.False,
                $"{exportedType.FullName} inherits from a ContentSystem type.");

            foreach (Type interfaceType in exportedType.GetInterfaces())
            {
                Assert.That(ContainsContentSystemType(interfaceType), Is.False,
                    $"{exportedType.FullName} implements a ContentSystem interface.");
            }

            foreach (ConstructorInfo constructor in exportedType.GetConstructors())
            {
                foreach (ParameterInfo parameter in constructor.GetParameters())
                {
                    Assert.That(ContainsContentSystemType(parameter.ParameterType), Is.False,
                        $"{exportedType.FullName} constructor exposes {parameter.ParameterType.FullName}.");
                }
            }

            foreach (MethodInfo method in exportedType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                Assert.That(ContainsContentSystemType(method.ReturnType), Is.False,
                    $"{exportedType.FullName}.{method.Name} returns {method.ReturnType.FullName}.");
                foreach (ParameterInfo parameter in method.GetParameters())
                {
                    Assert.That(ContainsContentSystemType(parameter.ParameterType), Is.False,
                        $"{exportedType.FullName}.{method.Name} accepts {parameter.ParameterType.FullName}.");
                }
            }

            foreach (PropertyInfo property in exportedType.GetProperties())
            {
                Assert.That(ContainsContentSystemType(property.PropertyType), Is.False,
                    $"{exportedType.FullName}.{property.Name} exposes {property.PropertyType.FullName}.");
            }

            foreach (FieldInfo field in exportedType.GetFields())
            {
                Assert.That(ContainsContentSystemType(field.FieldType), Is.False,
                    $"{exportedType.FullName}.{field.Name} exposes {field.FieldType.FullName}.");
            }

            foreach (EventInfo eventInfo in exportedType.GetEvents())
            {
                if (eventInfo.EventHandlerType is not null)
                {
                    Assert.That(ContainsContentSystemType(eventInfo.EventHandlerType), Is.False,
                        $"{exportedType.FullName}.{eventInfo.Name} exposes {eventInfo.EventHandlerType.FullName}.");
                }
            }
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

    private sealed class TestConsoleEntry : IConsoleEntry
    {
        public TestConsoleEntry(DateTimeOffset timestamp)
        {
            Timestamp = timestamp;
        }

        public DateTimeOffset Timestamp { get; set; }
    }
}
