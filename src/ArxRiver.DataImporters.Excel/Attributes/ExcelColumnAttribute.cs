namespace ArxRiver.DataImporters.Excel.Attributes;

/// <summary>
/// Maps a DTO property to an Excel column by header name or 1-based column number.
/// If neither is specified, the property name is matched against the header row (case-insensitive).
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class ExcelColumnAttribute : Attribute
{
    /// <summary>Maps by header name (row 1 text). Takes priority over <see cref="Number"/>.</summary>
    public string? Name { get; }

    /// <summary>Maps by 1-based column number. Used when <see cref="Name"/> is null.</summary>
    public int Number { get; }

    public ExcelColumnAttribute(string name)
    {
        Name = name;
        Number = -1;
    }

    public ExcelColumnAttribute(int number)
    {
        Number = number;
    }
}