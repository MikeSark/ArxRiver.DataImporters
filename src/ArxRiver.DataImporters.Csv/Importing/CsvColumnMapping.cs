using System.Reflection;
using ArxRiver.DataImporters.Csv.Attributes;

namespace ArxRiver.DataImporters.Csv.Importing;

/// <summary>
/// Resolves the mapping between DTO properties and CSV column indices.
/// </summary>
internal sealed class CsvColumnMapping<T> where T : class
{
    public required IReadOnlyList<PropertyMapping> Mappings { get; init; }

    public sealed record PropertyMapping(PropertyInfo Property, int ColumnIndex); // 0-based

    /// <summary>
    /// Builds mapping using header names. Properties are matched by attribute name, attribute number, or property name convention.
    /// </summary>
    public static CsvColumnMapping<T> Build(string[] headers)
    {
        var headerLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Length; i++)
        {
            headerLookup[headers[i].Trim()] = i;
        }

        var mappings = new List<PropertyMapping>();

        foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var attr = prop.GetCustomAttribute<CsvColumnAttribute>();

            int columnIndex;
            if (attr?.Name is not null)
            {
                if (!headerLookup.TryGetValue(attr.Name, out columnIndex))
                    throw new InvalidOperationException(
                        $"Column '{attr.Name}' not found in CSV headers for property '{prop.Name}'.");
            }
            else if (attr is not null && attr.Number > 0)
            {
                columnIndex = attr.Number - 1; // convert 1-based to 0-based
            }
            else
            {
                // Convention: use property name as header name
                if (!headerLookup.TryGetValue(prop.Name, out columnIndex))
                    continue; // Skip unmapped properties without error
            }

            mappings.Add(new PropertyMapping(prop, columnIndex));
        }

        return new CsvColumnMapping<T> { Mappings = mappings };
    }

    /// <summary>
    /// Builds mapping using only 1-based column numbers from <see cref="CsvColumnAttribute"/>. Used when there is no header row.
    /// </summary>
    public static CsvColumnMapping<T> BuildByNumber()
    {
        var mappings = new List<PropertyMapping>();

        foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var attr = prop.GetCustomAttribute<CsvColumnAttribute>();
            if (attr is not null && attr.Number > 0)
            {
                mappings.Add(new PropertyMapping(prop, attr.Number - 1)); // convert 1-based to 0-based
            }
        }

        return new CsvColumnMapping<T> { Mappings = mappings };
    }
}
