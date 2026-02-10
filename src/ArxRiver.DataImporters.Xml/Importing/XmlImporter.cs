using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;
using ArxRiver.DataImporters.Core.Reporting;
using ArxRiver.DataImporters.Core.Validation;

namespace ArxRiver.DataImporters.Xml.Importing;

public sealed class XmlImporter<T> where T : class, new()
{
    private readonly string _filePath;
    private readonly string _rowElementName;

    private List<(T Item, int RowNumber)>? _importedRows;
    private ReadOnlyCollection<ValidationResult>? _validationResults;

    private readonly List<Func<T, int, IEnumerable<ValidationResult>>> _fluentRules = [];

    public XmlImporter(string filePath, string rowElementName)
    {
        _filePath = filePath;
        _rowElementName = rowElementName;
    }

    public XmlImporter<T> ForColumn<TProp>(Expression<Func<T, TProp>> selector,
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

    public ReadOnlyCollection<T> Import()
    {
        var doc = XDocument.Load(_filePath);
        _importedRows = [];

        var mapping = XmlElementMapping<T>.Build();

        var elements = doc.Descendants()
            .Where(e => string.Equals(e.Name.LocalName, _rowElementName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var rowNumber = 0;
        foreach (var element in elements)
        {
            rowNumber++;
            var dto = new T();

            foreach (var pm in mapping.Mappings)
            {
                string? rawValue;

                if (pm.IsAttribute)
                {
                    rawValue = element.Attributes()
                        .FirstOrDefault(a => string.Equals(a.Name.LocalName, pm.ElementOrAttributeName, StringComparison.OrdinalIgnoreCase))
                        ?.Value;
                }
                else
                {
                    rawValue = element.Elements()
                        .FirstOrDefault(e => string.Equals(e.Name.LocalName, pm.ElementOrAttributeName, StringComparison.OrdinalIgnoreCase))
                        ?.Value;
                }

                if (rawValue is not null)
                {
                    var value = ConvertValue(rawValue, pm.Property.PropertyType);
                    pm.Property.SetValue(dto, value);
                }
            }

            _importedRows.Add((dto, rowNumber));
        }

        return _importedRows.Select(r => r.Item).ToList().AsReadOnly();
    }

    public ReadOnlyCollection<ValidationResult> Validate()
    {
        if (_importedRows is null)
            throw new InvalidOperationException("Call Import() before Validate().");

        var pipeline = new ValidationPipeline<T>(_fluentRules);
        _validationResults = pipeline.Validate(_importedRows);
        return _validationResults;
    }

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
