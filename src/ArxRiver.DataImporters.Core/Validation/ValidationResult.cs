namespace ArxRiver.DataImporters.Core.Validation;

/// <summary>
/// Represents a single validation failure for a specific row.
/// </summary>
public sealed record ValidationResult(
    int RowNumber,
    string RuleName,
    string PropertyName,
    string ErrorMessage);