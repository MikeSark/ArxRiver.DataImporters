using ArxRiver.DataImporters.Tsv.Importing;

namespace ArxRiver.DataImporters.Tsv.Tests;

public class TsvFluentValidationTests
{
    private static void WithTempTsv(string tsv, Action<string> test)
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.tsv");
        File.WriteAllText(path, tsv);
        try { test(path); }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void ForColumn_SingleValueValidator_CatchesInvalidValue()
    {
        var tsv = "name\tage\tscore\nAlice\t30\t90.0\n\t25\t80.0";

        WithTempTsv(tsv, path =>
        {
            var importer = new TsvImporter<TsvSimpleDto>(path)
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
        var tsv = "name\tage\tscore\nJunior\t15\t90.0";

        WithTempTsv(tsv, path =>
        {
            var importer = new TsvImporter<TsvSimpleDto>(path)
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
        var tsv = "name\tage\tscore\nX\t-1\t50.0";

        WithTempTsv(tsv, path =>
        {
            var importer = new TsvImporter<TsvSimpleDto>(path)
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
        WithTempTsv("name\tage\tscore", path =>
        {
            var importer = new TsvImporter<TsvSimpleDto>(path);
            var result = importer.ForColumn(x => x.Name, (n, _) => true);

            Assert.Same(importer, result);
        });
    }

    [Fact]
    public void ForColumn_PassingValidData_NoErrors()
    {
        var tsv = "name\tage\tscore\nAlice\t30\t90.0";

        WithTempTsv(tsv, path =>
        {
            var importer = new TsvImporter<TsvSimpleDto>(path)
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
        var tsv = "name\tage\tscore\n\t30\t90.0";

        WithTempTsv(tsv, path =>
        {
            var importer = new TsvImporter<TsvSimpleDto>(path)
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
        var tsv = "name\tage\tscore\nAlice\t-5\t90.0";

        WithTempTsv(tsv, path =>
        {
            var importer = new TsvImporter<TsvSimpleDto>(path)
                .ForColumn(x => x.Age, IsPositiveAge, "Age must be positive");

            importer.Import();
            var errors = importer.Validate();

            Assert.Single(errors);
        });
    }

    private static bool IsPositiveAge(int age, TsvSimpleDto row) => age > 0;
}
