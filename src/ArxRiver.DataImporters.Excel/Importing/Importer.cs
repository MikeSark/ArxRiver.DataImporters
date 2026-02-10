using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using ArxRiver.DataImporters.Core.Reporting;
using ArxRiver.DataImporters.Core.Validation;
using ClosedXML.Excel;

namespace ArxRiver.DataImporters.Excel.Importing;

/// <summary>
/// Generic Excel importer that reads rows into typed DTOs, validates them, and generates reports.
/// </summary>
public sealed class Importer<T> where T : class, new()
{
    private readonly string _filePath;
    private readonly string _worksheetName;
    private readonly int _dataStartRow;

    private List<(T Item, int RowNumber)>? _importedRows;
    private ReadOnlyCollection<ValidationResult>? _validationResults;

    private readonly List<Func<T, int, IEnumerable<ValidationResult>>> _fluentRules = [];

    public Importer(string filePath, string worksheetName = "", int dataStartRow = 2)
    {
        _filePath = filePath;
        _worksheetName = worksheetName;
        _dataStartRow = dataStartRow;
    }

    /// <summary>
    /// Registers a column-level validator using a lambda/function.
    /// The validator receives both the property value and the full row for context.
    /// </summary>
    public Importer<T> ForColumn<TProp>(
        Expression<Func<T, TProp>> selector,
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

        return this; // fluent chaining
    }

    /// <summary>
    /// Reads the Excel file and returns a readonly collection of DTOs.
    /// </summary>
    public ReadOnlyCollection<T> Import()
    {
        using var workbook = new XLWorkbook(_filePath);
        var worksheet = string.IsNullOrEmpty(_worksheetName)
            ? workbook.Worksheets.First()
            : workbook.Worksheet(_worksheetName);

        var mapping = ColumnMapping<T>.Build(worksheet);
        _importedRows = [];

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? _dataStartRow - 1;

        for (var rowNum = _dataStartRow; rowNum <= lastRow; rowNum++)
        {
            var row = worksheet.Row(rowNum);
            if (row.IsEmpty()) continue;

            var dto = new T();

            foreach (var pm in mapping.Mappings)
            {
                var cell = row.Cell(pm.ColumnIndex);
                var value = ConvertCellValue(cell, pm.Property.PropertyType);
                pm.Property.SetValue(dto, value);
            }

            _importedRows.Add((dto, rowNum));
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

    private static object? ConvertCellValue(IXLCell cell, Type targetType)
    {
        if (cell.IsEmpty())
        {
            if (targetType == typeof(string))
                return "";
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        return underlying switch
        {
            _ when underlying == typeof(string) => cell.GetString(),
            _ when underlying == typeof(int) => (int)cell.GetDouble(),
            _ when underlying == typeof(long) => (long)cell.GetDouble(),
            _ when underlying == typeof(double) => cell.GetDouble(),
            _ when underlying == typeof(decimal) => (decimal)cell.GetDouble(),
            _ when underlying == typeof(DateTime) => cell.GetDateTime(),
            _ when underlying == typeof(bool) => cell.GetBoolean(),
            _ => Convert.ChangeType(cell.GetString(), underlying)
        };
    }
}
