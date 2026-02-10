using System.Reflection;
using ArxRiver.DataImporters.Excel.Attributes;

namespace ArxRiver.DataImporters.Excel.Tests;

public class ExcelColumnAttributeTests
{
    [Fact]
    public void ExcelColumnAttribute_ByName_SetsNameProperty()
    {
        var attr = new ExcelColumnAttribute("First Name");

        Assert.Equal("First Name", attr.Name);
        Assert.Equal(-1, attr.Number);
    }

    [Fact]
    public void ExcelColumnAttribute_ByNumber_SetsNumberProperty()
    {
        var attr = new ExcelColumnAttribute(3);

        Assert.Null(attr.Name);
        Assert.Equal(3, attr.Number);
    }

    [Fact]
    public void ExcelColumnAttribute_AllowsSinglePerProperty()
    {
        var usage = typeof(ExcelColumnAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        Assert.NotNull(usage);
        Assert.False(usage!.AllowMultiple);
        Assert.Equal(AttributeTargets.Property, usage.ValidOn);
    }
}
