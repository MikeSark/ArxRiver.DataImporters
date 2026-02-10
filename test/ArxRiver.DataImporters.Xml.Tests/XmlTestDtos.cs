using ArxRiver.DataImporters.Core.Attributes;
using ArxRiver.DataImporters.Core.Validation;
using ArxRiver.DataImporters.Xml.Attributes;

namespace ArxRiver.DataImporters.Xml.Tests;

/// <summary>Simple DTO mapped by child element names.</summary>
public class XmlSimpleDto
{
    [XmlColumn("name")]
    public string Name { get; set; } = "";

    [XmlColumn("age")]
    public int Age { get; set; }

    [XmlColumn("score")]
    public double Score { get; set; }
}

/// <summary>DTO mapped by XML attributes instead of child elements.</summary>
public class XmlAttributeDto
{
    [XmlColumn("name", IsAttribute = true)]
    public string Name { get; set; } = "";

    [XmlColumn("age", IsAttribute = true)]
    public int Age { get; set; }

    [XmlColumn("score", IsAttribute = true)]
    public double Score { get; set; }
}

/// <summary>DTO using property name convention (no XmlColumn attribute).</summary>
public class XmlConventionDto
{
    public string Title { get; set; } = "";
    public int Year { get; set; }
}

/// <summary>DTO with nullable types.</summary>
public class XmlNullableDto
{
    [XmlColumn("value")]
    public int? Value { get; set; }

    [XmlColumn("label")]
    public string? Label { get; set; }
}

/// <summary>DTO with inline validation attributes.</summary>
[InlineValidation("Row.Name.Length > 0", ErrorMessage = "Name is required", RuleName = "NameRequired")]
public class XmlValidatedDto
{
    [XmlColumn("name")]
    public string Name { get; set; } = "";

    [XmlColumn("score")]
    [InlineValidation("Row.Score >= 0 && Row.Score <= 100", ErrorMessage = "Score must be 0-100", RuleName = "ScoreRange")]
    public int Score { get; set; }
}

/// <summary>DTO with a class-level IRowValidator.</summary>
[Validator(typeof(XmlCrossFieldValidator), RuleName = "CrossFieldCheck")]
public class XmlCrossFieldDto
{
    [XmlColumn("min")]
    public int Min { get; set; }

    [XmlColumn("max")]
    public int Max { get; set; }
}

public class XmlCrossFieldValidator : IRowValidator<XmlCrossFieldDto>
{
    public bool Validate(XmlCrossFieldDto row, out string? errorMessage)
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
public class XmlTypesDto
{
    [XmlColumn("date")]
    public DateTime Date { get; set; }

    [XmlColumn("active")]
    public bool Active { get; set; }

    [XmlColumn("amount")]
    public decimal Amount { get; set; }

    [XmlColumn("count")]
    public long Count { get; set; }
}

/// <summary>DTO with mixed element and attribute mappings.</summary>
public class XmlMixedDto
{
    [XmlColumn("id", IsAttribute = true)]
    public int Id { get; set; }

    [XmlColumn("name")]
    public string Name { get; set; } = "";

    [XmlColumn("active", IsAttribute = true)]
    public bool Active { get; set; }
}
