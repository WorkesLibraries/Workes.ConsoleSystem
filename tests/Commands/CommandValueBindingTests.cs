using Workes.ConsoleSystem.Commands;
using Workes.ConsoleSystem.Configuration;
using Workes.ConsoleSystem.Core;

namespace Workes.ConsoleSystem.Tests.Commands;

public sealed class CommandValueBindingTests
{
    [Test]
    public void ParseCommand_ResolvesExplicitValuesDefaultsAndTypeDefaults_AndKeepsSourceViewsExplicit()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("values")
            .Argument<ValueState>(x => x.Name, "name", "player")
            .Argument<ValueState>(x => x.Count, "count", "count").Optional()
            .Option<ValueState>(x => x.Count, "count", "amount")
            .Flag<ValueState>(x => x.Enabled, "enabled", "enabled")
            .Defaults(("count", 9))
            .Execute<ValueState>((_, _) => CommandResult.Success())
            .Build());

        BoundCommand parsed = console.ParseCommand("values Alex").Command!;
        BoundCommand explicitOption = console.ParseCommand("values Alex --amount 4").Command!;

        Assert.That(parsed.Values["name"], Is.EqualTo("Alex"));
        Assert.That(parsed.Values["count"], Is.EqualTo(9));
        Assert.That(parsed.Values["enabled"], Is.False);
        Assert.That(parsed.Arguments.Keys, Is.EquivalentTo(new[] { "player" }));
        Assert.That(parsed.Options, Is.Empty);
        Assert.That(parsed.Flags, Is.Empty);
        Assert.That(parsed.GetState<ValueState>()!.Count, Is.EqualTo(9));
        Assert.That(explicitOption.Values["count"], Is.EqualTo(4));
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, object?>)parsed.Values)["count"] = 0);
    }

    [Test]
    public void ParseCommand_UsesNullZeroAndFalseTypeDefaultsWhenNothingIsSupplied()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("empty-values")
            .Argument<ValueState>(x => x.Name, "name", "player").Optional()
            .Option<ValueState>(x => x.Count, "count", "amount")
            .Flag<ValueState>(x => x.Enabled, "enabled", "enabled")
            .Execute<ValueState>((_, _) => CommandResult.Success()).Build());

        BoundCommand parsed = console.ParseCommand("empty-values").Command!;

        Assert.That(parsed.Values["name"], Is.Null);
        Assert.That(parsed.Values["count"], Is.EqualTo(0));
        Assert.That(parsed.Values["enabled"], Is.EqualTo(false));
        Assert.That(parsed.GetState<ValueState>()!.Name, Is.Null);
    }

    [Test]
    public void ParseCommand_OptionalArgumentAndOptionShareOneValue_AndRejectBothSources()
    {
        var console = NewConsole(allowInterleaved: true);
        console.RegisterCommand(SharedValueCommand());

        BoundCommand option = console.ParseCommand("shared Alex --amount-option 4").Command!;
        BoundCommand argument = console.ParseCommand("shared Alex 5").Command!;
        CommandParseResult conflict = console.ParseCommand("shared Alex 5 --amount-option 4");

        Assert.That(option.Values["amount"], Is.EqualTo(4));
        Assert.That(option.Arguments.ContainsKey("amount"), Is.False);
        Assert.That(option.Options["amount-option"], Is.EqualTo(4));
        Assert.That(argument.Values["amount"], Is.EqualTo(5));
        Assert.That(argument.Arguments["amount"], Is.EqualTo(5));
        Assert.That(argument.Options, Is.Empty);
        Assert.That(conflict.IsSuccess, Is.False);
        Assert.That(conflict.Failure!.Code, Is.EqualTo(ConsoleFailureCodes.CommandMemberDuplicate));
    }

    [Test]
    public void ParseCommand_OmittedOptionalArgumentDoesNotBlockNamedMembersWhenInterleavingIsOff()
    {
        var console = NewConsole(allowInterleaved: false);
        console.RegisterCommand(SharedValueCommand());

        CommandParseResult parsed = console.ParseCommand("shared Alex --amount-option 6");

        Assert.That(parsed.IsSuccess, Is.True, parsed.Failure?.Message);
        Assert.That(parsed.Command!.GetState<ValueState>()!.Count, Is.EqualTo(6));
    }

    [Test]
    public void Build_RequiresOptionalArgumentToBeUniqueAndLast()
    {
        Assert.Throws<InvalidOperationException>(() => new CommandBuilder("bad.order")
            .Argument<TwoArgsState>(x => x.First, "first", "first").Optional()
            .Argument<TwoArgsState>(x => x.Second, "second", "second")
            .Execute<TwoArgsState>((_, _) => CommandResult.Success()).Build());

        Assert.Throws<InvalidOperationException>(() => new CommandBuilder("bad.count")
            .Argument<TwoArgsState>(x => x.First, "first", "first").Optional()
            .Argument<TwoArgsState>(x => x.Second, "second", "second").Optional()
            .Execute<TwoArgsState>((_, _) => CommandResult.Success()).Build());
    }

    [Test]
    public void Build_RejectsInvalidSharedIds()
    {
        Assert.Throws<InvalidOperationException>(() => new CommandBuilder("bad.required")
            .Argument<TwoArgsState>(x => x.First, "same", "first")
            .Option<TwoArgsState>(x => x.First, "same", "value")
            .Execute<TwoArgsState>((_, _) => CommandResult.Success()).Build());

        Assert.Throws<InvalidOperationException>(() => new CommandBuilder("bad.property")
            .Argument<TwoArgsState>(x => x.First, "same", "value").Optional()
            .Option<TwoArgsState>(x => x.Second, "same", "value-option")
            .Execute<TwoArgsState>((_, _) => CommandResult.Success()).Build());

        Assert.Throws<InvalidOperationException>(() => new CommandBuilder("bad.flag")
            .Argument<ValueState>(x => x.Count, "same", "value").Optional()
            .Flag<ValueState>(x => x.Enabled, "same", "same")
            .Execute<ValueState>((_, _) => CommandResult.Success()).Build());

        Assert.Throws<InvalidOperationException>(() => new CommandBuilder("bad.options")
            .Option<TwoArgsState>(x => x.First, "same", "first")
            .Option<TwoArgsState>(x => x.Second, "same", "second")
            .Execute<TwoArgsState>((_, _) => CommandResult.Success()).Build());
    }

    [Test]
    public void Build_ValidatesDefaultsByIdAndType()
    {
        Assert.Throws<InvalidOperationException>(() => new CommandBuilder("default.unknown")
            .Defaults(("missing", 1)).Execute(ctx => CommandResult.Success()).Build());
        Assert.Throws<InvalidOperationException>(() => new CommandBuilder("default.duplicate")
            .Argument<ValueState>(x => x.Name, "name", "player")
            .Defaults(("name", "A"), ("name", "B"))
            .Execute<ValueState>((_, _) => CommandResult.Success()).Build());
        Assert.Throws<InvalidOperationException>(() => new CommandBuilder("default.required")
            .Argument<ValueState>(x => x.Name, "name", "player")
            .Defaults(("name", "A"))
            .Execute<ValueState>((_, _) => CommandResult.Success()).Build());
        Assert.Throws<InvalidOperationException>(() => new CommandBuilder("default.flag")
            .Flag<ValueState>(x => x.Enabled, "enabled", "enabled")
            .Defaults(("enabled", true))
            .Execute<ValueState>((_, _) => CommandResult.Success()).Build());
        Assert.Throws<InvalidOperationException>(() => new CommandBuilder("default.type")
            .Option<ValueState>(x => x.Count, "count", "amount")
            .Defaults(("count", "many"))
            .Execute<ValueState>((_, _) => CommandResult.Success()).Build());
    }

    [Test]
    public void Build_AllowsOrdinalDistinctIds()
    {
        CommandDefinition command = new CommandBuilder("ordinal")
            .Option<TwoArgsState>(x => x.First, "value", "first")
            .Option<TwoArgsState>(x => x.Second, "Value", "second")
            .Execute<TwoArgsState>((_, _) => CommandResult.Success()).Build();

        Assert.That(command.Options[0].Id, Is.EqualTo("value"));
        Assert.That(command.Options[1].Id, Is.EqualTo("Value"));
    }

    [Test]
    public void ValidateCommand_AppliesBindingRulesToExplicitArgumentValues()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("bounded")
            .Argument<CountState>(x => x.Count, "count", "count")
            .Range(1, 3)
            .Execute<CountState>((_, _) => CommandResult.Success()).Build());

        CommandValidationResult valid = console.ValidateCommand(console.ParseCommand("bounded 2").Command!);
        CommandValidationResult invalid = console.ValidateCommand(console.ParseCommand("bounded 4").Command!);

        Assert.That(valid.IsSuccess, Is.True);
        Assert.That(invalid.IsSuccess, Is.False);
        Assert.That(invalid.Failure!.Source, Is.EqualTo("count"));
    }

    [Test]
    public void ValidateCommand_UsesRulesFromTheInputBindingThatSuppliedASharedId()
    {
        var console = new ConsoleManager();
        console.RegisterCommand(new CommandBuilder("shared-rule")
            .Argument<CountState>(x => x.Count, "count", "count")
                .Optional()
                .AllowedValues(1, 2)
            .Option<CountState>(x => x.Count, "count", "count-option")
                .AllowedValues(3)
            .Execute<CountState>((_, _) => CommandResult.Success()).Build());

        CommandValidationResult positional = console.ValidateCommand(console.ParseCommand("shared-rule 3").Command!);
        CommandValidationResult option = console.ValidateCommand(console.ParseCommand("shared-rule --count-option 3").Command!);

        Assert.That(positional.IsSuccess, Is.False);
        Assert.That(option.IsSuccess, Is.True);
    }

    [Test]
    public void Autocomplete_UsesArgumentCandidatesAndSuppressesSharedOptionAfterArgumentInput()
    {
        var console = NewConsole(allowInterleaved: true);
        console.RegisterCommand(new CommandBuilder("suggest")
            .Argument<ValueState>(x => x.Name, "name", "player")
            .Argument<ValueState>(x => x.Count, "amount", "amount")
                .Optional()
                .AllowedValues(1, 2)
                .SuggestAllowedValues()
            .Option<ValueState>(x => x.Count, "amount", "amount-option")
            .Execute<ValueState>((_, _) => CommandResult.Success()).Build());

        CommandAutocompleteResult values = console.GetAutocomplete("suggest Alex ", "suggest Alex ".Length);
        CommandAutocompleteResult members = console.GetAutocomplete("suggest Alex 1 --", "suggest Alex 1 --".Length);

        Assert.That(values.Candidates.Select(x => x.Text), Is.EquivalentTo(new[] { "1", "2" }));
        Assert.That(members.Candidates.Any(x => x.Text == "--amount-option"), Is.False);
    }

    private static ConsoleManager NewConsole(bool allowInterleaved) => new(new ConsoleManagerOptions
    {
        CommandParsing = new CommandParsingOptions { AllowInterleavedOptions = allowInterleaved }
    });

    private static CommandDefinition SharedValueCommand() => new CommandBuilder("shared")
        .Argument<ValueState>(x => x.Name, "name", "player")
        .Argument<ValueState>(x => x.Count, "amount", "amount").Optional()
        .Option<ValueState>(x => x.Count, "amount", "amount-option")
        .Flag<ValueState>(x => x.Enabled, "enabled", "enabled")
        .Execute<ValueState>((_, _) => CommandResult.Success()).Build();

    private sealed class ValueState
    {
        public ValueState(string? name, int count, bool enabled) { Name = name; Count = count; Enabled = enabled; }
        public string? Name { get; }
        public int Count { get; }
        public bool Enabled { get; }
    }

    private sealed class TwoArgsState
    {
        public TwoArgsState(string? first, string? second) { First = first; Second = second; }
        public string? First { get; }
        public string? Second { get; }
    }

    private sealed class CountState
    {
        public CountState(int count) => Count = count;
        public int Count { get; }
    }
}
