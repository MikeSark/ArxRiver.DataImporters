using ArxRiver.DataImporters.Csv.Importing;

namespace ArxRiver.DataImporters.Csv.Tests;

public class CsvImporterBuilderTests
{
    private static void WithTempCsv(string csv, Action<string> test)
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.csv");
        File.WriteAllText(path, csv);
        try { test(path); }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void Build_WithoutFilePath_ThrowsArgumentException()
    {
        var builder = CsvImporterBuilder<CsvSimpleDto>.Create();

        Assert.Throws<ArgumentException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithEmptyFilePath_ThrowsArgumentException()
    {
        var builder = CsvImporterBuilder<CsvSimpleDto>.Create()
            .FromFile("");

        Assert.Throws<ArgumentException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithFilePath_ReturnsImporter()
    {
        var csv = "name,age,score\nAlice,30,95.5";

        WithTempCsv(csv, path =>
        {
            var importer = CsvImporterBuilder<CsvSimpleDto>.Create()
                .FromFile(path)
                .Build();

            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal("Alice", rows[0].Name);
            Assert.Equal(30, rows[0].Age);
            Assert.Equal(95.5, rows[0].Score);
        });
    }

    [Fact]
    public void Build_WithHeaderRowFalse_UsesColumnNumbers()
    {
        var csv = "Alice,30,95.5";

        WithTempCsv(csv, path =>
        {
            var importer = CsvImporterBuilder<CsvNumberDto>.Create()
                .FromFile(path)
                .WithHeaderRow(false)
                .Build();

            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal("Alice", rows[0].Name);
            Assert.Equal(30, rows[0].Age);
        });
    }

    [Fact]
    public void Build_WithCustomDelimiter_ParsesCorrectly()
    {
        var csv = "name;age;score\nAlice;30;95.5";

        WithTempCsv(csv, path =>
        {
            var importer = CsvImporterBuilder<CsvSimpleDto>.Create()
                .FromFile(path)
                .WithDelimiter(';')
                .Build();

            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal("Alice", rows[0].Name);
        });
    }

    [Fact]
    public void Build_WithForColumn_AppliesValidationRules()
    {
        var csv = "name,age,score\nAlice,30,95.0\nBob,-5,80.0";

        WithTempCsv(csv, path =>
        {
            var importer = CsvImporterBuilder<CsvSimpleDto>.Create()
                .FromFile(path)
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
        var csv = "name,age,score\n,-1,50.0";

        WithTempCsv(csv, path =>
        {
            var importer = CsvImporterBuilder<CsvSimpleDto>.Create()
                .FromFile(path)
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
        var csv = "name,age,score\nAlice,30,95.5";

        WithTempCsv(csv, path =>
        {
            var importer = CsvImporterBuilder<CsvSimpleDto>.Create()
                .FromFile(path)
                .WithHeaderRow(true)
                .WithDelimiter(',')
                .ForColumn(x => x.Age, (age, _) => age > 0, "Age must be positive")
                .Build();

            var rows = importer.Import();
            Assert.Single(rows);
        });
    }
}
