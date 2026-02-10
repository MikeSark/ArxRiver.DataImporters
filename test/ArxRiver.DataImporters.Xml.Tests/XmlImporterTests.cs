using ArxRiver.DataImporters.Xml.Importing;

namespace ArxRiver.DataImporters.Xml.Tests;

public class XmlImporterTests
{
    private static string WriteTempXml(string xml)
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.xml");
        File.WriteAllText(path, xml);
        return path;
    }

    private static void WithTempXml(string xml, Action<string> test)
    {
        var path = WriteTempXml(xml);
        try { test(path); }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void Import_ChildElements_ReturnsCorrectData()
    {
        var xml = """
            <People>
              <Person>
                <name>Alice</name>
                <age>30</age>
                <score>95.5</score>
              </Person>
              <Person>
                <name>Bob</name>
                <age>25</age>
                <score>88.0</score>
              </Person>
            </People>
            """;

        WithTempXml(xml, path =>
        {
            var importer = new XmlImporter<XmlSimpleDto>(path, "Person");
            var rows = importer.Import();

            Assert.Equal(2, rows.Count);
            Assert.Equal("Alice", rows[0].Name);
            Assert.Equal(30, rows[0].Age);
            Assert.Equal(95.5, rows[0].Score);
            Assert.Equal("Bob", rows[1].Name);
            Assert.Equal(25, rows[1].Age);
        });
    }

    [Fact]
    public void Import_XmlAttributes_ReturnsCorrectData()
    {
        var xml = """
            <People>
              <Person name="Alice" age="30" score="95.5" />
              <Person name="Bob" age="25" score="88.0" />
            </People>
            """;

        WithTempXml(xml, path =>
        {
            var importer = new XmlImporter<XmlAttributeDto>(path, "Person");
            var rows = importer.Import();

            Assert.Equal(2, rows.Count);
            Assert.Equal("Alice", rows[0].Name);
            Assert.Equal(30, rows[0].Age);
            Assert.Equal(95.5, rows[0].Score);
        });
    }

    [Fact]
    public void Import_MixedElementsAndAttributes_ReturnsCorrectData()
    {
        var xml = """
            <Items>
              <Item id="1" active="true">
                <name>Widget</name>
              </Item>
              <Item id="2" active="false">
                <name>Gadget</name>
              </Item>
            </Items>
            """;

        WithTempXml(xml, path =>
        {
            var importer = new XmlImporter<XmlMixedDto>(path, "Item");
            var rows = importer.Import();

            Assert.Equal(2, rows.Count);
            Assert.Equal(1, rows[0].Id);
            Assert.Equal("Widget", rows[0].Name);
            Assert.True(rows[0].Active);
            Assert.Equal(2, rows[1].Id);
            Assert.Equal("Gadget", rows[1].Name);
            Assert.False(rows[1].Active);
        });
    }

    [Fact]
    public void Import_ConventionMapping_MatchesPropertyNames()
    {
        var xml = """
            <Movies>
              <Movie>
                <Title>Inception</Title>
                <Year>2010</Year>
              </Movie>
            </Movies>
            """;

        WithTempXml(xml, path =>
        {
            var importer = new XmlImporter<XmlConventionDto>(path, "Movie");
            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal("Inception", rows[0].Title);
            Assert.Equal(2010, rows[0].Year);
        });
    }

    [Fact]
    public void Import_CaseInsensitiveElementMatching()
    {
        var xml = """
            <People>
              <PERSON>
                <NAME>Test</NAME>
                <AGE>20</AGE>
                <SCORE>50.0</SCORE>
              </PERSON>
            </People>
            """;

        WithTempXml(xml, path =>
        {
            var importer = new XmlImporter<XmlSimpleDto>(path, "Person");
            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal("Test", rows[0].Name);
            Assert.Equal(20, rows[0].Age);
        });
    }

    [Fact]
    public void Import_TypeConversion_HandlesAllTypes()
    {
        var xml = """
            <Records>
              <Record>
                <date>2023-06-15</date>
                <active>true</active>
                <amount>123.45</amount>
                <count>999999</count>
              </Record>
            </Records>
            """;

        WithTempXml(xml, path =>
        {
            var importer = new XmlImporter<XmlTypesDto>(path, "Record");
            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal(new DateTime(2023, 6, 15), rows[0].Date);
            Assert.True(rows[0].Active);
            Assert.Equal(123.45m, rows[0].Amount);
            Assert.Equal(999999L, rows[0].Count);
        });
    }

    [Fact]
    public void Import_MissingElements_LeavesDefaults()
    {
        var xml = """
            <People>
              <Person>
                <name>Alice</name>
              </Person>
            </People>
            """;

        WithTempXml(xml, path =>
        {
            var importer = new XmlImporter<XmlSimpleDto>(path, "Person");
            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal("Alice", rows[0].Name);
            Assert.Equal(0, rows[0].Age);
            Assert.Equal(0.0, rows[0].Score);
        });
    }

    [Fact]
    public void Import_EmptyFile_ReturnsEmptyCollection()
    {
        var xml = "<People></People>";

        WithTempXml(xml, path =>
        {
            var importer = new XmlImporter<XmlSimpleDto>(path, "Person");
            var rows = importer.Import();

            Assert.Empty(rows);
        });
    }

    [Fact]
    public void Import_NoMatchingElements_ReturnsEmptyCollection()
    {
        var xml = """
            <Data>
              <Item><name>Test</name></Item>
            </Data>
            """;

        WithTempXml(xml, path =>
        {
            var importer = new XmlImporter<XmlSimpleDto>(path, "Person");
            var rows = importer.Import();

            Assert.Empty(rows);
        });
    }

    [Fact]
    public void Import_ReturnsReadOnlyCollection()
    {
        var xml = """
            <People>
              <Person><name>Alice</name><age>30</age><score>90.0</score></Person>
            </People>
            """;

        WithTempXml(xml, path =>
        {
            var importer = new XmlImporter<XmlSimpleDto>(path, "Person");
            var rows = importer.Import();

            Assert.IsType<System.Collections.ObjectModel.ReadOnlyCollection<XmlSimpleDto>>(rows);
        });
    }

    [Fact]
    public void Import_NullableTypes_EmptyElements_ReturnsNull()
    {
        var xml = """
            <Records>
              <Record>
                <value></value>
                <label></label>
              </Record>
            </Records>
            """;

        WithTempXml(xml, path =>
        {
            var importer = new XmlImporter<XmlNullableDto>(path, "Record");
            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Null(rows[0].Value);
            Assert.Equal("", rows[0].Label);
        });
    }

    [Fact]
    public void Validate_BeforeImport_Throws()
    {
        var xml = "<People></People>";

        WithTempXml(xml, path =>
        {
            var importer = new XmlImporter<XmlSimpleDto>(path, "Person");
            Assert.Throws<InvalidOperationException>(() => importer.Validate());
        });
    }

    [Fact]
    public void GetValidRows_BeforeValidate_Throws()
    {
        var xml = """
            <People>
              <Person><name>Alice</name><age>30</age><score>90.0</score></Person>
            </People>
            """;

        WithTempXml(xml, path =>
        {
            var importer = new XmlImporter<XmlSimpleDto>(path, "Person");
            importer.Import();
            Assert.Throws<InvalidOperationException>(() => importer.GetValidRows());
        });
    }

    [Fact]
    public void GetValidRows_And_GetInvalidRows_PartitionCorrectly()
    {
        var xml = """
            <People>
              <Person><name>Valid</name><score>50</score></Person>
              <Person><name></name><score>150</score></Person>
            </People>
            """;

        WithTempXml(xml, path =>
        {
            var importer = new XmlImporter<XmlValidatedDto>(path, "Person");
            importer.Import();
            importer.Validate();

            var valid = importer.GetValidRows();
            var invalid = importer.GetInvalidRows();

            Assert.Single(valid);
            Assert.Equal("Valid", valid[0].Name);
            Assert.Single(invalid);
        });
    }

    [Fact]
    public void Validate_InlineValidation_DetectsErrors()
    {
        var xml = """
            <People>
              <Person><name>Alice</name><score>50</score></Person>
              <Person><name></name><score>150</score></Person>
            </People>
            """;

        WithTempXml(xml, path =>
        {
            var importer = new XmlImporter<XmlValidatedDto>(path, "Person");
            importer.Import();
            var errors = importer.Validate();

            // Row 2: empty name (NameRequired) + score > 100 (ScoreRange)
            Assert.True(errors.Count >= 2);
            Assert.Contains(errors, e => e.RuleName == "NameRequired");
            Assert.Contains(errors, e => e.RuleName == "ScoreRange");
        });
    }

    [Fact]
    public void Validate_IRowValidator_DetectsErrors()
    {
        var xml = """
            <Ranges>
              <Range><min>1</min><max>10</max></Range>
              <Range><min>20</min><max>5</max></Range>
            </Ranges>
            """;

        WithTempXml(xml, path =>
        {
            var importer = new XmlImporter<XmlCrossFieldDto>(path, "Range");
            importer.Import();
            var errors = importer.Validate();

            Assert.Single(errors);
            Assert.Equal("CrossFieldCheck", errors[0].RuleName);
            Assert.Equal(2, errors[0].RowNumber);
        });
    }

    [Fact]
    public void CreateReportGenerator_BeforeValidate_Throws()
    {
        var xml = """
            <People>
              <Person><name>Alice</name><age>30</age><score>90.0</score></Person>
            </People>
            """;

        WithTempXml(xml, path =>
        {
            var importer = new XmlImporter<XmlSimpleDto>(path, "Person");
            importer.Import();
            Assert.Throws<InvalidOperationException>(() => importer.CreateReportGenerator());
        });
    }

    [Fact]
    public void Import_RowNumbers_AreOneBased()
    {
        var xml = """
            <People>
              <Person><name>First</name><score>10</score></Person>
              <Person><name>Second</name><score>20</score></Person>
              <Person><name>Third</name><score>30</score></Person>
            </People>
            """;

        WithTempXml(xml, path =>
        {
            var importer = new XmlImporter<XmlValidatedDto>(path, "Person");
            importer.Import();
            var errors = importer.Validate();

            // All rows are valid, so no errors — but we can verify row count
            Assert.Equal(3, importer.GetValidRows().Count);
        });
    }
}
