namespace ArxRiver.DataImporters.Core.Attributes;

/// <summary>
/// Accepts a C# expression string evaluated at runtime via Roslyn scripting.
/// The variable <c>Row</c> refers to the current DTO instance.
/// Example: <c>[InlineValidation("Row.Age &gt;= 18")]</c>
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = true)]
public sealed class InlineValidationAttribute(string expression) : Attribute
{
    public string Expression { get; } = expression;

    /// <summary>Custom error message when validation fails.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Optional human-readable rule name for reporting.</summary>
    public string? RuleName { get; init; }
}