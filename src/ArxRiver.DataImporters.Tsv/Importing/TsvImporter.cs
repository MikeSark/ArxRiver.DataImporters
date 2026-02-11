using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using ArxRiver.DataImporters.Core.Reporting;
using ArxRiver.DataImporters.Core.Validation;

namespace ArxRiver.DataImporters.Tsv.Importing;

/// <summary>
/// Generic TSV importer that reads rows from a TSV file into typed DTOs, validates them, and generates reports.
/// </summary>
public sealed class TsvImporter<T> where T : class, new()
{
    private readonly string _filePath;
    private readonly bool _hasHeaderRow;
    private readonly char _delimiter;

    private List<(T Item, int RowNumber)>? _importedRows;
    private ReadOnlyCollection<ValidationResult>? _validationResults;

    private readonly List<Func<T, int, IEnumerable<ValidationResult>>> _fluentRules = [];

    /// <summary>
    /// Creates a new TSV importer.
    /// </summary>
    /// <param name="filePath">Path to the TSV file.</param>
    /// <param name="hasHeaderRow">Whether the first row contains column headers. Default is true.</param>
    /// <param name="delimiter">Field delimiter character. Default is tab.</param>
    public TsvImporter(string filePath, bool hasHeaderRow = true, char delimiter = '\t')
    {
        _filePath = filePath;
        _hasHeaderRow = hasHeaderRow;
        _delimiter = delimiter;
    }

    /// <summary>
    /// Registers a column-level validator using a lambda/function.
    /// The validator receives both the property value and the full row for context.
    /// </summary>
    public TsvImporter<T> ForColumn<TProp>(Expression<Func<T, TProp>> selector,
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
    /// Reads the TSV file and returns a readonly collection of DTOs.
    /// </summary>
    public ReadOnlyCollection<T> Import()
    {
        var lines = File.ReadAllLines(_filePath);
        _importedRows = [];

        if (lines.Length == 0)
            return _importedRows.Select(r => r.Item).ToList().AsReadOnly();

        TsvColumnMapping<T> mapping;
        int dataStart;

        if (_hasHeaderRow)
        {
            var headers = TsvParser.ParseLine(lines[0], _delimiter);
            mapping = TsvColumnMapping<T>.Build(headers);
            dataStart = 1;
        }
        else
        {
            mapping = TsvColumnMapping<T>.BuildByNumber();
            dataStart = 0;
        }

        for (var i = dataStart; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line) && !line.Contains(_delimiter))
                continue;

            var fields = TsvParser.ParseLine(line, _delimiter);
            var dto = new T();

            foreach (var pm in mapping.Mappings)
            {
                if (pm.ColumnIndex < fields.Length)
                {
                    var rawValue = fields[pm.ColumnIndex];
                    var value = ConvertValue(rawValue, pm.Property.PropertyType);
                    pm.Property.SetValue(dto, value);
                }
            }

            var rowNumber = i - dataStart + 1; // 1-based from data start
            _importedRows.Add((dto, rowNumber));
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

    private static object? ConvertValue(string rawValue, Type targetType)
    {
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (string.IsNullOrEmpty(rawValue))
        {
            if (underlying == typeof(string))
                return "";
            if (targetType != underlying) // nullable value type
                return null;
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }

        if (underlying == typeof(string))
            return rawValue;

        if (underlying == typeof(int))
            return int.Parse(rawValue);

        if (underlying == typeof(long))
            return long.Parse(rawValue);

        if (underlying == typeof(double))
            return double.Parse(rawValue);

        if (underlying == typeof(decimal))
            return decimal.Parse(rawValue);

        if (underlying == typeof(DateTime))
            return DateTime.Parse(rawValue);

        if (underlying == typeof(bool))
            return bool.Parse(rawValue);

        // Fallback
        return Convert.ChangeType(rawValue, underlying);
    }
}
