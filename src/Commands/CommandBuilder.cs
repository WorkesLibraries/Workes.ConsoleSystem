using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Workes.ConsoleSystem.Permissions;
using Workes.ConsoleSystem.Presentation;

namespace Workes.ConsoleSystem.Commands;

/// <summary>
/// Builds immutable command definitions.
/// </summary>
public sealed class CommandBuilder
{
    private readonly string _path;
    private readonly List<ArgumentDraft> _arguments = new List<ArgumentDraft>();
    private readonly List<FlagDraft> _flags = new List<FlagDraft>();
    private readonly List<OptionDraft> _options = new List<OptionDraft>();
    private readonly List<(string Id, object? Value)> _defaults = new List<(string Id, object? Value)>();
    private readonly List<CommandConstraintDefinition> _constraints = new List<CommandConstraintDefinition>();
    private readonly List<string> _requiredPermissions = new List<string>();
    private readonly List<IReadOnlyList<string>> _requiredAnyPermissionGroups = new List<IReadOnlyList<string>>();
    private readonly List<Func<CommandAvailabilityContext, CommandAvailabilityResult>> _availabilityChecks = new List<Func<CommandAvailabilityContext, CommandAvailabilityResult>>();
    private readonly List<CommandSuccessOutputDefinition> _successOutputs = new List<CommandSuccessOutputDefinition>();
    private string _description = string.Empty;
    private readonly List<string> _notes = new List<string>();
    private readonly List<string> _examples = new List<string>();
    private string? _syntax;
    private CommandSyntaxFormatter? _syntaxFormatter;
    private Type? _stateType;
    private Delegate? _handler;
    private ValueMemberDraft? _lastValueMember;
    private ArgumentDraft? _lastArgument;
    private bool? _echoInput;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandBuilder"/> class.
    /// </summary>
    /// <param name="path">The command path.</param>
    public CommandBuilder(string path)
    {
        _path = path;
    }

    /// <summary>
    /// Sets the optional command description.
    /// </summary>
    /// <param name="description">The command description.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder Description(string description)
    {
        _description = description ?? throw new ArgumentNullException(nameof(description));
        _lastValueMember = null;
        return this;
    }

    /// <summary>Sets optional notes displayed by detailed help formatters.</summary>
    public CommandBuilder Notes(IReadOnlyList<string>? notes)
    {
        _notes.Clear();
        if (notes is not null) _notes.AddRange(ValidateTextList(notes, nameof(notes)));
        _lastValueMember = null;
        return this;
    }

    /// <summary>Sets complete command invocation examples.</summary>
    public CommandBuilder Examples(IReadOnlyList<string>? examples)
    {
        _examples.Clear();
        if (examples is not null) _examples.AddRange(ValidateTextList(examples, nameof(examples)));
        _lastValueMember = null;
        return this;
    }

    /// <summary>Sets manually authored command syntax. This is exclusive with generated syntax.</summary>
    public CommandBuilder Syntax(string syntax)
    {
        if (_syntaxFormatter is not null) throw new InvalidOperationException("Manual and generated syntax are mutually exclusive.");
        _syntax = syntax ?? throw new ArgumentNullException(nameof(syntax));
        _lastValueMember = null;
        return this;
    }

    /// <summary>Sets generated command syntax using a custom or built-in formatter.</summary>
    public CommandBuilder GenerateSyntaxWith(CommandSyntaxFormatter? formatter = null)
    {
        if (_syntax is not null) throw new InvalidOperationException("Manual and generated syntax are mutually exclusive.");
        _syntaxFormatter = formatter ?? CommandSyntaxFormatters.Canonical;
        _lastValueMember = null;
        return this;
    }

