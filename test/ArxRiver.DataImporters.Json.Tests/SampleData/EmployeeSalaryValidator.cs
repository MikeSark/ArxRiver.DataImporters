using ArxRiver.DataImporters.Core.Validation;

namespace ArxRiver.DataImporters.Json.Tests.SampleData;

/// <summary>
/// Complex cross-field validation: salary must be within department-specific ranges.
/// </summary>
public sealed class EmployeeSalaryValidator : IRowValidator<EmployeeDto>
{
    private static readonly Dictionary<string, (decimal Min, decimal Max)> _departmentRanges = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Engineering"] = (60_000m, 250_000m),
        ["Marketing"] = (40_000m, 180_000m),
        ["Sales"] = (35_000m, 200_000m),
        ["HR"] = (40_000m, 150_000m),
    };

    public bool Validate(EmployeeDto row, out string? errorMessage)
    {
        if (_departmentRanges.TryGetValue(row.Department, out var range))
        {
            if (row.Salary < range.Min || row.Salary > range.Max)
            {
                errorMessage = $"Salary {row.Salary:C} out of range for {row.Department} (expected {range.Min:C} - {range.Max:C})";
                return false;
            }
        }

        errorMessage = null;
        return true;
    }
}
