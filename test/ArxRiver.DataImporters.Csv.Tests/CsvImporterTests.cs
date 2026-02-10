using ArxRiver.DataImporters.Csv.Importing;

namespace ArxRiver.DataImporters.Csv.Tests;

public class CsvImporterTests
{
    private static string WriteTempCsv(string csv)
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.csv");
        File.WriteAllText(path, csv);
        return path;
    }

    private static void WithTempCsv(string csv, Action<string> test)
    {
        var path = WriteTempCsv(csv);
        try { test(path); }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void Import_WithHeaders_ReturnsCorrectData()
    {
        var csv = "name,age,score\nAlice,30,95.5\nBob,25,88.0";

        WithTempCsv(csv, path =>
        {
            var importer = new CsvImporter<CsvSimpleDto>(path);
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
    public void Import_WithoutHeaders_MapsByColumnNumber()
    {
        var csv = "Alice,30,95.5\nBob,25,88.0";

        WithTempCsv(csv, path =>
        {
            var importer = new CsvImporter<CsvNumberDto>(path, hasHeaderRow: false);
            var rows = importer.Import();

            Assert.Equal(2, rows.Count);
            Assert.Equal("Alice", rows[0].Name);
            Assert.Equal(30, rows[0].Age);
            Assert.Equal(95.5, rows[0].Score);
        });
    }

    [Fact]
    public void Import_ConventionMapping_MatchesPropertyNames()
    {
        var csv = "Title,Year\nInception,2010";

        WithTempCsv(csv, path =>
        {
            var importer = new CsvImporter<CsvConventionDto>(path);
            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal("Inception", rows[0].Title);
            Assert.Equal(2010, rows[0].Year);
        });
    }

    [Fact]
    public void Import_CaseInsensitiveHeaderMatching()
    {
        var csv = "NAME,AGE,SCORE\nTest,20,50.0";

        WithTempCsv(csv, path =>
        {
            var importer = new CsvImporter<CsvSimpleDto>(path);
            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal("Test", rows[0].Name);
            Assert.Equal(20, rows[0].Age);
        });
    }

    [Fact]
    public void Import_TypeConversion_HandlesAllTypes()
    {
        var csv = "date,active,amount,count\n2023-06-15,true,123.45,999999";

        WithTempCsv(csv, path =>
        {
            var importer = new CsvImporter<CsvTypesDto>(path);
            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal(new DateTime(2023, 6, 15), rows[0].Date);
            Assert.True(rows[0].Active);
            Assert.Equal(123.45m, rows[0].Amount);
            Assert.Equal(999999L, rows[0].Count);
        });
    }

    [Fact]
    public void Import_EmptyValues_HandledCorrectly()
    {
        var csv = "value,label\n,";

        WithTempCsv(csv, path =>
        {
            var importer = new CsvImporter<CsvNullableDto>(path);
            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Null(rows[0].Value);
            Assert.Equal("", rows[0].Label);
        });
    }

    [Fact]
    public void Import_EmptyFile_ReturnsEmptyCollection()
    {
        WithTempCsv("", path =>
        {
            var importer = new CsvImporter<CsvSimpleDto>(path);
            var rows = importer.Import();

            Assert.Empty(rows);
        });
    }

    [Fact]
    public void Import_HeaderOnly_ReturnsEmptyCollection()
    {
        WithTempCsv("name,age,score", path =>
        {
            var importer = new CsvImporter<CsvSimpleDto>(path);
            var rows = importer.Import();

            Assert.Empty(rows);
        });
    }

    [Fact]
    public void Import_ReturnsReadOnlyCollection()
    {
        var csv = "name,age,score\nAlice,30,90.0";

        WithTempCsv(csv, path =>
        {
            var importer = new CsvImporter<CsvSimpleDto>(path);
            var rows = importer.Import();

            Assert.IsType<System.Collections.ObjectModel.ReadOnlyCollection<CsvSimpleDto>>(rows);
        });
    }

    [Fact]
    public void Import_QuotedFields_ParsedCorrectly()
    {
        var csv = "name,age,score\n\"Alice, Jr.\",30,95.5\n\"Bob \"\"The Builder\"\"\",25,88.0";

        WithTempCsv(csv, path =>
        {
            var importer = new CsvImporter<CsvSimpleDto>(path);
            var rows = importer.Import();

            Assert.Equal(2, rows.Count);
            Assert.Equal("Alice, Jr.", rows[0].Name);
            Assert.Equal("Bob \"The Builder\"", rows[1].Name);
        });
    }

    [Fact]
    public void Import_CustomDelimiter_SemicolonSeparated()
    {
        var csv = "name;age;score\nAlice;30;95.5";

        WithTempCsv(csv, path =>
        {
            var importer = new CsvImporter<CsvSimpleDto>(path, delimiter: ';');
            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal("Alice", rows[0].Name);
            Assert.Equal(30, rows[0].Age);
            Assert.Equal(95.5, rows[0].Score);
        });
    }

    [Fact]
    public void Import_CustomDelimiter_TabSeparated()
    {
        var csv = "name\tage\tscore\nAlice\t30\t95.5";

        WithTempCsv(csv, path =>
        {
            var importer = new CsvImporter<CsvSimpleDto>(path, delimiter: '\t');
            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal("Alice", rows[0].Name);
        });
    }

    [Fact]
    public void Validate_BeforeImport_Throws()
    {
        WithTempCsv("name,age,score", path =>
        {
            var importer = new CsvImporter<CsvSimpleDto>(path);
            Assert.Throws<InvalidOperationException>(() => importer.Validate());
        });
    }

    [Fact]
    public void GetValidRows_BeforeValidate_Throws()
    {
        var csv = "name,age,score\nAlice,30,90.0";

        WithTempCsv(csv, path =>
        {
            var importer = new CsvImporter<CsvSimpleDto>(path);
            importer.Import();
            Assert.Throws<InvalidOperationException>(() => importer.GetValidRows());
        });
    }

    [Fact]
    public void GetValidRows_And_GetInvalidRows_PartitionCorrectly()
    {
        var csv = "name,score\nValid,50\n,150";

        WithTempCsv(csv, path =>
        {
            var importer = new CsvImporter<CsvValidatedDto>(path);
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
        var csv = "name,score\nAlice,50\n,150";

        WithTempCsv(csv, path =>
        {
            var importer = new CsvImporter<CsvValidatedDto>(path);
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
        var csv = "min,max\n1,10\n20,5";

        WithTempCsv(csv, path =>
        {
            var importer = new CsvImporter<CsvCrossFieldDto>(path);
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
        var csv = "name,age,score\nAlice,30,90.0";

        WithTempCsv(csv, path =>
        {
            var importer = new CsvImporter<CsvSimpleDto>(path);
            importer.Import();
            Assert.Throws<InvalidOperationException>(() => importer.CreateReportGenerator());
        });
    }

    [Fact]
    public void Import_SkipsBlankLines()
    {
        var csv = "name,age,score\nAlice,30,95.5\n\nBob,25,88.0\n  \n";

        WithTempCsv(csv, path =>
        {
            var importer = new CsvImporter<CsvSimpleDto>(path);
            var rows = importer.Import();

            Assert.Equal(2, rows.Count);
            Assert.Equal("Alice", rows[0].Name);
            Assert.Equal("Bob", rows[1].Name);
        });
    }
}
