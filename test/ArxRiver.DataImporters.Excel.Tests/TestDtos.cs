using ArxRiver.DataImporters.Core.Attributes;
using ArxRiver.DataImporters.Core.Validation;
using ArxRiver.DataImporters.Excel.Attributes;

namespace ArxRiver.DataImporters.Excel.Tests;

/// <summary>Simple DTO mapped by header names — no validation attributes.</summary>
public class SimpleDto
{
    [ExcelColumn("Name")]
    public string Name { get; set; } = "";

    [ExcelColumn("Age")]
    public int Age { get; set; }

    [ExcelColumn("Score")]
    public double Score { get; set; }
}

/// <summary>DTO mapped by column numbers.</summary>
public class NumberMappedDto
{
    [ExcelColumn(1)]
    public string City { get; set; } = "";

    [ExcelColumn(2)]
    public int Population { get; set; }

    [ExcelColumn(3)]
    public decimal Area { get; set; }
}

/// <summary>DTO using property name convention (no ExcelColumn attribute).</summary>
public class ConventionDto
{
    public string Title { get; set; } = "";
    public int Year { get; set; }
}

/// <summary>DTO with nullable types.</summary>
public class NullableDto
{
    [ExcelColumn("Value")]
    public int? Value { get; set; }

    [ExcelColumn("Label")]
    public string? Label { get; set; }
}

/// <summary>DTO with inline validation attributes.</summary>
[InlineValidation("row.Name.Length > 0", ErrorMessage = "Name is required", RuleName = "NameRequired")]
public class ValidatedDto
{
    [ExcelColumn("Name")]
    public string Name { get; set; } = "";

    [ExcelColumn("Score")]
    [InlineValidation("row.Score >= 0 && row.Score <= 100", ErrorMessage = "Score must be 0-100", RuleName = "ScoreRange")]
    public int Score { get; set; }
}

/// <summary>DTO with a class-level IRowValidator.</summary>
[Validator(typeof(CrossFieldValidator), RuleName = "CrossFieldCheck")]
public class CrossFieldDto
{
    [ExcelColumn("Min")]
    public int Min { get; set; }

    [ExcelColumn("Max")]
    public int Max { get; set; }
}

public class CrossFieldValidator : IRowValidator<CrossFieldDto>
{
    public bool Validate(CrossFieldDto row, out string? errorMessage)
    {
        if (row.Min > row.Max)
        {
            errorMessage = "Min must be less than or equal to Max";
            return false;
        }
        errorMessage = null;
        return true;
    }
}

/// <summary>DTO with date and bool types for conversion testing.</summary>
public class TypesDto
{
    [ExcelColumn("Date")]
    public DateTime Date { get; set; }

    [ExcelColumn("Active")]
    public bool Active { get; set; }

    [ExcelColumn("Amount")]
    public decimal Amount { get; set; }

    [ExcelColumn("Count")]
    public long Count { get; set; }
}
