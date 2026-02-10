namespace ArxRiver.DataImporters.Core.Attributes;

/// <summary>
/// References a class implementing <c>IRowValidator&lt;T&gt;</c> for complex, multi-field validation.
/// Can be placed on the DTO class (row-level) or on individual properties.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = true)]
public sealed class ValidatorAttribute(Type validatorType) : Attribute
{
    public Type ValidatorType { get; } = validatorType;

    /// <summary>Optional human-readable rule name for reporting.</summary>
    public string? RuleName { get; init; }
}