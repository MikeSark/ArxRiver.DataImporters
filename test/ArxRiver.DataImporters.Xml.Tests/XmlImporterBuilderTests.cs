using ArxRiver.DataImporters.Xml.Importing;

namespace ArxRiver.DataImporters.Xml.Tests;

public class XmlImporterBuilderTests
{
    private static void WithTempXml(string xml, Action<string> test)
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.xml");
        File.WriteAllText(path, xml);
        try { test(path); }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void Build_WithoutFilePath_ThrowsArgumentException()
    {
        var builder = XmlImporterBuilder<XmlSimpleDto>.Create()
            .WithRowElementName("Person");

        Assert.Throws<ArgumentException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithEmptyFilePath_ThrowsArgumentException()
    {
        var builder = XmlImporterBuilder<XmlSimpleDto>.Create()
            .FromFile("")
            .WithRowElementName("Person");

        Assert.Throws<ArgumentException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithoutRowElementName_ThrowsArgumentException()
    {
        var builder = XmlImporterBuilder<XmlSimpleDto>.Create()
            .FromFile("test.xml");

        Assert.Throws<ArgumentException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithEmptyRowElementName_ThrowsArgumentException()
    {
        var builder = XmlImporterBuilder<XmlSimpleDto>.Create()
            .FromFile("test.xml")
            .WithRowElementName("");

        Assert.Throws<ArgumentException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithRequiredParams_ReturnsImporter()
    {
        var xml = """
            <People>
              <Person><name>Alice</name><age>30</age><score>95.5</score></Person>
            </People>
            """;

        WithTempXml(xml, path =>
        {
            var importer = XmlImporterBuilder<XmlSimpleDto>.Create()
                .FromFile(path)
                .WithRowElementName("Person")
                .Build();

            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal("Alice", rows[0].Name);
            Assert.Equal(30, rows[0].Age);
            Assert.Equal(95.5, rows[0].Score);
        });
    }

    [Fact]
    public void Build_WithForColumn_AppliesValidationRules()
    {
        var xml = """
            <People>
              <Person><name>Alice</name><age>30</age><score>95.0</score></Person>
              <Person><name>Bob</name><age>-5</age><score>80.0</score></Person>
            </People>
            """;

        WithTempXml(xml, path =>
        {
            var importer = XmlImporterBuilder<XmlSimpleDto>.Create()
                .FromFile(path)
                .WithRowElementName("Person")
                .ForColumn(x => x.Age, (age, _) => age >= 0, "Age must be non-negative")
                .Build();

            var rows = importer.Import();
            var errors = importer.Validate();

            Assert.Equal(2, rows.Count);
            Assert.Single(errors);
            Assert.Equal("Age must be non-negative", errors[0].ErrorMessage);
        });
    }

    [Fact]
    public void Build_WithMultipleForColumns_AppliesAllRules()
    {
        var xml = """
            <People>
              <Person><name></name><age>-1</age><score>50.0</score></Person>
            </People>
            """;

        WithTempXml(xml, path =>
        {
            var importer = XmlImporterBuilder<XmlSimpleDto>.Create()
                .FromFile(path)
                .WithRowElementName("Person")
                .ForColumn(x => x.Name, (name, _) => name.Length > 0, "Name is required")
                .ForColumn(x => x.Age, (age, _) => age >= 0, "Age must be non-negative")
                .Build();

            importer.Import();
            var errors = importer.Validate();

            Assert.Equal(2, errors.Count);
            Assert.Contains(errors, e => e.ErrorMessage == "Name is required");
            Assert.Contains(errors, e => e.ErrorMessage == "Age must be non-negative");
        });
    }

    [Fact]
    public void Build_FluentChaining_AllMethodsReturnBuilder()
    {
        var xml = """
            <People>
              <Person><name>Alice</name><age>30</age><score>95.5</score></Person>
            </People>
            """;

        WithTempXml(xml, path =>
        {
            var importer = XmlImporterBuilder<XmlSimpleDto>.Create()
                .FromFile(path)
                .WithRowElementName("Person")
                .ForColumn(x => x.Age, (age, _) => age > 0, "Age must be positive")
                .Build();

            var rows = importer.Import();
            Assert.Single(rows);
        });
    }
}