    /// <summary>
    /// Adds a positional argument. Arguments are required unless marked optional.
    /// </summary>
    /// <typeparam name="TState">The command state type.</typeparam>
    /// <param name="propertySelector">The state property selector.</param>
    /// <param name="id">The logical state value ID.</param>
    /// <param name="name">The argument name.</param>
    /// <param name="description">Optional help description.</param>
    /// <param name="examples">Optional sample values.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder Argument<TState>(Expression<Func<TState, object?>> propertySelector, string id, string name, string description = "", IReadOnlyList<string>? examples = null)
    {
        PropertyInfo property = GetProperty(propertySelector);
        UseStateType(typeof(TState));
        var argument = new ArgumentDraft(property, ValidateName(id, nameof(id)), ValidateName(name, nameof(name)), description ?? throw new ArgumentNullException(nameof(description)), examples is null ? new List<string>() : ValidateTextList(examples, nameof(examples)));
        _arguments.Add(argument);
        _lastValueMember = argument;
        _lastArgument = argument;
        return this;
    }

    /// <summary>
    /// Adds a boolean flag.
    /// </summary>
    /// <typeparam name="TState">The command state type.</typeparam>
    /// <param name="propertySelector">The state property selector.</param>
    /// <param name="id">The logical state value ID.</param>
    /// <param name="name">The canonical flag name.</param>
    /// <param name="aliases">The flag aliases.</param>
    /// <param name="description">Optional help description.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder Flag<TState>(Expression<Func<TState, object?>> propertySelector, string id, string name, IReadOnlyList<string>? aliases = null, string description = "")
    {
        PropertyInfo property = GetProperty(propertySelector);
        UseStateType(typeof(TState));
        _flags.Add(new FlagDraft(property, ValidateName(id, nameof(id)), ValidateName(name, nameof(name)), ValidateAliases(aliases), description ?? throw new ArgumentNullException(nameof(description))));
        _lastValueMember = null;
        _lastArgument = null;
        return this;
    }

    /// <summary>
    /// Adds a named option.
    /// </summary>
    /// <typeparam name="TState">The command state type.</typeparam>
    /// <param name="propertySelector">The state property selector.</param>
    /// <param name="id">The logical state value ID.</param>
    /// <param name="name">The canonical option name.</param>
    /// <param name="aliases">The option aliases.</param>
    /// <param name="description">Optional help description.</param>
    /// <param name="examples">Optional sample values.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder Option<TState>(Expression<Func<TState, object?>> propertySelector, string id, string name, IReadOnlyList<string>? aliases = null, string description = "", IReadOnlyList<string>? examples = null)
    {
        PropertyInfo property = GetProperty(propertySelector);
        UseStateType(typeof(TState));
        var option = new OptionDraft(property, ValidateName(id, nameof(id)), ValidateName(name, nameof(name)), ValidateAliases(aliases), description ?? throw new ArgumentNullException(nameof(description)), examples is null ? new List<string>() : ValidateTextList(examples, nameof(examples)));
        _options.Add(option);
        _lastValueMember = option;
        _lastArgument = null;
        return this;
    }

    /// <summary>Marks the most recently declared positional argument as optional.</summary>
    public CommandBuilder Optional()
    {
        ArgumentDraft argument = _lastArgument is not null && ReferenceEquals(_lastValueMember, _lastArgument)
            ? _lastArgument
            : throw new InvalidOperationException("Optional() must follow a positional argument definition.");
        argument.IsOptional = true;
        _lastArgument = null;
        return this;
    }

    /// <summary>
    /// Sets defaults for logical command values by ID.
    /// </summary>
    /// <param name="defaults">The logical value IDs and their default values.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder Defaults(params (string Id, object? Value)[] defaults)
    {
        if (defaults is null) throw new ArgumentNullException(nameof(defaults));
        foreach ((string id, object? value) in defaults)
            _defaults.Add((ValidateName(id, nameof(defaults)), value));
        _lastArgument = null;
        return this;
    }

    /// <summary>
    /// Sets range metadata on the most recently added positional argument or option.
    /// </summary>
    /// <param name="minimum">The inclusive minimum value metadata.</param>
    /// <param name="maximum">The inclusive maximum value metadata.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder Range(object? minimum, object? maximum)
    {
        ValueMemberDraft member = RequireLastValueMember();
        member.RangeMinimum = minimum;
        member.RangeMaximum = maximum;
        return this;
    }

