namespace ArxRiver.DataImporters.Xml.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class XmlColumnAttribute : Attribute
{
    public string Name { get; }
    public bool IsAttribute { get; set; }

    public XmlColumnAttribute(string name)
    {
        Name = name;
    }
}
