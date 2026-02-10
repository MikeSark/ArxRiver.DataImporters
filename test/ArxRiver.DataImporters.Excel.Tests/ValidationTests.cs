using ArxRiver.DataImporters.Excel.Importing;

namespace ArxRiver.DataImporters.Excel.Tests;

public class ValidationTests
{
    [Fact]
    public void Validate_NoValidationAttributes_ReturnsZeroErrors()
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
            var importer = new Importer<SimpleDto>(path);
            importer.Import();
            var errors = importer.Validate();

            Assert.Empty(errors);
        });
    }

    [Fact]
    public void Validate_InlineValidation_CatchesInvalidScore()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "Score";
            ws.Cell(2, 1).Value = "Alice";
            ws.Cell(2, 2).Value = 150; // Out of 0-100 range
        }, path =>
        {
            var importer = new Importer<ValidatedDto>(path);
            importer.Import();
            var errors = importer.Validate();

            Assert.Contains(errors, e => e.RuleName == "ScoreRange");
        });
    }

    [Fact]
    public void Validate_InlineValidation_PassesValidData()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "Score";
            ws.Cell(2, 1).Value = "Alice";
            ws.Cell(2, 2).Value = 85;
        }, path =>
        {
            var importer = new Importer<ValidatedDto>(path);
            importer.Import();
            var errors = importer.Validate();

            Assert.Empty(errors);
        });
    }

    [Fact]
    public void Validate_ClassLevelInlineValidation_CatchesEmptyName()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "Score";
            ws.Cell(2, 1).Value = "";  // Empty name
            ws.Cell(2, 2).Value = 50;
        }, path =>
        {
            var importer = new Importer<ValidatedDto>(path);
            importer.Import();
            var errors = importer.Validate();

            Assert.Contains(errors, e => e.RuleName == "NameRequired");
        });
    }

    [Fact]
    public void Validate_CrossFieldValidator_CatchesMinGreaterThanMax()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Min";
            ws.Cell(1, 2).Value = "Max";
            ws.Cell(2, 1).Value = 100;
            ws.Cell(2, 2).Value = 50; // Min > Max
        }, path =>
        {
            var importer = new Importer<CrossFieldDto>(path);
            importer.Import();
            var errors = importer.Validate();

            Assert.Single(errors);
            Assert.Equal("CrossFieldCheck", errors[0].RuleName);
            Assert.Contains("Min must be less than or equal to Max", errors[0].ErrorMessage);
        });
    }

    [Fact]
    public void Validate_CrossFieldValidator_PassesValidData()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Min";
            ws.Cell(1, 2).Value = "Max";
            ws.Cell(2, 1).Value = 10;
            ws.Cell(2, 2).Value = 50;
        }, path =>
        {
            var importer = new Importer<CrossFieldDto>(path);
            importer.Import();
            var errors = importer.Validate();

            Assert.Empty(errors);
        });
    }

    [Fact]
    public void Validate_GetValidAndInvalidRows_CorrectPartition()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Min";
            ws.Cell(1, 2).Value = "Max";
            ws.Cell(2, 1).Value = 10;
            ws.Cell(2, 2).Value = 50;  // valid
            ws.Cell(3, 1).Value = 100;
            ws.Cell(3, 2).Value = 50;  // invalid: Min > Max
            ws.Cell(4, 1).Value = 5;
            ws.Cell(4, 2).Value = 5;   // valid: Min == Max
        }, path =>
        {
            var importer = new Importer<CrossFieldDto>(path);
            importer.Import();
            importer.Validate();

            var valid = importer.GetValidRows();
            var invalid = importer.GetInvalidRows();

            Assert.Equal(2, valid.Count);
            Assert.Single(invalid);
            Assert.Equal(100, invalid[0].Min);
        });
    }

    [Fact]
    public void Validate_MultipleErrorsOnSameRow()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "Score";
            ws.Cell(2, 1).Value = "";    // Fails NameRequired
            ws.Cell(2, 2).Value = 150;   // Fails ScoreRange
        }, path =>
        {
            var importer = new Importer<ValidatedDto>(path);
            importer.Import();
            var errors = importer.Validate();

            Assert.Equal(2, errors.Count);
            Assert.Contains(errors, e => e.RuleName == "NameRequired");
            Assert.Contains(errors, e => e.RuleName == "ScoreRange");
        });
    }

    [Fact]
    public void Validate_ErrorContainsCorrectRowNumber()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Min";
            ws.Cell(1, 2).Value = "Max";
            ws.Cell(2, 1).Value = 1;
            ws.Cell(2, 2).Value = 10; // valid (row 2)
            ws.Cell(3, 1).Value = 99;
            ws.Cell(3, 2).Value = 1;  // invalid (row 3)
        }, path =>
        {
            var importer = new Importer<CrossFieldDto>(path);
            importer.Import();
            var errors = importer.Validate();

            Assert.Single(errors);
            Assert.Equal(3, errors[0].RowNumber); // Excel row 3
        });
    }

    [Fact]
    public void Validate_ReturnsReadOnlyCollection()
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
            var importer = new Importer<SimpleDto>(path);
            importer.Import();
            var errors = importer.Validate();

            Assert.IsType<System.Collections.ObjectModel.ReadOnlyCollection<DataImporters.Core.Validation.ValidationResult>>(errors);
        });
    }
}