    /// <summary>
    /// Sets allowed value metadata on the most recently added positional argument or option.
    /// </summary>
    /// <param name="values">The allowed value metadata.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder AllowedValues(params object?[] values)
    {
        RequireLastValueMember().AllowedValues = values is null
            ? throw new ArgumentNullException(nameof(values))
            : new List<object?>(values);
        return this;
    }

    /// <summary>
    /// Enables autocomplete suggestions from the allowed values of the most recently added positional argument or option.
    /// </summary>
    /// <returns>The current builder.</returns>
    public CommandBuilder SuggestAllowedValues()
    {
        RequireLastValueMember().SuggestAllowedValues = true;
        return this;
    }

    /// <summary>
    /// Sets value autocomplete candidates on the most recently added positional argument or option.
    /// </summary>
    /// <param name="provider">The value candidate provider.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder ValueCandidates(Func<CommandAutocompleteContext, IEnumerable<string>> provider)
    {
        if (provider is null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        RequireLastValueMember().ValueCandidateProvider = provider;
        return this;
    }

    /// <summary>
    /// Adds typed command-state constraint validation.
    /// </summary>
    /// <typeparam name="TState">The command state type.</typeparam>
    /// <param name="name">The constraint name.</param>
    /// <param name="message">The constraint failure message.</param>
    /// <param name="predicate">The predicate that returns <see langword="true" /> when the command state is valid.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder Constraint<TState>(string name, string message, Func<TState, bool> predicate)
        where TState : class
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        UseStateType(typeof(TState));
        _constraints.Add(new CommandConstraintDefinition(
            ValidateName(name, nameof(name)),
            message ?? throw new ArgumentNullException(nameof(message)),
            state => predicate((TState)state!)));
        _lastValueMember = null;
        return this;
    }

    /// <summary>Requires one permission to execute this command. Multiple calls are combined with AND.</summary>
    /// <param name="permission">The required permission name.</param>
    public CommandBuilder RequirePermission(string permission)
    {
        _requiredPermissions.Add(PermissionPath.ValidateName(permission, nameof(permission)));
        _lastValueMember = null;
        return this;
    }

    /// <summary>Requires at least one permission from the supplied group. Multiple groups are combined with AND.</summary>
    /// <param name="permissions">The alternative permissions.</param>
    public CommandBuilder RequireAnyPermission(params string[] permissions)
    {
        if (permissions is null)
        {
            throw new ArgumentNullException(nameof(permissions));
        }

        if (permissions.Length == 0)
        {
            throw new ArgumentException("At least one permission is required.", nameof(permissions));
        }

        var copy = new List<string>(permissions.Length);
        foreach (string permission in permissions)
        {
            string validated = PermissionPath.ValidateName(permission, nameof(permissions));
            if (!copy.Exists(existing => string.Equals(existing, validated, StringComparison.OrdinalIgnoreCase)))
            {
                copy.Add(validated);
            }
        }

        _requiredAnyPermissionGroups.Add(copy.AsReadOnly());
        _lastValueMember = null;
        return this;
    }

    /// <summary>Adds a runtime availability check. Every configured check must return available.</summary>
    /// <param name="check">The availability check.</param>
    public CommandBuilder AvailableWhen(Func<CommandAvailabilityContext, CommandAvailabilityResult> check)
    {
        _availabilityChecks.Add(check ?? throw new ArgumentNullException(nameof(check)));
        _lastValueMember = null;
        return this;
    }

    /// <summary>
    /// Echoes submitted input for this command using manager defaults.
    /// </summary>
    /// <returns>The current builder.</returns>
    public CommandBuilder EchoInput()
    {
        _echoInput = true;
        _lastValueMember = null;
        return this;
    }

