using ArxRiver.DataImporters.Core.Attributes;
using ArxRiver.DataImporters.Core.Validation;
using ArxRiver.DataImporters.Tsv.Attributes;

namespace ArxRiver.DataImporters.Tsv.Tests;

/// <summary>Simple DTO mapped by TSV header names.</summary>
public class TsvSimpleDto
{
    [TsvColumn("name")]
    public string Name { get; set; } = "";

    [TsvColumn("age")]
    public int Age { get; set; }

    [TsvColumn("score")]
    public double Score { get; set; }
}

/// <summary>DTO using property name convention (no TsvColumn attribute).</summary>
public class TsvConventionDto
{
    public string Title { get; set; } = "";
    public int Year { get; set; }
}

/// <summary>DTO mapped by 1-based column number (no header row).</summary>
public class TsvNumberDto
{
    [TsvColumn(1)]
    public string Name { get; set; } = "";

    [TsvColumn(2)]
    public int Age { get; set; }

    [TsvColumn(3)]
    public double Score { get; set; }
}

/// <summary>DTO with nullable types.</summary>
public class TsvNullableDto
{
    [TsvColumn("value")]
    public int? Value { get; set; }

    [TsvColumn("label")]
    public string? Label { get; set; }
}

/// <summary>DTO with inline validation attributes.</summary>
[InlineValidation("Row.Name.Length > 0", ErrorMessage = "Name is required", RuleName = "NameRequired")]
public class TsvValidatedDto
{
    [TsvColumn("name")]
    public string Name { get; set; } = "";

    [TsvColumn("score")]
    [InlineValidation("Row.Score >= 0 && Row.Score <= 100", ErrorMessage = "Score must be 0-100", RuleName = "ScoreRange")]
    public int Score { get; set; }
}

/// <summary>DTO with a class-level IRowValidator.</summary>
[Validator(typeof(TsvCrossFieldValidator), RuleName = "CrossFieldCheck")]
public class TsvCrossFieldDto
{
    [TsvColumn("min")]
    public int Min { get; set; }

    [TsvColumn("max")]
    public int Max { get; set; }
}

public class TsvCrossFieldValidator : IRowValidator<TsvCrossFieldDto>
{
    public bool Validate(TsvCrossFieldDto row, out string? errorMessage)
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
public class TsvTypesDto
{
    [TsvColumn("date")]
    public DateTime Date { get; set; }

    [TsvColumn("active")]
    public bool Active { get; set; }

    [TsvColumn("amount")]
    public decimal Amount { get; set; }

    [TsvColumn("count")]
    public long Count { get; set; }
}
