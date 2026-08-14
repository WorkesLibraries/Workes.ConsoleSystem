using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
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
    private readonly List<CommandConstraintDefinition> _constraints = new List<CommandConstraintDefinition>();
    private readonly List<CommandSuccessOutputDefinition> _successOutputs = new List<CommandSuccessOutputDefinition>();
    private string _description = string.Empty;
    private Type? _stateType;
    private Delegate? _handler;
    private OptionDraft? _lastOption;
    private bool? _echoInput;
    private string? _echoInputDefaultStyle;

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
        return this;
    }

    /// <summary>
    /// Adds a required positional argument.
    /// </summary>
    /// <typeparam name="TState">The command state type.</typeparam>
    /// <param name="propertySelector">The state property selector.</param>
    /// <param name="name">The argument name.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder Argument<TState>(Expression<Func<TState, object?>> propertySelector, string name)
    {
        PropertyInfo property = GetProperty(propertySelector);
        UseStateType(typeof(TState));
        _arguments.Add(new ArgumentDraft(property, ValidateName(name, nameof(name)), string.Empty));
        _lastOption = null;
        return this;
    }

    /// <summary>
    /// Adds a boolean flag.
    /// </summary>
    /// <typeparam name="TState">The command state type.</typeparam>
    /// <param name="propertySelector">The state property selector.</param>
    /// <param name="name">The canonical flag name.</param>
    /// <param name="aliases">The flag aliases.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder Flag<TState>(Expression<Func<TState, object?>> propertySelector, string name, params string[] aliases)
    {
        PropertyInfo property = GetProperty(propertySelector);
        UseStateType(typeof(TState));
        _flags.Add(new FlagDraft(property, ValidateName(name, nameof(name)), ValidateAliases(aliases), string.Empty));
        _lastOption = null;
        return this;
    }

    /// <summary>
    /// Adds a named option.
    /// </summary>
    /// <typeparam name="TState">The command state type.</typeparam>
    /// <param name="propertySelector">The state property selector.</param>
    /// <param name="name">The canonical option name.</param>
    /// <param name="aliases">The option aliases.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder Option<TState>(Expression<Func<TState, object?>> propertySelector, string name, params string[] aliases)
    {
        PropertyInfo property = GetProperty(propertySelector);
        UseStateType(typeof(TState));
        _lastOption = new OptionDraft(property, ValidateName(name, nameof(name)), ValidateAliases(aliases), string.Empty);
        _options.Add(_lastOption);
        return this;
    }

    /// <summary>
    /// Sets default value metadata on the most recently added option.
    /// </summary>
    /// <param name="value">The default value metadata.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder Default(object? value)
    {
        RequireLastOption().DefaultValue = value;
        return this;
    }

    /// <summary>
    /// Sets range metadata on the most recently added option.
    /// </summary>
    /// <param name="minimum">The inclusive minimum value metadata.</param>
    /// <param name="maximum">The inclusive maximum value metadata.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder Range(object? minimum, object? maximum)
    {
        OptionDraft option = RequireLastOption();
        option.RangeMinimum = minimum;
        option.RangeMaximum = maximum;
        return this;
    }

    /// <summary>
    /// Sets allowed value metadata on the most recently added option.
    /// </summary>
    /// <param name="values">The allowed value metadata.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder AllowedValues(params object?[] values)
    {
        RequireLastOption().AllowedValues = values is null
            ? throw new ArgumentNullException(nameof(values))
            : new List<object?>(values);
        return this;
    }

    /// <summary>
    /// Adds constraint metadata.
    /// </summary>
    /// <param name="name">The constraint name.</param>
    /// <param name="message">The constraint failure message.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder Constraint(string name, string message)
    {
        _constraints.Add(new CommandConstraintDefinition(
            ValidateName(name, nameof(name)),
            message ?? throw new ArgumentNullException(nameof(message))));
        _lastOption = null;
        return this;
    }

    /// <summary>
    /// Echoes submitted input for this command using manager defaults.
    /// </summary>
    /// <returns>The current builder.</returns>
    public CommandBuilder EchoInput()
    {
        _echoInput = true;
        _echoInputDefaultStyle = null;
        _lastOption = null;
        return this;
    }

    /// <summary>
    /// Echoes submitted input for this command using a command-specific default style.
    /// </summary>
    /// <param name="defaultStyle">The default style identifier.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder EchoInput(string defaultStyle)
    {
        _echoInput = true;
        _echoInputDefaultStyle = ValidateName(defaultStyle, nameof(defaultStyle));
        _lastOption = null;
        return this;
    }

    /// <summary>
    /// Disables echoed input for this command.
    /// </summary>
    /// <returns>The current builder.</returns>
    public CommandBuilder DoNotEchoInput()
    {
        _echoInput = false;
        _echoInputDefaultStyle = null;
        _lastOption = null;
        return this;
    }

    /// <summary>
    /// Adds declared inline success output from plain text.
    /// </summary>
    /// <param name="text">The output text.</param>
    /// <param name="defaultStyle">The optional default style identifier.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder SuccessOutputInline(string text, string? defaultStyle = null)
    {
        _successOutputs.Add(new CommandSuccessOutputDefinition(CommandOutput.InlineText(text, defaultStyle)));
        _lastOption = null;
        return this;
    }

    /// <summary>
    /// Adds declared inline success output from console text.
    /// </summary>
    /// <param name="content">The output content.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder SuccessOutputInline(ConsoleText content)
    {
        _successOutputs.Add(new CommandSuccessOutputDefinition(CommandOutput.InlineText(content)));
        _lastOption = null;
        return this;
    }

    /// <summary>
    /// Adds declared block success output from plain text.
    /// </summary>
    /// <param name="text">The output text.</param>
    /// <param name="defaultStyle">The optional default style identifier.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder SuccessOutputBlock(string text, string? defaultStyle = null)
    {
        _successOutputs.Add(new CommandSuccessOutputDefinition(CommandOutput.BlockText(text, defaultStyle)));
        _lastOption = null;
        return this;
    }

    /// <summary>
    /// Adds declared block success output from console text.
    /// </summary>
    /// <param name="content">The output content.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder SuccessOutputBlock(ConsoleText content)
    {
        _successOutputs.Add(new CommandSuccessOutputDefinition(CommandOutput.BlockText(content)));
        _lastOption = null;
        return this;
    }

    /// <summary>
    /// Adds declared inline success output from formatting-aware markup metadata.
    /// </summary>
    /// <param name="markup">The output markup.</param>
    /// <param name="defaultStyle">The optional default style identifier.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder SuccessOutputInlineMarkup(string markup, string? defaultStyle = null)
    {
        _successOutputs.Add(new CommandSuccessOutputDefinition(CommandOutput.InlineMarkup(markup, defaultStyle)));
        _lastOption = null;
        return this;
    }

    /// <summary>
    /// Adds declared block success output from formatting-aware markup metadata.
    /// </summary>
    /// <param name="markup">The output markup.</param>
    /// <param name="defaultStyle">The optional default style identifier.</param>
    /// <returns>The current builder.</returns>
    public CommandBuilder SuccessOutputBlockMarkup(string markup, string? defaultStyle = null)
    {
        _successOutputs.Add(new CommandSuccessOutputDefinition(CommandOutput.BlockMarkup(markup, defaultStyle)));
        _lastOption = null;
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

        return new CommandDefinition(
            _path,
            _description,
            _stateType,
            _arguments.ConvertAll(x => new CommandArgumentDefinition(
                x.Property.Name,
                x.Property.PropertyType,
                x.Name,
                x.Description)).AsReadOnly(),
            _flags.ConvertAll(x => new CommandFlagDefinition(
                x.Property.Name,
                x.Name,
                new List<string>(x.Aliases).AsReadOnly(),
                x.Description)).AsReadOnly(),
            _options.ConvertAll(x => new CommandOptionDefinition(
                x.Property.Name,
                x.Property.PropertyType,
                x.Name,
                new List<string>(x.Aliases).AsReadOnly(),
                x.Description,
                x.DefaultValue,
                x.RangeMinimum,
                x.RangeMaximum,
                new List<object?>(x.AllowedValues).AsReadOnly())).AsReadOnly(),
            new List<CommandConstraintDefinition>(_constraints).AsReadOnly(),
            new CommandEchoInputDefinition(_echoInput, _echoInputDefaultStyle),
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

    private static List<string> ValidateAliases(string[] aliases)
    {
        if (aliases is null)
        {
            throw new ArgumentNullException(nameof(aliases));
        }

        var result = new List<string>(aliases.Length);
        foreach (string alias in aliases)
        {
            result.Add(ValidateName(alias, nameof(aliases)));
        }

        return result;
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

    private OptionDraft RequireLastOption()
    {
        return _lastOption ?? throw new InvalidOperationException("Option metadata must follow an option definition.");
    }

    private void ValidateUniqueMemberNames()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (ArgumentDraft argument in _arguments)
        {
            AddUnique(seen, argument.Name);
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

    private static void AddUnique(HashSet<string> seen, string name)
    {
        if (!seen.Add(name))
        {
            throw new InvalidOperationException($"Duplicate command member name or alias '{name}'.");
        }
    }

    private sealed class ArgumentDraft
    {
        public ArgumentDraft(PropertyInfo property, string name, string description)
        {
            Property = property;
            Name = name;
            Description = description;
        }

        public PropertyInfo Property { get; }

        public string Name { get; }

        public string Description { get; }
    }

    private sealed class FlagDraft
    {
        public FlagDraft(PropertyInfo property, string name, List<string> aliases, string description)
        {
            if (property.PropertyType != typeof(bool))
            {
                throw new ArgumentException("Flags can only bind to bool properties.", nameof(property));
            }

            Property = property;
            Name = name;
            Aliases = aliases;
            Description = description;
        }

        public PropertyInfo Property { get; }

        public string Name { get; }

        public List<string> Aliases { get; }

        public string Description { get; }
    }

    private sealed class OptionDraft
    {
        public OptionDraft(PropertyInfo property, string name, List<string> aliases, string description)
        {
            Property = property;
            Name = name;
            Aliases = aliases;
            Description = description;
        }

        public PropertyInfo Property { get; }

        public string Name { get; }

        public List<string> Aliases { get; }

        public string Description { get; }

        public object? DefaultValue { get; set; }

        public object? RangeMinimum { get; set; }

        public object? RangeMaximum { get; set; }

        public List<object?> AllowedValues { get; set; } = new List<object?>();
    }
}