    /// <summary>
    /// Disables echoed input for this command.
    /// </summary>
    /// <returns>The current builder.</returns>
    public CommandBuilder DoNotEchoInput()
    {
        _echoInput = false;
        _lastValueMember = null;
        return this;
    }

    /// <summary>
    /// Adds declared inline success output from literal plain text.
    /// </summary>
    /// <param name="text">The output text.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder SuccessOutputInline(string text)
    {
        _successOutputs.Add(new CommandSuccessOutputDefinition(CommandOutput.Inline(text)));
        _lastValueMember = null;
        return this;
    }

    /// <summary>
    /// Adds declared inline success output from console text.
    /// </summary>
    /// <param name="content">The output content.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder SuccessOutputInline(ConsoleText content)
    {
        _successOutputs.Add(new CommandSuccessOutputDefinition(CommandOutput.Inline(content)));
        _lastValueMember = null;
        return this;
    }

    /// <summary>
    /// Adds declared block success output from literal plain text.
    /// </summary>
    /// <param name="text">The output text.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder SuccessOutputBlock(string text)
    {
        _successOutputs.Add(new CommandSuccessOutputDefinition(CommandOutput.Block(text)));
        _lastValueMember = null;
        return this;
    }

    /// <summary>
    /// Adds declared block success output from console text.
    /// </summary>
    /// <param name="content">The output content.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder SuccessOutputBlock(ConsoleText content)
    {
        _successOutputs.Add(new CommandSuccessOutputDefinition(CommandOutput.Block(content)));
        _lastValueMember = null;
        return this;
    }

    /// <summary>
    /// Stores a simple command handler.
    /// </summary>
    /// <param name="handler">The command handler.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder Execute(Func<CommandContext, CommandResult> handler)
    {
        if (_stateType is not null)
        {
            throw new InvalidOperationException("A typed command must use a typed command handler.");
        }

        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _lastValueMember = null;
        return this;
    }

    /// <summary>
    /// Stores a typed command handler.
    /// </summary>
    /// <typeparam name="TState">The command state type.</typeparam>
    /// <param name="handler">The command handler.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder Execute<TState>(Func<CommandContext, TState, CommandResult> handler)
    {
        UseStateType(typeof(TState));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _lastValueMember = null;
        return this;
    }

    /// <summary>
    /// Builds an immutable command definition.
    /// </summary>
    /// <returns>The built command definition.</returns>
    public CommandDefinition Build()
    {
        CommandPathValidation.Validate(_path);

        if (_handler is null)
        {
            throw new InvalidOperationException("A command must have an execution handler before it can be built.");
        }

        ValidateUniqueMemberNames();
        ValidateValueIds();

        return new CommandDefinition(
            _path,
            _description,
            new List<string>(_notes).AsReadOnly(),
            new List<string>(_examples).AsReadOnly(),
            _syntax,
            _syntaxFormatter,
            _stateType,
            _arguments.ConvertAll(x => new CommandArgumentDefinition(
                x.Property.Name,
                x.Property.PropertyType,
                x.Id,
                x.Name,
                x.Description,
                new List<string>(x.Examples).AsReadOnly(),
                x.ValueCandidateProvider,
                x.IsOptional,
                NormalizeRangeValue(x.RangeMinimum, x.RangeMaximum, x.Property.PropertyType, "minimum", x.Name),
                NormalizeRangeValue(x.RangeMaximum, x.RangeMinimum, x.Property.PropertyType, "maximum", x.Name),
                NormalizeAllowedValues(x.AllowedValues, x.Property.PropertyType, x.Name).AsReadOnly(),
                x.SuggestAllowedValues)).AsReadOnly(),
            _flags.ConvertAll(x => new CommandFlagDefinition(
                x.Property.Name,
                x.Id,
                x.Name,
                new List<string>(x.Aliases).AsReadOnly(),
                x.Description)).AsReadOnly(),
            _options.ConvertAll(x => new CommandOptionDefinition(
                x.Property.Name,
                x.Property.PropertyType,
                x.Id,
                x.Name,
                new List<string>(x.Aliases).AsReadOnly(),
                x.Description,
                new List<string>(x.Examples).AsReadOnly(),
                NormalizeRangeValue(x.RangeMinimum, x.RangeMaximum, x.Property.PropertyType, "minimum", x.Name),
                NormalizeRangeValue(x.RangeMaximum, x.RangeMinimum, x.Property.PropertyType, "maximum", x.Name),
                NormalizeAllowedValues(x.AllowedValues, x.Property.PropertyType, x.Name).AsReadOnly(),
                x.SuggestAllowedValues,
                x.ValueCandidateProvider)).AsReadOnly(),
            BuildDefaults(),
            new List<CommandConstraintDefinition>(_constraints).AsReadOnly(),
            new List<string>(_requiredPermissions).AsReadOnly(),
            new List<IReadOnlyList<string>>(_requiredAnyPermissionGroups).AsReadOnly(),
            new List<Func<CommandAvailabilityContext, CommandAvailabilityResult>>(_availabilityChecks).AsReadOnly(),
            new CommandEchoInputDefinition(_echoInput),
            new List<CommandSuccessOutputDefinition>(_successOutputs).AsReadOnly(),
            _handler);
    }

