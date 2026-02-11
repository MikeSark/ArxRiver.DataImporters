using System.Reflection;
using ArxRiver.DataImporters.Tsv.Attributes;

namespace ArxRiver.DataImporters.Tsv.Importing;

/// <summary>
/// Resolves the mapping between DTO properties and TSV column indices.
/// </summary>
internal sealed class TsvColumnMapping<T> where T : class
{
    public required IReadOnlyList<PropertyMapping> Mappings { get; init; }

    public sealed record PropertyMapping(PropertyInfo Property, int ColumnIndex); // 0-based

    /// <summary>
    /// Builds mapping using header names. Properties are matched by attribute name, attribute number, or property name convention.
    /// </summary>
    public static TsvColumnMapping<T> Build(string[] headers)
    {
        var headerLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Length; i++)
        {
            headerLookup[headers[i].Trim()] = i;
        }

        var mappings = new List<PropertyMapping>();

        foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var attr = prop.GetCustomAttribute<TsvColumnAttribute>();

            int columnIndex;
            if (attr?.Name is not null)
            {
                if (!headerLookup.TryGetValue(attr.Name, out columnIndex))
                    throw new InvalidOperationException(
                        $"Column '{attr.Name}' not found in TSV headers for property '{prop.Name}'.");
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

        return new TsvColumnMapping<T> { Mappings = mappings };
    }

    /// <summary>
    /// Builds mapping using only 1-based column numbers from <see cref="TsvColumnAttribute"/>. Used when there is no header row.
    /// </summary>
    public static TsvColumnMapping<T> BuildByNumber()
    {
        var mappings = new List<PropertyMapping>();

        foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var attr = prop.GetCustomAttribute<TsvColumnAttribute>();
            if (attr is not null && attr.Number > 0)
            {
                mappings.Add(new PropertyMapping(prop, attr.Number - 1)); // convert 1-based to 0-based
            }
        }

        return new TsvColumnMapping<T> { Mappings = mappings };
    }
}
