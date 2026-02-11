namespace ArxRiver.DataImporters.Tsv.Attributes;

/// <summary>
/// Maps a DTO property to a TSV column by header name or 1-based column number.
/// If neither is specified, the property name is matched against the header row (case-insensitive).
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class TsvColumnAttribute : Attribute
{
    /// <summary>Maps by header name (case-insensitive). Takes priority over <see cref="Number"/>.</summary>
    public string? Name { get; }

    /// <summary>Maps by 1-based column number. Used when <see cref="Name"/> is null.</summary>
    public int Number { get; }

    public TsvColumnAttribute(string name)
    {
        Name = name;
        Number = -1;
    }

    public TsvColumnAttribute(int number)
    {
        Number = number;
    }
}
