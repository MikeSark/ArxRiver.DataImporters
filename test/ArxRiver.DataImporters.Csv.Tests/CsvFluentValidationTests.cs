using ArxRiver.DataImporters.Csv.Importing;

namespace ArxRiver.DataImporters.Csv.Tests;

public class CsvFluentValidationTests
{
    private static void WithTempCsv(string csv, Action<string> test)
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.csv");
        File.WriteAllText(path, csv);
        try { test(path); }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void ForColumn_SingleValueValidator_CatchesInvalidValue()
    {
        var csv = "name,age,score\nAlice,30,90.0\n,25,80.0";

        WithTempCsv(csv, path =>
        {
            var importer = new CsvImporter<CsvSimpleDto>(path)
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
        var csv = "name,age,score\nJunior,15,90.0";

        WithTempCsv(csv, path =>
        {
            var importer = new CsvImporter<CsvSimpleDto>(path)
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
        var csv = "name,age,score\nX,-1,50.0";

        WithTempCsv(csv, path =>
        {
            var importer = new CsvImporter<CsvSimpleDto>(path)
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
        WithTempCsv("name,age,score", path =>
        {
            var importer = new CsvImporter<CsvSimpleDto>(path);
            var result = importer.ForColumn(x => x.Name, (n, _) => true);

            Assert.Same(importer, result);
        });
    }

    [Fact]
    public void ForColumn_PassingValidData_NoErrors()
    {
        var csv = "name,age,score\nAlice,30,90.0";

        WithTempCsv(csv, path =>
        {
            var importer = new CsvImporter<CsvSimpleDto>(path)
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
        var csv = "name,age,score\n,30,90.0";

        WithTempCsv(csv, path =>
        {
            var importer = new CsvImporter<CsvSimpleDto>(path)
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
        var csv = "name,age,score\nAlice,-5,90.0";

        WithTempCsv(csv, path =>
        {
            var importer = new CsvImporter<CsvSimpleDto>(path)
                .ForColumn(x => x.Age, IsPositiveAge, "Age must be positive");

            importer.Import();
            var errors = importer.Validate();

            Assert.Single(errors);
        });
    }

    private static bool IsPositiveAge(int age, CsvSimpleDto row) => age > 0;
}
