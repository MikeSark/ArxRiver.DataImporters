using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using ArxRiver.DataImporters.Core.Reporting;
using ArxRiver.DataImporters.Core.Validation;

namespace ArxRiver.DataImporters.Json.Importing;

/// <summary>
/// Generic JSON importer that reads objects from a JSON array into typed DTOs, validates them, and generates reports.
/// </summary>
public sealed class JsonImporter<T> where T : class, new()
{
    private readonly string _filePath;
    private readonly string? _arrayPath;

    private List<(T Item, int RowNumber)>? _importedRows;
    private ReadOnlyCollection<ValidationResult>? _validationResults;

    private readonly List<Func<T, int, IEnumerable<ValidationResult>>> _fluentRules = [];

    /// <summary>
    /// Creates a new JSON importer.
    /// </summary>
    /// <param name="filePath">Path to the JSON file.</param>
    /// <param name="arrayPath">Optional dot-separated path to the array inside the JSON (e.g. "data.employees"). If null, the root must be an array.</param>
    public JsonImporter(string filePath, string? arrayPath = null)
    {
        _filePath = filePath;
        _arrayPath = arrayPath;
    }

    /// <summary>
    /// Registers a column-level validator using a lambda/function.
    /// The validator receives both the property value and the full row for context.
    /// </summary>
    public JsonImporter<T> ForColumn<TProp>(Expression<Func<T, TProp>> selector,
                                            Func<TProp, T, bool> validator,
                                            string? errorMessage = null)
    {
        var memberExpr = selector.Body as MemberExpression
                         ?? throw new ArgumentException("Selector must be a simple property access expression.", nameof(selector));

        var propInfo = memberExpr.Member as PropertyInfo
                       ?? throw new ArgumentException("Selector must reference a property.", nameof(selector));

        var compiledSelector = selector.Compile();
        var propName = propInfo.Name;
        var errMsg = errorMessage ?? $"Column validation failed for {propName}";

        _fluentRules.Add((row, rowNum) =>
            {
                var value = compiledSelector(row);
                if (!validator(value, row))
                    return [new ValidationResult(rowNum, $"ForColumn:{propName}", propName, errMsg)];
                return [];
            });

        return this;
    }

    /// <summary>
    /// Reads the JSON file and returns a readonly collection of DTOs.
    /// </summary>
    public ReadOnlyCollection<T> Import()
    {
        var json = File.ReadAllText(_filePath);
        using var doc = JsonDocument.Parse(json);

        var arrayElement = NavigateToArray(doc.RootElement);

        if (arrayElement.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException(
                _arrayPath is null
                    ? "Root JSON element must be an array."
                    : $"Element at path '{_arrayPath}' is not an array.");

        var mapping = JsonColumnMapping<T>.Build();
        _importedRows = [];

        var index = 1; // 1-based "row number"
        foreach (var element in arrayElement.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                index++;
                continue;
            }

            var dto = new T();
            var props = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

            foreach (var prop in element.EnumerateObject())
            {
                props[prop.Name] = prop.Value;
            }

            foreach (var pm in mapping.Mappings)
            {
                if (props.TryGetValue(pm.JsonPropertyName, out var jsonValue))
                {
                    var value = ConvertJsonValue(jsonValue, pm.Property.PropertyType);
                    pm.Property.SetValue(dto, value);
                }
            }

            _importedRows.Add((dto, index));
            index++;
        }

        return _importedRows.Select(r => r.Item).ToList().AsReadOnly();
    }

    /// <summary>
    /// Validates all imported rows using both attribute-based and fluent validators.
    /// Must be called after <see cref="Import"/>.
    /// </summary>
    public ReadOnlyCollection<ValidationResult> Validate()
    {
        if (_importedRows is null)
            throw new InvalidOperationException("Call Import() before Validate().");

        var pipeline = new ValidationPipeline<T>(_fluentRules);
        _validationResults = pipeline.Validate(_importedRows);
        return _validationResults;
    }

    /// <summary>Returns rows that have zero validation errors.</summary>
    public ReadOnlyCollection<T> GetValidRows()
    {
        EnsureValidated();
        var invalidRowNumbers = new HashSet<int>(_validationResults!.Select(v => v.RowNumber));
        return _importedRows!
            .Where(r => !invalidRowNumbers.Contains(r.RowNumber))
            .Select(r => r.Item)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>Returns rows that have at least one validation error.</summary>
    public ReadOnlyCollection<T> GetInvalidRows()
    {
        EnsureValidated();
        var invalidRowNumbers = new HashSet<int>(_validationResults!.Select(v => v.RowNumber));
        return _importedRows!
            .Where(r => invalidRowNumbers.Contains(r.RowNumber))
            .Select(r => r.Item)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Creates a <see cref="ReportGenerator{T}"/> for the imported and validated data.
    /// Must be called after <see cref="Validate"/>.
    /// </summary>
    public ReportGenerator<T> CreateReportGenerator()
    {
        EnsureValidated();
        return new ReportGenerator<T>(_importedRows!, _validationResults!);
    }

    private void EnsureValidated()
    {
        if (_validationResults is null)
            throw new InvalidOperationException("Call Validate() before accessing validation results.");
    }

    private JsonElement NavigateToArray(JsonElement root)
    {
        if (_arrayPath is null)
            return root;

        var current = root;
        foreach (var segment in _arrayPath.Split('.'))
        {
            if (current.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException(
                    $"Cannot navigate path '{_arrayPath}': expected object at segment '{segment}'.");

            if (!current.TryGetProperty(segment, out var next))
            {
                // Try case-insensitive
                var found = false;
                foreach (var prop in current.EnumerateObject())
                {
                    if (string.Equals(prop.Name, segment, StringComparison.OrdinalIgnoreCase))
                    {
                        next = prop.Value;
                        found = true;
                        break;
                    }
                }

                if (!found)
                    throw new InvalidOperationException(
                        $"Cannot navigate path '{_arrayPath}': property '{segment}' not found.");
            }

            current = next;
        }

        return current;
    }

    private static object? ConvertJsonValue(JsonElement element, Type targetType)
    {
        if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
        {
            if (targetType == typeof(string))
                return "";
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlying == typeof(string))
            return element.GetString() ?? "";

        if (underlying == typeof(int))
            return element.TryGetInt32(out var i) ? i : (int)element.GetDouble();

        if (underlying == typeof(long))
            return element.TryGetInt64(out var l) ? l : (long)element.GetDouble();

        if (underlying == typeof(double))
            return element.GetDouble();

        if (underlying == typeof(decimal))
            return element.TryGetDecimal(out var d) ? d : (decimal)element.GetDouble();

        if (underlying == typeof(DateTime))
            return element.ValueKind == JsonValueKind.String
                ? DateTime.Parse(element.GetString()!)
                : throw new InvalidOperationException($"Cannot convert {element.ValueKind} to DateTime.");

        if (underlying == typeof(bool))
            return element.ValueKind switch
                   {
                       JsonValueKind.True => true,
                       JsonValueKind.False => false,
                       JsonValueKind.String => bool.Parse(element.GetString()!),
                       _ => throw new InvalidOperationException($"Cannot convert {element.ValueKind} to bool.")
                   };

        // Fallback: try parsing from string representation
        return Convert.ChangeType(element.GetRawText().Trim('"'), underlying);
    }
}