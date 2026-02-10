using ArxRiver.DataImporters.Json.Importing;

namespace ArxRiver.DataImporters.Json.Tests;

public class JsonImporterBuilderTests
{
    private static string WriteTempJson(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }

    private static void WithTempJson(string json, Action<string> test)
    {
        var path = WriteTempJson(json);
        try { test(path); }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void Build_WithoutFilePath_ThrowsArgumentException()
    {
        var builder = JsonImporterBuilder<JsonSimpleDto>.Create();

        Assert.Throws<ArgumentException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithEmptyFilePath_ThrowsArgumentException()
    {
        var builder = JsonImporterBuilder<JsonSimpleDto>.Create()
            .FromFile("");

        Assert.Throws<ArgumentException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithFilePath_ReturnsImporter()
    {
        var json = """
        [
            { "name": "Alice", "age": 30, "score": 95.5 }
        ]
        """;

        WithTempJson(json, path =>
        {
            var importer = JsonImporterBuilder<JsonSimpleDto>.Create()
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
    public void Build_WithoutArrayPath_UsesRootArray()
    {
        var json = """
        [
            { "name": "Alice", "age": 30, "score": 90.0 }
        ]
        """;

        WithTempJson(json, path =>
        {
            var importer = JsonImporterBuilder<JsonSimpleDto>.Create()
                .FromFile(path)
                .Build();

            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal("Alice", rows[0].Name);
        });
    }

    [Fact]
    public void Build_WithArrayPath_NavigatesToNestedArray()
    {
        var json = """
        {
            "data": {
                "items": [
                    { "id": 1, "value": "first" },
                    { "id": 2, "value": "second" }
                ]
            }
        }
        """;

        WithTempJson(json, path =>
        {
            var importer = JsonImporterBuilder<JsonNestedDto>.Create()
                .FromFile(path)
                .WithArrayPath("data.items")
                .Build();

            var rows = importer.Import();

            Assert.Equal(2, rows.Count);
            Assert.Equal(1, rows[0].Id);
            Assert.Equal("first", rows[0].Value);
        });
    }

    [Fact]
    public void Build_WithForColumn_AppliesValidationRules()
    {
        var json = """
        [
            { "name": "Alice", "age": 30, "score": 95.0 },
            { "name": "Bob", "age": -5, "score": 80.0 }
        ]
        """;

        WithTempJson(json, path =>
        {
            var importer = JsonImporterBuilder<JsonSimpleDto>.Create()
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
        var json = """
        [
            { "name": "", "age": -1, "score": 50.0 }
        ]
        """;

        WithTempJson(json, path =>
        {
            var importer = JsonImporterBuilder<JsonSimpleDto>.Create()
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
        var json = """
        {
            "data": {
                "items": [
                    { "id": 1, "value": "test" }
                ]
            }
        }
        """;

        WithTempJson(json, path =>
        {
            var importer = JsonImporterBuilder<JsonNestedDto>.Create()
                .FromFile(path)
                .WithArrayPath("data.items")
                .ForColumn(x => x.Id, (id, _) => id > 0, "Id must be positive")
                .Build();

            var rows = importer.Import();
            Assert.Single(rows);
        });
    }
}
