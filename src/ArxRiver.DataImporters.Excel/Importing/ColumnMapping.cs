using System.Reflection;
using ArxRiver.DataImporters.Excel.Attributes;
using ClosedXML.Excel;

namespace ArxRiver.DataImporters.Excel.Importing;

/// <summary>
/// Resolves the mapping between DTO properties and Excel column indices.
/// </summary>
internal sealed class ColumnMapping<T> where T : class
{
    public required IReadOnlyList<PropertyMapping> Mappings { get; init; }

    public sealed record PropertyMapping(PropertyInfo Property, int ColumnIndex); // 1-based

    public static ColumnMapping<T> Build(IXLWorksheet worksheet)
    {
        var headerRow = worksheet.Row(1);
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var cell in headerRow.CellsUsed())
        {
            headers[cell.GetString().Trim()] = cell.Address.ColumnNumber;
        }

        var mappings = new List<PropertyMapping>();

        foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var attr = prop.GetCustomAttribute<ExcelColumnAttribute>();

            int columnIndex;
            if (attr?.Name is not null)
            {
                if (!headers.TryGetValue(attr.Name, out columnIndex))
                    throw new InvalidOperationException(
                        $"Column '{attr.Name}' not found in worksheet headers for property '{prop.Name}'.");
            }
            else if (attr is not null && attr.Number > 0)
            {
                columnIndex = attr.Number;
            }
            else
            {
                // Convention: use property name as header name
                if (!headers.TryGetValue(prop.Name, out columnIndex))
                    continue; // Skip unmapped properties without error
            }

            mappings.Add(new PropertyMapping(prop, columnIndex));
        }

        return new ColumnMapping<T> { Mappings = mappings };
    }
}
