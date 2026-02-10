namespace ArxRiver.DataImporters.Json.Attributes;

/// <summary>
/// Maps a DTO property to a JSON property name.
/// If not specified, the property name is used as-is (case-insensitive match).
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class JsonColumnAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}