    private static PropertyInfo GetProperty<TState, TValue>(Expression<Func<TState, TValue>> selector)
    {
        if (selector is null)
        {
            throw new ArgumentNullException(nameof(selector));
        }

        Expression expression = selector.Body;
        if (expression is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
        {
            expression = unary.Operand;
        }

        if (expression is not MemberExpression memberExpression || memberExpression.Member is not PropertyInfo property)
        {
            throw new ArgumentException("Command state bindings must target public instance properties.", nameof(selector));
        }

        MethodInfo? getter = property.GetMethod;
        if (getter is null || !getter.IsPublic || getter.IsStatic)
        {
            throw new ArgumentException("Command state bindings must target public instance properties.", nameof(selector));
        }

        return property;
    }

    private static string ValidateName(string value, string parameterName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Names cannot be blank.", parameterName);
        }

        return value;
    }

    private static List<string> ValidateAliases(IReadOnlyList<string>? aliases)
    {
        var result = new List<string>(aliases?.Count ?? 0);
        if (aliases is null) return result;
        foreach (string alias in aliases)
        {
            result.Add(ValidateName(alias, nameof(aliases)));
        }

        return result;
    }

    private static List<string> ValidateTextList(IReadOnlyList<string> values, string parameterName)
    {
        var copy = new List<string>(values.Count);
        foreach (string value in values) copy.Add(value ?? throw new ArgumentException("Metadata collections cannot contain null.", parameterName));
        return copy;
    }

    private void UseStateType(Type stateType)
    {
        if (!stateType.IsClass)
        {
            throw new InvalidOperationException("Typed command state must be a reference type.");
        }

        if (_stateType is not null && _stateType != stateType)
        {
            throw new InvalidOperationException("A command can only bind one typed state type.");
        }

        _stateType = stateType;
    }

    private ValueMemberDraft RequireLastValueMember()
    {
        return _lastValueMember ?? throw new InvalidOperationException("Value candidate metadata must follow a positional argument or option definition.");
    }

    private IReadOnlyDictionary<string, object?> BuildDefaults()
    {
        var schema = new Dictionary<string, (Type Type, bool RequiredArgument, bool Flag)>(StringComparer.Ordinal);
        foreach (ArgumentDraft argument in _arguments)
        {
            schema[argument.Id] = (argument.Property.PropertyType, !argument.IsOptional, false);
        }

        foreach (OptionDraft option in _options)
        {
            if (!schema.ContainsKey(option.Id)) schema[option.Id] = (option.Property.PropertyType, false, false);
        }

        foreach (FlagDraft flag in _flags)
        {
            schema[flag.Id] = (typeof(bool), false, true);
        }

        var defaults = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach ((string id, object? value) in _defaults)
        {
            if (!schema.TryGetValue(id, out var entry))
            {
                throw new InvalidOperationException($"Default references unknown value ID '{id}'.");
            }

            if (entry.Flag)
            {
                throw new InvalidOperationException($"Flags cannot have defaults (ID '{id}').");
            }

            if (entry.RequiredArgument)
            {
                throw new InvalidOperationException($"Required positional arguments cannot have defaults (ID '{id}').");
            }

            if (defaults.ContainsKey(id))
            {
                throw new InvalidOperationException($"Duplicate default for value ID '{id}'.");
            }

            defaults.Add(id, NormalizeMetadataValue(value, entry.Type, "default value", id, allowNull: AllowsNull(entry.Type)));
        }

        return new System.Collections.ObjectModel.ReadOnlyDictionary<string, object?>(defaults);
    }

