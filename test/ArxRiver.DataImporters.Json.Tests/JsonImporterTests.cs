using ArxRiver.DataImporters.Json.Importing;

namespace ArxRiver.DataImporters.Json.Tests;

public class JsonImporterTests
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
    public void Import_RootArray_ReturnsCorrectData()
    {
        var json = """
        [
            { "name": "Alice", "age": 30, "score": 95.5 },
            { "name": "Bob", "age": 25, "score": 88.0 }
        ]
        """;

        WithTempJson(json, path =>
        {
            var importer = new JsonImporter<JsonSimpleDto>(path);
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
    public void Import_WithArrayPath_NavigatesToNestedArray()
    {
        var json = """
        {
            "meta": { "total": 2 },
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
            var importer = new JsonImporter<JsonNestedDto>(path, arrayPath: "data.items");
            var rows = importer.Import();

            Assert.Equal(2, rows.Count);
            Assert.Equal(1, rows[0].Id);
            Assert.Equal("first", rows[0].Value);
            Assert.Equal(2, rows[1].Id);
            Assert.Equal("second", rows[1].Value);
        });
    }

    [Fact]
    public void Import_ConventionMapping_MatchesPropertyNames()
    {
        var json = """
        [
            { "Title": "Inception", "Year": 2010 }
        ]
        """;

        WithTempJson(json, path =>
        {
            var importer = new JsonImporter<JsonConventionDto>(path);
            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal("Inception", rows[0].Title);
            Assert.Equal(2010, rows[0].Year);
        });
    }

    [Fact]
    public void Import_CaseInsensitivePropertyMatching()
    {
        var json = """
        [
            { "NAME": "Test", "AGE": 20, "SCORE": 50.0 }
        ]
        """;

        WithTempJson(json, path =>
        {
            var importer = new JsonImporter<JsonSimpleDto>(path);
            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal("Test", rows[0].Name);
            Assert.Equal(20, rows[0].Age);
        });
    }

    [Fact]
    public void Import_TypeConversion_HandlesAllTypes()
    {
        var json = """
        [
            { "date": "2023-06-15", "active": true, "amount": 123.45, "count": 999999 }
        ]
        """;

        WithTempJson(json, path =>
        {
            var importer = new JsonImporter<JsonTypesDto>(path);
            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal(new DateTime(2023, 6, 15), rows[0].Date);
            Assert.True(rows[0].Active);
            Assert.Equal(123.45m, rows[0].Amount);
            Assert.Equal(999999L, rows[0].Count);
        });
    }

    [Fact]
    public void Import_NullValues_HandledCorrectly()
    {
        var json = """
        [
            { "value": null, "label": null }
        ]
        """;

        WithTempJson(json, path =>
        {
            var importer = new JsonImporter<JsonNullableDto>(path);
            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Null(rows[0].Value);
            Assert.Equal("", rows[0].Label);
        });
    }

    [Fact]
    public void Import_MissingProperties_UsesDefaults()
    {
        var json = """
        [
            { "name": "Alice" }
        ]
        """;

        WithTempJson(json, path =>
        {
            var importer = new JsonImporter<JsonSimpleDto>(path);
            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal("Alice", rows[0].Name);
            Assert.Equal(0, rows[0].Age);
            Assert.Equal(0.0, rows[0].Score);
        });
    }

    [Fact]
    public void Import_EmptyArray_ReturnsEmptyCollection()
    {
        WithTempJson("[]", path =>
        {
            var importer = new JsonImporter<JsonSimpleDto>(path);
            var rows = importer.Import();

            Assert.Empty(rows);
        });
    }

    [Fact]
    public void Import_ReturnsReadOnlyCollection()
    {
        var json = """[{ "name": "Alice", "age": 30, "score": 90.0 }]""";

        WithTempJson(json, path =>
        {
            var importer = new JsonImporter<JsonSimpleDto>(path);
            var rows = importer.Import();

            Assert.IsType<System.Collections.ObjectModel.ReadOnlyCollection<JsonSimpleDto>>(rows);
        });
    }

    [Fact]
    public void Import_RootNotArray_WithoutArrayPath_Throws()
    {
        var json = """{ "name": "Alice" }""";

        WithTempJson(json, path =>
        {
            var importer = new JsonImporter<JsonSimpleDto>(path);
            Assert.Throws<InvalidOperationException>(() => importer.Import());
        });
    }

    [Fact]
    public void Import_InvalidArrayPath_Throws()
    {
        var json = """{ "data": { "items": [] } }""";

        WithTempJson(json, path =>
        {
            var importer = new JsonImporter<JsonNestedDto>(path, arrayPath: "data.nonexistent");
            Assert.Throws<InvalidOperationException>(() => importer.Import());
        });
    }

    [Fact]
    public void Validate_BeforeImport_Throws()
    {
        var json = "[]";

        WithTempJson(json, path =>
        {
            var importer = new JsonImporter<JsonSimpleDto>(path);
            Assert.Throws<InvalidOperationException>(() => importer.Validate());
        });
    }

    [Fact]
    public void GetValidRows_BeforeValidate_Throws()
    {
        var json = """[{ "name": "Alice", "age": 30, "score": 90.0 }]""";

        WithTempJson(json, path =>
        {
            var importer = new JsonImporter<JsonSimpleDto>(path);
            importer.Import();
            Assert.Throws<InvalidOperationException>(() => importer.GetValidRows());
        });
    }

    [Fact]
    public void GetValidRows_And_GetInvalidRows_PartitionCorrectly()
    {
        var json = """
        [
            { "name": "Valid", "score": 50 },
            { "name": "", "score": 150 }
        ]
        """;

        WithTempJson(json, path =>
        {
            var importer = new JsonImporter<JsonValidatedDto>(path);
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
        var json = """
        [
            { "name": "Alice", "score": 50 },
            { "name": "", "score": 150 }
        ]
        """;

        WithTempJson(json, path =>
        {
            var importer = new JsonImporter<JsonValidatedDto>(path);
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
        var json = """
        [
            { "min": 1, "max": 10 },
            { "min": 20, "max": 5 }
        ]
        """;

        WithTempJson(json, path =>
        {
            var importer = new JsonImporter<JsonCrossFieldDto>(path);
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
        var json = """[{ "name": "Alice", "age": 30, "score": 90.0 }]""";

        WithTempJson(json, path =>
        {
            var importer = new JsonImporter<JsonSimpleDto>(path);
            importer.Import();
            Assert.Throws<InvalidOperationException>(() => importer.CreateReportGenerator());
        });
    }
}
