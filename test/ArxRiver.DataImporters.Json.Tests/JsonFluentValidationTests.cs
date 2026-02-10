using ArxRiver.DataImporters.Json.Importing;

namespace ArxRiver.DataImporters.Json.Tests;

public class JsonFluentValidationTests
{
    private static void WithTempJson(string json, Action<string> test)
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        try { test(path); }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void ForColumn_SingleValueValidator_CatchesInvalidValue()
    {
        var json = """
        [
            { "name": "Alice", "age": 30, "score": 90.0 },
            { "name": "", "age": 25, "score": 80.0 }
        ]
        """;

        WithTempJson(json, path =>
        {
            var importer = new JsonImporter<JsonSimpleDto>(path)
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
        var json = """
        [
            { "name": "Junior", "age": 15, "score": 90.0 }
        ]
        """;

        WithTempJson(json, path =>
        {
            var importer = new JsonImporter<JsonSimpleDto>(path)
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
        var json = """
        [
            { "name": "X", "age": -1, "score": 50.0 }
        ]
        """;

        WithTempJson(json, path =>
        {
            var importer = new JsonImporter<JsonSimpleDto>(path)
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
        WithTempJson("[]", path =>
        {
            var importer = new JsonImporter<JsonSimpleDto>(path);
            var result = importer.ForColumn(x => x.Name, (n, _) => true);

            Assert.Same(importer, result);
        });
    }

    [Fact]
    public void ForColumn_PassingValidData_NoErrors()
    {
        var json = """
        [
            { "name": "Alice", "age": 30, "score": 90.0 }
        ]
        """;

        WithTempJson(json, path =>
        {
            var importer = new JsonImporter<JsonSimpleDto>(path)
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
        var json = """
        [
            { "name": "", "age": 30, "score": 90.0 }
        ]
        """;

        WithTempJson(json, path =>
        {
            var importer = new JsonImporter<JsonSimpleDto>(path)
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
        var json = """
        [
            { "name": "Alice", "age": -5, "score": 90.0 }
        ]
        """;

        WithTempJson(json, path =>
        {
            var importer = new JsonImporter<JsonSimpleDto>(path)
                .ForColumn(x => x.Age, IsPositiveAge, "Age must be positive");

            importer.Import();
            var errors = importer.Validate();

            Assert.Single(errors);
        });
    }

    private static bool IsPositiveAge(int age, JsonSimpleDto row) => age > 0;
}
