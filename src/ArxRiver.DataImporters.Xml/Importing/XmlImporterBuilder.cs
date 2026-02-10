using System.Linq.Expressions;

namespace ArxRiver.DataImporters.Xml.Importing;

public sealed class XmlImporterBuilder<T> where T : class, new()
{
    private string? _filePath;
    private string? _rowElementName;
    private readonly List<Action<XmlImporter<T>>> _columnRules = [];

    public static XmlImporterBuilder<T> Create() => new();

    public XmlImporterBuilder<T> FromFile(string filePath)
    {
        _filePath = filePath;
        return this;
    }

    public XmlImporterBuilder<T> WithRowElementName(string rowElementName)
    {
        _rowElementName = rowElementName;
        return this;
    }

    public XmlImporterBuilder<T> ForColumn<TProp>(
        Expression<Func<T, TProp>> selector,
        Func<TProp, T, bool> validator,
        string? errorMessage = null)
    {
        _columnRules.Add(importer => importer.ForColumn(selector, validator, errorMessage));
        return this;
    }

    public XmlImporter<T> Build()
    {
        if (string.IsNullOrWhiteSpace(_filePath))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(_filePath));

        if (string.IsNullOrWhiteSpace(_rowElementName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(_rowElementName));

        var importer = new XmlImporter<T>(_filePath!, _rowElementName!);

        foreach (var rule in _columnRules)
            rule(importer);

        return importer;
    }
}
