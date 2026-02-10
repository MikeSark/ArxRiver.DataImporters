using ArxRiver.DataImporters.Core.Attributes;
using ArxRiver.DataImporters.Core.Validation;
using ArxRiver.DataImporters.Json.Attributes;

namespace ArxRiver.DataImporters.Json.Tests;

/// <summary>Simple DTO mapped by JSON property names.</summary>
public class JsonSimpleDto
{
    [JsonColumn("name")]
    public string Name { get; set; } = "";

    [JsonColumn("age")]
    public int Age { get; set; }

    [JsonColumn("score")]
    public double Score { get; set; }
}

/// <summary>DTO using property name convention (no JsonColumn attribute).</summary>
public class JsonConventionDto
{
    public string Title { get; set; } = "";
    public int Year { get; set; }
}

/// <summary>DTO with nullable types.</summary>
public class JsonNullableDto
{
    [JsonColumn("value")]
    public int? Value { get; set; }

    [JsonColumn("label")]
    public string? Label { get; set; }
}

/// <summary>DTO with inline validation attributes.</summary>
[InlineValidation("row.Name.Length > 0", ErrorMessage = "Name is required", RuleName = "NameRequired")]
public class JsonValidatedDto
{
    [JsonColumn("name")]
    public string Name { get; set; } = "";

    [JsonColumn("score")]
    [InlineValidation("row.Score >= 0 && row.Score <= 100", ErrorMessage = "Score must be 0-100", RuleName = "ScoreRange")]
    public int Score { get; set; }
}

/// <summary>DTO with a class-level IRowValidator.</summary>
[Validator(typeof(JsonCrossFieldValidator), RuleName = "CrossFieldCheck")]
public class JsonCrossFieldDto
{
    [JsonColumn("min")]
    public int Min { get; set; }

    [JsonColumn("max")]
    public int Max { get; set; }
}

public class JsonCrossFieldValidator : IRowValidator<JsonCrossFieldDto>
{
    public bool Validate(JsonCrossFieldDto row, out string? errorMessage)
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
public class JsonTypesDto
{
    [JsonColumn("date")]
    public DateTime Date { get; set; }

    [JsonColumn("active")]
    public bool Active { get; set; }

    [JsonColumn("amount")]
    public decimal Amount { get; set; }

    [JsonColumn("count")]
    public long Count { get; set; }
}

/// <summary>DTO for nested array path testing.</summary>
public class JsonNestedDto
{
    [JsonColumn("id")]
    public int Id { get; set; }

    [JsonColumn("value")]
    public string Value { get; set; } = "";
}
