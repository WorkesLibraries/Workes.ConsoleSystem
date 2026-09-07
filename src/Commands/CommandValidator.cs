using System;
using System.Collections.Generic;
using Workes.ConsoleSystem.Core;

namespace Workes.ConsoleSystem.Commands;

internal static class CommandValidator
{
    public static CommandValidationResult Validate(BoundCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        foreach (CommandOptionDefinition option in command.Definition.Options)
        {
            object? value = command.Options[option.Name];
            ConsoleFailure? rangeFailure = ValidateRange(option, value);
            if (rangeFailure is not null)
            {
                return CommandValidationResult.Failed(command, rangeFailure);
            }

            ConsoleFailure? allowedValuesFailure = ValidateAllowedValues(option, value);
            if (allowedValuesFailure is not null)
            {
                return CommandValidationResult.Failed(command, allowedValuesFailure);
            }
        }

        foreach (CommandConstraintDefinition constraint in command.Definition.Constraints)
        {
            bool valid;
            try
            {
                valid = constraint.IsSatisfiedBy(command.State);
            }
            catch (Exception ex)
            {
                ConsoleFailure cause = ConsoleFailure.FromException(
                    ex,
                    ConsoleFailureKind.CommandConstraint,
                    ConsoleFailureCodes.CommandConstraintRejected);
                return CommandValidationResult.Failed(
                    command,
                    ConsoleFailures.CommandConstraint(
                        $"Constraint '{constraint.Name}' threw while validating command state.",
                        constraint.Name,
                        cause));
            }

            if (!valid)
            {
                return CommandValidationResult.Failed(
                    command,
                    ConsoleFailures.CommandConstraint(constraint.Message, constraint.Name));
            }
        }

        return CommandValidationResult.Succeeded(command);
    }

    private static ConsoleFailure? ValidateRange(CommandOptionDefinition option, object? value)
    {
        if (option.RangeMinimum is null && option.RangeMaximum is null)
        {
            return null;
        }

        if (value is null)
        {
            return ConsoleFailures.CommandConstraint(
                $"Option '{option.Name}' must be between {option.RangeMinimum} and {option.RangeMaximum}.",
                option.Name);
        }

        int minimumComparison = Compare(value, option.RangeMinimum!, option.Name);
        if (minimumComparison < 0)
        {
            return ConsoleFailures.CommandConstraint(
                $"Option '{option.Name}' must be greater than or equal to {option.RangeMinimum}.",
                option.Name);
        }

        int maximumComparison = Compare(value, option.RangeMaximum!, option.Name);
        if (maximumComparison > 0)
        {
            return ConsoleFailures.CommandConstraint(
                $"Option '{option.Name}' must be less than or equal to {option.RangeMaximum}.",
                option.Name);
        }

        return null;
    }

    private static ConsoleFailure? ValidateAllowedValues(CommandOptionDefinition option, object? value)
    {
        if (option.AllowedValues.Count == 0)
        {
            return null;
        }

        foreach (object? allowedValue in option.AllowedValues)
        {
            if (EqualityComparer<object?>.Default.Equals(value, allowedValue))
            {
                return null;
            }
        }

        return ConsoleFailures.CommandConstraint(
            $"Option '{option.Name}' must be one of: {string.Join(", ", option.AllowedValues)}.",
            option.Name);
    }

    private static int Compare(object value, object limit, string optionName)
    {
        if (value is not IComparable comparable)
        {
            throw new InvalidOperationException($"Option '{optionName}' value must be comparable for range validation.");
        }

        try
        {
            return comparable.CompareTo(limit);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException($"Option '{optionName}' value must be comparable with its configured range metadata.", ex);
        }
    }
}
