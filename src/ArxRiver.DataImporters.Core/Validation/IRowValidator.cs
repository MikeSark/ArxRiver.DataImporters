namespace ArxRiver.DataImporters.Core.Validation;

/// <summary>
/// Implement this interface for complex, reusable validation logic.
/// Referenced by <see cref="Attributes.ValidatorAttribute"/>.
/// </summary>
public interface IRowValidator<in T>
{
    bool Validate(T row, out string? errorMessage);
}