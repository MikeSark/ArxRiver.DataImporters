using ArxRiver.DataImporters.Core.Attributes;
using ArxRiver.DataImporters.Core.Validation;
using ArxRiver.DataImporters.Csv.Attributes;

namespace ArxRiver.DataImporters.Csv.Tests;

/// <summary>Simple DTO mapped by CSV header names.</summary>
public class CsvSimpleDto
{
    [CsvColumn("name")]
    public string Name { get; set; } = "";

    [CsvColumn("age")]
    public int Age { get; set; }

    [CsvColumn("score")]
    public double Score { get; set; }
}

/// <summary>DTO using property name convention (no CsvColumn attribute).</summary>
public class CsvConventionDto
{
    public string Title { get; set; } = "";
    public int Year { get; set; }
}

/// <summary>DTO mapped by 1-based column number (no header row).</summary>
public class CsvNumberDto
{
    [CsvColumn(1)]
    public string Name { get; set; } = "";

    [CsvColumn(2)]
    public int Age { get; set; }

    [CsvColumn(3)]
    public double Score { get; set; }
}

/// <summary>DTO with nullable types.</summary>
public class CsvNullableDto
{
    [CsvColumn("value")]
    public int? Value { get; set; }

    [CsvColumn("label")]
    public string? Label { get; set; }
}

/// <summary>DTO with inline validation attributes.</summary>
[InlineValidation("row.Name.Length > 0", ErrorMessage = "Name is required", RuleName = "NameRequired")]
public class CsvValidatedDto
{
    [CsvColumn("name")]
    public string Name { get; set; } = "";

    [CsvColumn("score")]
    [InlineValidation("row.Score >= 0 && row.Score <= 100", ErrorMessage = "Score must be 0-100", RuleName = "ScoreRange")]
    public int Score { get; set; }
}

/// <summary>DTO with a class-level IRowValidator.</summary>
[Validator(typeof(CsvCrossFieldValidator), RuleName = "CrossFieldCheck")]
public class CsvCrossFieldDto
{
    [CsvColumn("min")]
    public int Min { get; set; }

    [CsvColumn("max")]
    public int Max { get; set; }
}

public class CsvCrossFieldValidator : IRowValidator<CsvCrossFieldDto>
{
    public bool Validate(CsvCrossFieldDto row, out string? errorMessage)
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

/// <summary>DTO with various types for conversion testing.</summary>
public class CsvTypesDto
{
    [CsvColumn("date")]
    public DateTime Date { get; set; }

    [CsvColumn("active")]
    public bool Active { get; set; }

    [CsvColumn("amount")]
    public decimal Amount { get; set; }

    [CsvColumn("count")]
    public long Count { get; set; }
}
