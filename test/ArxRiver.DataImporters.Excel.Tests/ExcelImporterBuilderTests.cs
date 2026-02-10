using ArxRiver.DataImporters.Excel.Importing;

namespace ArxRiver.DataImporters.Excel.Tests;

public class ExcelImporterBuilderTests
{
    [Fact]
    public void Build_WithoutFilePath_ThrowsArgumentException()
    {
        var builder = ExcelImporterBuilder<SimpleDto>.Create();

        Assert.Throws<ArgumentException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithEmptyFilePath_ThrowsArgumentException()
    {
        var builder = ExcelImporterBuilder<SimpleDto>.Create()
            .FromFile("");

        Assert.Throws<ArgumentException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithFilePath_ReturnsImporter()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "Age";
            ws.Cell(1, 3).Value = "Score";
            ws.Cell(2, 1).Value = "Alice";
            ws.Cell(2, 2).Value = 30;
            ws.Cell(2, 3).Value = 95.5;
        }, path =>
        {
            var importer = ExcelImporterBuilder<SimpleDto>.Create()
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
    public void Build_WithoutWorksheet_UsesFirstWorksheet()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.xlsx");
        try
        {
            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("First");
                ws.Cell(1, 1).Value = "Name";
                ws.Cell(1, 2).Value = "Age";
                ws.Cell(1, 3).Value = "Score";
                ws.Cell(2, 1).Value = "Alice";
                ws.Cell(2, 2).Value = 30;
                ws.Cell(2, 3).Value = 90.0;
                workbook.SaveAs(path);
            }

            var importer = ExcelImporterBuilder<SimpleDto>.Create()
                .FromFile(path)
                .Build();

            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal("Alice", rows[0].Name);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Build_WithWorksheet_UsesSpecifiedWorksheet()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.xlsx");
        try
        {
            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var ws1 = workbook.Worksheets.Add("Ignore");
                ws1.Cell(1, 1).Value = "Name";
                ws1.Cell(2, 1).Value = "Wrong";

                var ws2 = workbook.Worksheets.Add("Target");
                ws2.Cell(1, 1).Value = "Name";
                ws2.Cell(1, 2).Value = "Age";
                ws2.Cell(1, 3).Value = "Score";
                ws2.Cell(2, 1).Value = "Alice";
                ws2.Cell(2, 2).Value = 30;
                ws2.Cell(2, 3).Value = 95.0;
                workbook.SaveAs(path);
            }

            var importer = ExcelImporterBuilder<SimpleDto>.Create()
                .FromFile(path)
                .WithWorksheet("Target")
                .Build();

            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal("Alice", rows[0].Name);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Build_WithDataStartRow_SkipsRows()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "Age";
            ws.Cell(1, 3).Value = "Score";
            ws.Cell(2, 1).Value = "Skipped";
            ws.Cell(2, 2).Value = 0;
            ws.Cell(2, 3).Value = 0.0;
            ws.Cell(3, 1).Value = "Alice";
            ws.Cell(3, 2).Value = 30;
            ws.Cell(3, 3).Value = 95.0;
        }, path =>
        {
            var importer = ExcelImporterBuilder<SimpleDto>.Create()
                .FromFile(path)
                .WithDataStartRow(3)
                .Build();

            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal("Alice", rows[0].Name);
        });
    }

    [Fact]
    public void Build_WithForColumn_AppliesValidationRules()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "Age";
            ws.Cell(1, 3).Value = "Score";
            ws.Cell(2, 1).Value = "Alice";
            ws.Cell(2, 2).Value = 30;
            ws.Cell(2, 3).Value = 95.0;
            ws.Cell(3, 1).Value = "Bob";
            ws.Cell(3, 2).Value = -5;
            ws.Cell(3, 3).Value = 80.0;
        }, path =>
        {
            var importer = ExcelImporterBuilder<SimpleDto>.Create()
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
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "Age";
            ws.Cell(1, 3).Value = "Score";
            ws.Cell(2, 1).Value = "";
            ws.Cell(2, 2).Value = -1;
            ws.Cell(2, 3).Value = 50.0;
        }, path =>
        {
            var importer = ExcelImporterBuilder<SimpleDto>.Create()
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
            // Verify the entire chain compiles and works
            var importer = ExcelImporterBuilder<SimpleDto>.Create()
                .FromFile(path)
                .WithWorksheet("Sheet1")
                .WithDataStartRow(2)
                .ForColumn(x => x.Age, (age, _) => age > 0, "Positive age")
                .Build();

            var rows = importer.Import();
            Assert.Single(rows);
        });
    }
}
