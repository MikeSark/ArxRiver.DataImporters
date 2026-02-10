using ArxRiver.DataImporters.Excel.Importing;

namespace ArxRiver.DataImporters.Excel.Tests;

public class FluentValidationTests
{
    [Fact]
    public void ForColumn_SingleValueValidator_CatchesInvalidValue()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "Age";
            ws.Cell(1, 3).Value = "Score";
            ws.Cell(2, 1).Value = "Alice";
            ws.Cell(2, 2).Value = 30;
            ws.Cell(2, 3).Value = 90.0;
            ws.Cell(3, 1).Value = "";     // empty name
            ws.Cell(3, 2).Value = 25;
            ws.Cell(3, 3).Value = 80.0;
        }, path =>
        {
            var importer = new Importer<SimpleDto>(path)
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
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "Age";
            ws.Cell(1, 3).Value = "Score";
            ws.Cell(2, 1).Value = "Junior";
            ws.Cell(2, 2).Value = 15;
            ws.Cell(2, 3).Value = 90.0;  // Score > 50 but age < 18
        }, path =>
        {
            var importer = new Importer<SimpleDto>(path)
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
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "Age";
            ws.Cell(1, 3).Value = "Score";
            ws.Cell(2, 1).Value = "X";     // too short
            ws.Cell(2, 2).Value = -1;      // negative age
            ws.Cell(2, 3).Value = 50.0;
        }, path =>
        {
            var importer = new Importer<SimpleDto>(path)
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
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "Age";
            ws.Cell(1, 3).Value = "Score";
        }, path =>
        {
            var importer = new Importer<SimpleDto>(path);
            var result = importer.ForColumn(x => x.Name, (n, _) => true);

            Assert.Same(importer, result);
        });
    }

    [Fact]
    public void ForColumn_PassingValidData_NoErrors()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "Age";
            ws.Cell(1, 3).Value = "Score";
            ws.Cell(2, 1).Value = "Alice";
            ws.Cell(2, 2).Value = 30;
            ws.Cell(2, 3).Value = 90.0;
        }, path =>
        {
            var importer = new Importer<SimpleDto>(path)
                .ForColumn(x => x.Name, (name, _) => name.Length > 0, "Name required")
                .ForColumn(x => x.Age, (age, _) => age > 0, "Age positive");

            importer.Import();
            var errors = importer.Validate();

            Assert.Empty(errors);
        });
    }

    [Fact]
    public void ForColumn_MethodGroupReference_Works()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "Age";
            ws.Cell(1, 3).Value = "Score";
            ws.Cell(2, 1).Value = "Alice";
            ws.Cell(2, 2).Value = -5;
            ws.Cell(2, 3).Value = 90.0;
        }, path =>
        {
            var importer = new Importer<SimpleDto>(path)
                .ForColumn(x => x.Age, IsPositiveAge, "Age must be positive");

            importer.Import();
            var errors = importer.Validate();

            Assert.Single(errors);
        });
    }

    private static bool IsPositiveAge(int age, SimpleDto row) => age > 0;

    [Fact]
    public void ForColumn_DefaultErrorMessage()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "Age";
            ws.Cell(1, 3).Value = "Score";
            ws.Cell(2, 1).Value = "";
            ws.Cell(2, 2).Value = 30;
            ws.Cell(2, 3).Value = 90.0;
        }, path =>
        {
            // No custom error message — should use default
            var importer = new Importer<SimpleDto>(path)
                .ForColumn(x => x.Name, (name, _) => name.Length > 0);

            importer.Import();
            var errors = importer.Validate();

            Assert.Single(errors);
            Assert.Contains("Name", errors[0].ErrorMessage);
        });
    }
}
