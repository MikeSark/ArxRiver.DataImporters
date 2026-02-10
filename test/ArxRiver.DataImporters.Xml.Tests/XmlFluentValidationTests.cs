using ArxRiver.DataImporters.Xml.Importing;

namespace ArxRiver.DataImporters.Xml.Tests;

public class XmlFluentValidationTests
{
    private static void WithTempXml(string xml, Action<string> test)
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.xml");
        File.WriteAllText(path, xml);
        try { test(path); }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void ForColumn_SingleValueValidator_CatchesInvalidValue()
    {
        var xml = """
            <People>
              <Person><name>Alice</name><age>30</age><score>90.0</score></Person>
              <Person><name></name><age>25</age><score>80.0</score></Person>
            </People>
            """;

        WithTempXml(xml, path =>
        {
            var importer = new XmlImporter<XmlSimpleDto>(path, "Person")
                .ForColumn(x => x.Name, (name, _) => name.Length > 0, "Name is required");

            importer.Import();
            var errors = importer.Validate();

            Assert.Single(errors);
            Assert.Equal("ForColumn:Name", errors[0].RuleName);
            Assert.Equal("Name is required", errors[0].ErrorMessage);
        });
    }

    [Fact]
    public void ForColumn_WithRowContext_ValidatesAcrossFields()
    {
        var xml = """
            <People>
              <Person><name>Junior</name><age>15</age><score>90.0</score></Person>
            </People>
            """;

        WithTempXml(xml, path =>
        {
            var importer = new XmlImporter<XmlSimpleDto>(path, "Person")
                .ForColumn(x => x.Score, (score, row) => row.Age >= 18 || score <= 50,
                    "Minors cannot have scores above 50");

            importer.Import();
            var errors = importer.Validate();

            Assert.Single(errors);
            Assert.Contains("Minors cannot have scores above 50", errors[0].ErrorMessage);
        });
    }

    [Fact]
    public void ForColumn_MultipleValidators_AllExecute()
    {
        var xml = """
            <People>
              <Person><name>X</name><age>-1</age><score>50.0</score></Person>
            </People>
            """;

        WithTempXml(xml, path =>
        {
            var importer = new XmlImporter<XmlSimpleDto>(path, "Person")
                .ForColumn(x => x.Name, (name, _) => name.Length >= 2, "Name too short")
                .ForColumn(x => x.Age, (age, _) => age >= 0, "Age cannot be negative");

            importer.Import();
            var errors = importer.Validate();

            Assert.Equal(2, errors.Count);
            Assert.Contains(errors, e => e.ErrorMessage == "Name too short");
            Assert.Contains(errors, e => e.ErrorMessage == "Age cannot be negative");
        });
    }

    [Fact]
    public void ForColumn_ReturnsSameImporterForChaining()
    {
        var xml = "<People></People>";

        WithTempXml(xml, path =>
        {
            var importer = new XmlImporter<XmlSimpleDto>(path, "Person");
            var result = importer.ForColumn(x => x.Name, (n, _) => true);

            Assert.Same(importer, result);
        });
    }

    [Fact]
    public void ForColumn_PassingValidData_NoErrors()
    {
        var xml = """
            <People>
              <Person><name>Alice</name><age>30</age><score>90.0</score></Person>
            </People>
            """;

        WithTempXml(xml, path =>
        {
            var importer = new XmlImporter<XmlSimpleDto>(path, "Person")
                .ForColumn(x => x.Name, (name, _) => name.Length > 0, "Name required")
                .ForColumn(x => x.Age, (age, _) => age > 0, "Age positive");

            importer.Import();
            var errors = importer.Validate();

            Assert.Empty(errors);
        });
    }

    [Fact]
    public void ForColumn_DefaultErrorMessage()
    {
        var xml = """
            <People>
              <Person><name></name><age>30</age><score>90.0</score></Person>
            </People>
            """;

        WithTempXml(xml, path =>
        {
            var importer = new XmlImporter<XmlSimpleDto>(path, "Person")
                .ForColumn(x => x.Name, (name, _) => name.Length > 0);

            importer.Import();
            var errors = importer.Validate();

            Assert.Single(errors);
            Assert.Contains("Name", errors[0].ErrorMessage);
        });
    }

    [Fact]
    public void ForColumn_MethodGroupReference_Works()
    {
        var xml = """
            <People>
              <Person><name>Alice</name><age>-5</age><score>90.0</score></Person>
            </People>
            """;

        WithTempXml(xml, path =>
        {
            var importer = new XmlImporter<XmlSimpleDto>(path, "Person")
                .ForColumn(x => x.Age, IsPositiveAge, "Age must be positive");

            importer.Import();
            var errors = importer.Validate();

            Assert.Single(errors);
        });
    }

    private static bool IsPositiveAge(int age, XmlSimpleDto row) => age > 0;
}
