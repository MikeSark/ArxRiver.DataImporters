using System.Linq.Expressions;

namespace ArxRiver.DataImporters.Json.Importing;

/// <summary>
/// Fluent builder for constructing a configured <see cref="JsonImporter{T}"/>.
/// </summary>
public sealed class JsonImporterBuilder<T> where T : class, new()
{
    private string? _filePath;
    private string? _arrayPath;
    private readonly List<Action<JsonImporter<T>>> _columnRules = [];

    public static JsonImporterBuilder<T> Create() => new();

    /// <summary>Required. Path to the JSON file.</summary>
    public JsonImporterBuilder<T> FromFile(string filePath)
    {
        _filePath = filePath;
        return this;
    }

    /// <summary>
    /// Optional. Dot-separated path to the array inside the JSON (e.g. "data.items").
    /// If omitted, the root element must be an array.
    /// </summary>
    public JsonImporterBuilder<T> WithArrayPath(string arrayPath)
    {
        _arrayPath = arrayPath;
        return this;
    }

    /// <summary>
    /// Registers a column-level validation rule.
    /// The validator receives both the property value and the full row for context.
    /// </summary>
    public JsonImporterBuilder<T> ForColumn<TProp>(
        Expression<Func<T, TProp>> selector,
        Func<TProp, T, bool> validator,
        string? errorMessage = null)
    {
        _columnRules.Add(importer => importer.ForColumn(selector, validator, errorMessage));
        return this;
    }

    /// <summary>
    /// Builds and returns a fully configured <see cref="JsonImporter{T}"/>.
    /// </summary>
    public JsonImporter<T> Build()
    {
        if (string.IsNullOrWhiteSpace(_filePath))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(_filePath));

        var importer = new JsonImporter<T>(_filePath!, _arrayPath);

        foreach (var rule in _columnRules)
            rule(importer);

        return importer;
    }
}