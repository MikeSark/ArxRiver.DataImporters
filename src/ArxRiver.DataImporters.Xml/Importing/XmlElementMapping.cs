using System.Reflection;
using ArxRiver.DataImporters.Xml.Attributes;

namespace ArxRiver.DataImporters.Xml.Importing;

internal sealed class XmlElementMapping<T> where T : class
{
    public required IReadOnlyList<PropertyMapping> Mappings { get; init; }

    public sealed record PropertyMapping(PropertyInfo Property, string ElementOrAttributeName, bool IsAttribute);

    public static XmlElementMapping<T> Build()
    {
        var mappings = new List<PropertyMapping>();

        foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var attr = prop.GetCustomAttribute<XmlColumnAttribute>();

            if (attr is not null)
            {
                mappings.Add(new PropertyMapping(prop, attr.Name, attr.IsAttribute));
            }
            else
            {
                // Convention: use property name as child element name
                mappings.Add(new PropertyMapping(prop, prop.Name, false));
            }
        }

        return new XmlElementMapping<T> { Mappings = mappings };
    }
}