    private void ValidateValueIds()
    {
        for (int i = 0; i < _arguments.Count; i++)
        {
            if (_arguments[i].IsOptional && i != _arguments.Count - 1)
            {
                throw new InvalidOperationException("An optional positional argument must be the final positional argument.");
            }
        }

        var groups = new Dictionary<string, List<(PropertyInfo Property, string Kind, bool Optional)>>(StringComparer.Ordinal);
        void Add(string id, PropertyInfo property, string kind, bool optional)
        {
            if (!groups.TryGetValue(id, out List<(PropertyInfo, string, bool)>? group))
            {
                groups.Add(id, group = new List<(PropertyInfo, string, bool)>());
            }

            group.Add((property, kind, optional));
        }

        foreach (ArgumentDraft argument in _arguments)
        {
            Add(argument.Id, argument.Property, "argument", argument.IsOptional);
        }

        foreach (FlagDraft flag in _flags)
        {
            Add(flag.Id, flag.Property, "flag", false);
        }

        foreach (OptionDraft option in _options)
        {
            Add(option.Id, option.Property, "option", false);
        }

        foreach (KeyValuePair<string, List<(PropertyInfo Property, string Kind, bool Optional)>> pair in groups)
        {
            if (pair.Value.Count <= 1)
            {
                continue;
            }

            bool allowedPair = pair.Value.Count == 2 && pair.Value.Exists(x => x.Kind == "argument" && x.Optional) && pair.Value.Exists(x => x.Kind == "option");
            if (!allowedPair
                || pair.Value[0].Property.PropertyType != pair.Value[1].Property.PropertyType
                || !pair.Value[0].Property.Equals(pair.Value[1].Property))
            {
                throw new InvalidOperationException($"Value ID '{pair.Key}' can only be shared by one optional argument and one option targeting the same property and type.");
            }
        }
    }

    private void ValidateUniqueMemberNames()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (ArgumentDraft argument in _arguments)
        {
            bool alternateNamedSource = argument.IsOptional && _options.Exists(option => option.Id == argument.Id);
            if (!alternateNamedSource) AddUnique(seen, argument.Name);
        }

        foreach (FlagDraft flag in _flags)
        {
            AddUnique(seen, flag.Name);
            foreach (string alias in flag.Aliases)
            {
                AddUnique(seen, alias);
            }
        }

