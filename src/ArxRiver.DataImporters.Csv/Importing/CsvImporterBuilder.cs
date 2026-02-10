using System.Linq.Expressions;

namespace ArxRiver.DataImporters.Csv.Importing;

/// <summary>
/// Fluent builder for constructing a configured <see cref="CsvImporter{T}"/>.
/// </summary>
public sealed class CsvImporterBuilder<T> where T : class, new()
{
    private string? _filePath;
    private bool _hasHeaderRow = true;
    private char _delimiter = ',';
    private readonly List<Action<CsvImporter<T>>> _columnRules = [];

    public static CsvImporterBuilder<T> Create() => new();

    /// <summary>Required. Path to the CSV file.</summary>
    public CsvImporterBuilder<T> FromFile(string filePath)
    {
        _filePath = filePath;
        return this;
    }

    /// <summary>
    /// Optional. Whether the first row contains column headers. Default is true.
    /// </summary>
    public CsvImporterBuilder<T> WithHeaderRow(bool hasHeaderRow)
    {
        _hasHeaderRow = hasHeaderRow;
        return this;
    }

    /// <summary>
    /// Optional. Field delimiter character. Default is comma.
    /// </summary>
    public CsvImporterBuilder<T> WithDelimiter(char delimiter)
    {
        _delimiter = delimiter;
        return this;
    }

    /// <summary>
    /// Registers a column-level validation rule.
    /// The validator receives both the property value and the full row for context.
    /// </summary>
    public CsvImporterBuilder<T> ForColumn<TProp>(
        Expression<Func<T, TProp>> selector,
        Func<TProp, T, bool> validator,
        string? errorMessage = null)
    {
        _columnRules.Add(importer => importer.ForColumn(selector, validator, errorMessage));
        return this;
    }

    /// <summary>
    /// Builds and returns a fully configured <see cref="CsvImporter{T}"/>.
    /// </summary>
    public CsvImporter<T> Build()
    {
        if (string.IsNullOrWhiteSpace(_filePath))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(_filePath));

        var importer = new CsvImporter<T>(_filePath!, _hasHeaderRow, _delimiter);

        foreach (var rule in _columnRules)
            rule(importer);

        return importer;
    }
}
