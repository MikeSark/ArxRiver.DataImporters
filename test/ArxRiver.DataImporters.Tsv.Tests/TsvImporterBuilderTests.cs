using ArxRiver.DataImporters.Tsv.Importing;

namespace ArxRiver.DataImporters.Tsv.Tests;

public class TsvImporterBuilderTests
{
    private static void WithTempTsv(string tsv, Action<string> test)
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.tsv");
        File.WriteAllText(path, tsv);
        try { test(path); }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void Build_WithoutFilePath_ThrowsArgumentException()
    {
        var builder = TsvImporterBuilder<TsvSimpleDto>.Create();

        Assert.Throws<ArgumentException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithEmptyFilePath_ThrowsArgumentException()
    {
        var builder = TsvImporterBuilder<TsvSimpleDto>.Create()
            .FromFile("");

        Assert.Throws<ArgumentException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithFilePath_ReturnsImporter()
    {
        var tsv = "name\tage\tscore\nAlice\t30\t95.5";

        WithTempTsv(tsv, path =>
        {
            var importer = TsvImporterBuilder<TsvSimpleDto>.Create()
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
        var tsv = "Alice\t30\t95.5";

        WithTempTsv(tsv, path =>
        {
            var importer = TsvImporterBuilder<TsvNumberDto>.Create()
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
        var tsv = "name;age;score\nAlice;30;95.5";

        WithTempTsv(tsv, path =>
        {
            var importer = TsvImporterBuilder<TsvSimpleDto>.Create()
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
        var tsv = "name\tage\tscore\nAlice\t30\t95.0\nBob\t-5\t80.0";

        WithTempTsv(tsv, path =>
        {
            var importer = TsvImporterBuilder<TsvSimpleDto>.Create()
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
        var tsv = "name\tage\tscore\n\t-1\t50.0";

        WithTempTsv(tsv, path =>
        {
            var importer = TsvImporterBuilder<TsvSimpleDto>.Create()
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
        var tsv = "name\tage\tscore\nAlice\t30\t95.5";

        WithTempTsv(tsv, path =>
        {
            var importer = TsvImporterBuilder<TsvSimpleDto>.Create()
                .FromFile(path)
                .WithHeaderRow(true)
                .WithDelimiter('\t')
                .ForColumn(x => x.Age, (age, _) => age > 0, "Age must be positive")
                .Build();

            var rows = importer.Import();
            Assert.Single(rows);
        });
    }
}
