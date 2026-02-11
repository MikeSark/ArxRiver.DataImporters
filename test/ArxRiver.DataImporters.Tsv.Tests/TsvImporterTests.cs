using ArxRiver.DataImporters.Tsv.Importing;

namespace ArxRiver.DataImporters.Tsv.Tests;

public class TsvImporterTests
{
    private static string WriteTempTsv(string tsv)
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.tsv");
        File.WriteAllText(path, tsv);
        return path;
    }

    private static void WithTempTsv(string tsv, Action<string> test)
    {
        var path = WriteTempTsv(tsv);
        try { test(path); }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void Import_WithHeaders_ReturnsCorrectData()
    {
        var tsv = "name\tage\tscore\nAlice\t30\t95.5\nBob\t25\t88.0";

        WithTempTsv(tsv, path =>
        {
            var importer = new TsvImporter<TsvSimpleDto>(path);
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
        var tsv = "Alice\t30\t95.5\nBob\t25\t88.0";

        WithTempTsv(tsv, path =>
        {
            var importer = new TsvImporter<TsvNumberDto>(path, hasHeaderRow: false);
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
        var tsv = "Title\tYear\nInception\t2010";

        WithTempTsv(tsv, path =>
        {
            var importer = new TsvImporter<TsvConventionDto>(path);
            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal("Inception", rows[0].Title);
            Assert.Equal(2010, rows[0].Year);
        });
    }

    [Fact]
    public void Import_CaseInsensitiveHeaderMatching()
    {
        var tsv = "NAME\tAGE\tSCORE\nTest\t20\t50.0";

        WithTempTsv(tsv, path =>
        {
            var importer = new TsvImporter<TsvSimpleDto>(path);
            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal("Test", rows[0].Name);
            Assert.Equal(20, rows[0].Age);
        });
    }

    [Fact]
    public void Import_TypeConversion_HandlesAllTypes()
    {
        var tsv = "date\tactive\tamount\tcount\n2023-06-15\ttrue\t123.45\t999999";

        WithTempTsv(tsv, path =>
        {
            var importer = new TsvImporter<TsvTypesDto>(path);
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
        var tsv = "value\tlabel\n\t";

        WithTempTsv(tsv, path =>
        {
            var importer = new TsvImporter<TsvNullableDto>(path);
            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Null(rows[0].Value);
            Assert.Equal("", rows[0].Label);
        });
    }

    [Fact]
    public void Import_EmptyFile_ReturnsEmptyCollection()
    {
        WithTempTsv("", path =>
        {
            var importer = new TsvImporter<TsvSimpleDto>(path);
            var rows = importer.Import();

            Assert.Empty(rows);
        });
    }

    [Fact]
    public void Import_HeaderOnly_ReturnsEmptyCollection()
    {
        WithTempTsv("name\tage\tscore", path =>
        {
            var importer = new TsvImporter<TsvSimpleDto>(path);
            var rows = importer.Import();

            Assert.Empty(rows);
        });
    }

    [Fact]
    public void Import_ReturnsReadOnlyCollection()
    {
        var tsv = "name\tage\tscore\nAlice\t30\t90.0";

        WithTempTsv(tsv, path =>
        {
            var importer = new TsvImporter<TsvSimpleDto>(path);
            var rows = importer.Import();

            Assert.IsType<System.Collections.ObjectModel.ReadOnlyCollection<TsvSimpleDto>>(rows);
        });
    }

    [Fact]
    public void Import_QuotedFields_ParsedCorrectly()
    {
        var tsv = "name\tage\tscore\n\"Alice\tJr.\"\t30\t95.5\n\"Bob \"\"The Builder\"\"\"\t25\t88.0";

        WithTempTsv(tsv, path =>
        {
            var importer = new TsvImporter<TsvSimpleDto>(path);
            var rows = importer.Import();

            Assert.Equal(2, rows.Count);
            Assert.Equal("Alice\tJr.", rows[0].Name);
            Assert.Equal("Bob \"The Builder\"", rows[1].Name);
        });
    }

    [Fact]
    public void Import_CustomDelimiter_SemicolonSeparated()
    {
        var tsv = "name;age;score\nAlice;30;95.5";

        WithTempTsv(tsv, path =>
        {
            var importer = new TsvImporter<TsvSimpleDto>(path, delimiter: ';');
            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal("Alice", rows[0].Name);
            Assert.Equal(30, rows[0].Age);
            Assert.Equal(95.5, rows[0].Score);
        });
    }

    [Fact]
    public void Validate_BeforeImport_Throws()
    {
        WithTempTsv("name\tage\tscore", path =>
        {
            var importer = new TsvImporter<TsvSimpleDto>(path);
            Assert.Throws<InvalidOperationException>(() => importer.Validate());
        });
    }

    [Fact]
    public void GetValidRows_BeforeValidate_Throws()
    {
        var tsv = "name\tage\tscore\nAlice\t30\t90.0";

        WithTempTsv(tsv, path =>
        {
            var importer = new TsvImporter<TsvSimpleDto>(path);
            importer.Import();
            Assert.Throws<InvalidOperationException>(() => importer.GetValidRows());
        });
    }

    [Fact]
    public void GetValidRows_And_GetInvalidRows_PartitionCorrectly()
    {
        var tsv = "name\tscore\nValid\t50\n\t150";

        WithTempTsv(tsv, path =>
        {
            var importer = new TsvImporter<TsvValidatedDto>(path);
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
        var tsv = "name\tscore\nAlice\t50\n\t150";

        WithTempTsv(tsv, path =>
        {
            var importer = new TsvImporter<TsvValidatedDto>(path);
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
        var tsv = "min\tmax\n1\t10\n20\t5";

        WithTempTsv(tsv, path =>
        {
            var importer = new TsvImporter<TsvCrossFieldDto>(path);
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
        var tsv = "name\tage\tscore\nAlice\t30\t90.0";

        WithTempTsv(tsv, path =>
        {
            var importer = new TsvImporter<TsvSimpleDto>(path);
            importer.Import();
            Assert.Throws<InvalidOperationException>(() => importer.CreateReportGenerator());
        });
    }

    [Fact]
    public void Import_SkipsBlankLines()
    {
        var tsv = "name\tage\tscore\nAlice\t30\t95.5\n\nBob\t25\t88.0\n  \n";

        WithTempTsv(tsv, path =>
        {
            var importer = new TsvImporter<TsvSimpleDto>(path);
            var rows = importer.Import();

            Assert.Equal(2, rows.Count);
            Assert.Equal("Alice", rows[0].Name);
            Assert.Equal("Bob", rows[1].Name);
        });
    }
}
