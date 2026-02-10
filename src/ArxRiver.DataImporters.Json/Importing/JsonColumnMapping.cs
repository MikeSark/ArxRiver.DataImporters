using System.Reflection;
using ArxRiver.DataImporters.Json.Attributes;

namespace ArxRiver.DataImporters.Json.Importing;

/// <summary>
/// Resolves the mapping between DTO properties and JSON property names.
/// </summary>
internal sealed class JsonColumnMapping<T> where T : class
{
    public required IReadOnlyList<PropertyMapping> Mappings { get; init; }

    public sealed record PropertyMapping(PropertyInfo Property, string JsonPropertyName);

    public static JsonColumnMapping<T> Build()
    {
        var mappings = (
                from prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                let attr = prop.GetCustomAttribute<JsonColumnAttribute>()
                let jsonName = attr?.Name ?? prop.Name
                select new PropertyMapping(prop, jsonName))
            .ToList();

        return new JsonColumnMapping<T> { Mappings = mappings };
    }
}