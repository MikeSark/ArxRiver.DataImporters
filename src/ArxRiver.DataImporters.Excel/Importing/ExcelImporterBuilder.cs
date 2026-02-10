using System.Linq.Expressions;

namespace ArxRiver.DataImporters.Excel.Importing;

/// <summary>
/// Fluent builder for constructing a configured <see cref="Importer{T}"/>.
/// </summary>
public sealed class ExcelImporterBuilder<T> where T : class, new()
{
    private string? _filePath;
    private string _worksheetName = "";
    private int _dataStartRow = 2;
    private readonly List<Action<Importer<T>>> _columnRules = [];

    public static ExcelImporterBuilder<T> Create() => new();

    /// <summary>Required. Path to the Excel file.</summary>
    public ExcelImporterBuilder<T> FromFile(string filePath)
    {
        _filePath = filePath;
        return this;
    }

    /// <summary>
    /// Optional. Worksheet name to read from.
    /// If omitted, the first worksheet is used (single-sheet assumption).
    /// </summary>
    public ExcelImporterBuilder<T> WithWorksheet(string worksheetName)
    {
        _worksheetName = worksheetName;
        return this;
    }

    /// <summary>Optional. Row number where data starts (default 2, after the header row).</summary>
    public ExcelImporterBuilder<T> WithDataStartRow(int row)
    {
        _dataStartRow = row;
        return this;
    }

    /// <summary>
    /// Registers a column-level validation rule.
    /// The validator receives both the property value and the full row for context.
    /// </summary>
    public ExcelImporterBuilder<T> ForColumn<TProp>(
        Expression<Func<T, TProp>> selector,
        Func<TProp, T, bool> validator,
        string? errorMessage = null)
    {
        _columnRules.Add(importer => importer.ForColumn(selector, validator, errorMessage));
        return this;
    }

    /// <summary>
    /// Builds and returns a fully configured <see cref="Importer{T}"/>.
    /// </summary>
    public Importer<T> Build()
    {
        if (string.IsNullOrWhiteSpace(_filePath))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(_filePath));

        var importer = new Importer<T>(_filePath!, _worksheetName, _dataStartRow);

        foreach (var rule in _columnRules)
            rule(importer);

        return importer;
    }
}
