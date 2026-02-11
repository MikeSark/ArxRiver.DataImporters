using System.Linq.Expressions;

namespace ArxRiver.DataImporters.Tsv.Importing;

/// <summary>
/// Fluent builder for constructing a configured <see cref="TsvImporter{T}"/>.
/// </summary>
public sealed class TsvImporterBuilder<T> where T : class, new()
{
    private string? _filePath;
    private bool _hasHeaderRow = true;
    private char _delimiter = '\t';
    private readonly List<Action<TsvImporter<T>>> _columnRules = [];

    public static TsvImporterBuilder<T> Create() => new();

    /// <summary>Required. Path to the TSV file.</summary>
    public TsvImporterBuilder<T> FromFile(string filePath)
    {
        _filePath = filePath;
        return this;
    }

    /// <summary>
    /// Optional. Whether the first row contains column headers. Default is true.
    /// </summary>
    public TsvImporterBuilder<T> WithHeaderRow(bool hasHeaderRow)
    {
        _hasHeaderRow = hasHeaderRow;
        return this;
    }

    /// <summary>
    /// Optional. Field delimiter character. Default is tab.
    /// </summary>
    public TsvImporterBuilder<T> WithDelimiter(char delimiter)
    {
        _delimiter = delimiter;
        return this;
    }

    /// <summary>
    /// Registers a column-level validation rule.
    /// The validator receives both the property value and the full row for context.
    /// </summary>
    public TsvImporterBuilder<T> ForColumn<TProp>(
        Expression<Func<T, TProp>> selector,
        Func<TProp, T, bool> validator,
        string? errorMessage = null)
    {
        _columnRules.Add(importer => importer.ForColumn(selector, validator, errorMessage));
        return this;
    }

    /// <summary>
    /// Builds and returns a fully configured <see cref="TsvImporter{T}"/>.
    /// </summary>
    public TsvImporter<T> Build()
    {
        if (string.IsNullOrWhiteSpace(_filePath))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(_filePath));

        var importer = new TsvImporter<T>(_filePath!, _hasHeaderRow, _delimiter);

        foreach (var rule in _columnRules)
            rule(importer);

        return importer;
    }
}