        foreach (OptionDraft option in _options)
        {
            AddUnique(seen, option.Name);
            foreach (string alias in option.Aliases)
            {
                AddUnique(seen, alias);
            }
        }
    }

    private static object? NormalizeRangeValue(object? value, object? otherValue, Type targetType, string label, string optionName)
    {
        if (value is null && otherValue is null)
        {
            return null;
        }

        if (value is null || otherValue is null)
        {
            throw new InvalidOperationException($"Binding '{optionName}' range metadata must include both minimum and maximum values.");
        }

        object normalized = NormalizeMetadataValue(value, targetType, $"range {label}", optionName, allowNull: false)!;
        object normalizedOther = NormalizeMetadataValue(otherValue, targetType, $"range {label}", optionName, allowNull: false)!;
        if (CompareMetadataValues(normalized, normalizedOther, optionName) > 0 && label == "minimum")
        {
            throw new InvalidOperationException($"Binding '{optionName}' range minimum cannot be greater than the maximum.");
        }

        return normalized;
    }

    private static List<object?> NormalizeAllowedValues(IReadOnlyList<object?> values, Type targetType, string optionName)
    {
        var normalized = new List<object?>(values.Count);
        foreach (object? value in values)
        {
            normalized.Add(NormalizeMetadataValue(value, targetType, "allowed value", optionName, allowNull: AllowsNull(targetType)));
        }

        return normalized;
    }

    private static object? NormalizeMetadataValue(object? value, Type targetType, string label, string optionName, bool allowNull)
    {
        if (value is null)
        {
            if (allowNull)
            {
                return null;
            }

            throw new InvalidOperationException($"Binding '{optionName}' {label} metadata cannot be null.");
        }

        Type conversionType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (conversionType.IsInstanceOfType(value))
        {
            return value;
        }

        try
        {
            if (conversionType.IsEnum)
            {
                if (value is string enumText)
                {
                    return Enum.Parse(conversionType, enumText, ignoreCase: false);
                }

                return Enum.ToObject(conversionType, value);
            }

            return Convert.ChangeType(value, conversionType, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is InvalidCastException || ex is OverflowException)
        {
            throw new InvalidOperationException($"Binding '{optionName}' {label} metadata must be compatible with {conversionType.Name}.", ex);
        }
    }

    private static int CompareMetadataValues(object left, object right, string optionName)
    {
        if (left is not IComparable comparable)
        {
            throw new InvalidOperationException($"Binding '{optionName}' range metadata must be comparable.");
        }

        try
        {
            return comparable.CompareTo(right);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException($"Binding '{optionName}' range metadata values must be comparable with each other.", ex);
        }
    }

    private static bool AllowsNull(Type type)
    {
        return !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
    }

    private static void AddUnique(HashSet<string> seen, string name)
    {
        if (!seen.Add(name))
        {
            throw new InvalidOperationException($"Duplicate command member name or alias '{name}'.");
        }
    }

    private abstract class ValueMemberDraft
    {
        public Func<CommandAutocompleteContext, IEnumerable<string>>? ValueCandidateProvider { get; set; }
        public object? RangeMinimum { get; set; }
        public object? RangeMaximum { get; set; }
        public List<object?> AllowedValues { get; set; } = new List<object?>();
        public bool SuggestAllowedValues { get; set; }
    }

    private sealed class ArgumentDraft : ValueMemberDraft
    {
        public ArgumentDraft(PropertyInfo property, string id, string name, string description, List<string> examples)
        {
            Property = property;
            Id = id;
            Name = name;
            Description = description;
            Examples = examples;
        }

        public PropertyInfo Property { get; }
        public string Id { get; }
        public bool IsOptional { get; set; }

        public string Name { get; }

        public string Description { get; }
        public List<string> Examples { get; }
    }

    private sealed class FlagDraft
    {
        public FlagDraft(PropertyInfo property, string id, string name, List<string> aliases, string description)
        {
            if (property.PropertyType != typeof(bool))
            {
                throw new ArgumentException("Flags can only bind to bool properties.", nameof(property));
            }

            Property = property;
            Id = id;
            Name = name;
            Aliases = aliases;
            Description = description;
        }

        public PropertyInfo Property { get; }
        public string Id { get; }

        public string Name { get; }

        public List<string> Aliases { get; }

        public string Description { get; }
    }

    private sealed class OptionDraft : ValueMemberDraft
    {
        public OptionDraft(PropertyInfo property, string id, string name, List<string> aliases, string description, List<string> examples)
        {
            Property = property;
            Id = id;
            Name = name;
            Aliases = aliases;
            Description = description;
            Examples = examples;
        }

        public PropertyInfo Property { get; }
        public string Id { get; }

        public string Name { get; }

        public List<string> Aliases { get; }

        public string Description { get; }
        public List<string> Examples { get; }

    }
}
