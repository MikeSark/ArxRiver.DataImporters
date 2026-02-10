using System.Collections.ObjectModel;
using System.Reflection;
using ArxRiver.DataImporters.Core.Attributes;

namespace ArxRiver.DataImporters.Core.Validation;

/// <summary>
/// Discovers validation rules from attributes and fluent registrations, then executes them against imported rows.
/// </summary>
public sealed class ValidationPipeline<T> where T : class
{
    private readonly List<Func<T, int, IEnumerable<ValidationResult>>> _rules = [];

    public ValidationPipeline(IReadOnlyList<Func<T, int, IEnumerable<ValidationResult>>>? fluentRules = null)
    {
        BuildRulesFromAttributes();

        if (fluentRules is not null)
        {
            _rules.AddRange(fluentRules);
        }
    }

    private void BuildRulesFromAttributes()
    {
        var dtoType = typeof(T);

        // Class-level [Validator] attributes
        foreach (var attr in dtoType.GetCustomAttributes<ValidatorAttribute>())
        {
            var validatorInstance = CreateValidatorInstance(attr.ValidatorType);
            var ruleName = attr.RuleName ?? attr.ValidatorType.Name;

            _rules.Add((row, rowNum) =>
                {
                    if (!validatorInstance.Validate(row, out var errorMessage))
                        return [new ValidationResult(rowNum, ruleName, "(row)", errorMessage ?? "Validation failed")];
                    return [];
                });
        }

        // Class-level [InlineValidation] attributes
        foreach (var attr in dtoType.GetCustomAttributes<InlineValidationAttribute>())
        {
            var compiled = CompileExpression(attr.Expression);
            var ruleName = attr.RuleName ?? attr.Expression;
            var errorMsg = attr.ErrorMessage ?? $"Expression failed: {attr.Expression}";

            _rules.Add((row, rowNum) => EvaluateExpression(compiled, row, rowNum, ruleName, "(row)", errorMsg));
        }

        // Property-level attributes
        foreach (var prop in dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            foreach (var attr in prop.GetCustomAttributes<ValidatorAttribute>())
            {
                var validatorInstance = CreateValidatorInstance(attr.ValidatorType);
                var ruleName = attr.RuleName ?? attr.ValidatorType.Name;
                var propName = prop.Name;

                _rules.Add((row, rowNum) =>
                    {
                        if (!validatorInstance.Validate(row, out var errorMessage))
                            return [new ValidationResult(rowNum, ruleName, propName, errorMessage ?? "Validation failed")];
                        return [];
                    });
            }

            foreach (var attr in prop.GetCustomAttributes<InlineValidationAttribute>())
            {
                var compiled = CompileExpression(attr.Expression);
                var ruleName = attr.RuleName ?? attr.Expression;
                var errorMsg = attr.ErrorMessage ?? $"Expression failed: {attr.Expression}";
                var propName = prop.Name;

                _rules.Add((row, rowNum) => EvaluateExpression(compiled, row, rowNum, ruleName, propName, errorMsg));
            }
        }
    }

    private static IEnumerable<ValidationResult> EvaluateExpression(
        Func<T, bool> compiled, T row, int rowNum, string ruleName, string propName, string errorMsg)
    {
        try
        {
            if (!compiled(row))
                return [new ValidationResult(rowNum, ruleName, propName, errorMsg)];
        }
        catch (Exception ex)
        {
            var innerMsg = ex.InnerException?.Message ?? ex.Message;
            return
            [
                new ValidationResult(rowNum, ruleName, propName,
                                     $"{errorMsg} (runtime error: {innerMsg})")
            ];
        }

        return [];
    }

    private static Func<T, bool> CompileExpression(string expression)
    {
        try
        {
            return ExpressionCache.GetOrCompile<T>(expression);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to compile inline validation expression: '{expression}'. {ex.Message}", ex);
        }
    }

    private static IRowValidator<T> CreateValidatorInstance(Type validatorType)
    {
        if (!typeof(IRowValidator<T>).IsAssignableFrom(validatorType))
            throw new InvalidOperationException(
                $"Type {validatorType.Name} does not implement IRowValidator<{typeof(T).Name}>.");

        return (IRowValidator<T>)(Activator.CreateInstance(validatorType)
                                  ?? throw new InvalidOperationException($"Failed to create instance of {validatorType.Name}."));
    }

    public ReadOnlyCollection<ValidationResult> Validate(IReadOnlyList<(T Item, int RowNumber)> rows)
    {
        var results = new List<ValidationResult>();

        foreach (var (item, rowNumber) in rows)
        {
            foreach (var rule in _rules)
            {
                results.AddRange(rule(item, rowNumber));
            }
        }

        return results.AsReadOnly();
    }
}