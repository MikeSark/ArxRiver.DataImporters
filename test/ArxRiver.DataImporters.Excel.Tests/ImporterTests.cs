using ArxRiver.DataImporters.Excel.Importing;

namespace ArxRiver.DataImporters.Excel.Tests;

public class ImporterTests
{
    [Fact]
    public void Import_WithHeaderNameMapping_ReturnsCorrectData()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "Age";
            ws.Cell(1, 3).Value = "Score";
            ws.Cell(2, 1).Value = "Alice";
            ws.Cell(2, 2).Value = 30;
            ws.Cell(2, 3).Value = 95.5;
            ws.Cell(3, 1).Value = "Bob";
            ws.Cell(3, 2).Value = 25;
            ws.Cell(3, 3).Value = 88.0;
        }, path =>
        {
            var importer = new Importer<SimpleDto>(path);
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
    public void Import_WithColumnNumberMapping_ReturnsCorrectData()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Ignored Header";
            ws.Cell(1, 2).Value = "Also Ignored";
            ws.Cell(1, 3).Value = "Whatever";
            ws.Cell(2, 1).Value = "New York";
            ws.Cell(2, 2).Value = 8_300_000;
            ws.Cell(2, 3).Value = 302.6;
        }, path =>
        {
            var importer = new Importer<NumberMappedDto>(path);
            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal("New York", rows[0].City);
            Assert.Equal(8_300_000, rows[0].Population);
            Assert.Equal(302.6m, rows[0].Area);
        });
    }

    [Fact]
    public void Import_WithConventionMapping_MatchesPropertyNames()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Title";
            ws.Cell(1, 2).Value = "Year";
            ws.Cell(2, 1).Value = "Inception";
            ws.Cell(2, 2).Value = 2010;
        }, path =>
        {
            var importer = new Importer<ConventionDto>(path);
            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal("Inception", rows[0].Title);
            Assert.Equal(2010, rows[0].Year);
        });
    }

    [Fact]
    public void Import_CaseInsensitiveHeaderMatching()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "NAME";
            ws.Cell(1, 2).Value = "age";
            ws.Cell(1, 3).Value = "SCORE";
            ws.Cell(2, 1).Value = "Test";
            ws.Cell(2, 2).Value = 20;
            ws.Cell(2, 3).Value = 50.0;
        }, path =>
        {
            var importer = new Importer<SimpleDto>(path);
            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal("Test", rows[0].Name);
        });
    }

    [Fact]
    public void Import_SkipsEmptyRows()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "Age";
            ws.Cell(1, 3).Value = "Score";
            ws.Cell(2, 1).Value = "Alice";
            ws.Cell(2, 2).Value = 30;
            ws.Cell(2, 3).Value = 90.0;
            // Row 3 is empty
            ws.Cell(4, 1).Value = "Bob";
            ws.Cell(4, 2).Value = 25;
            ws.Cell(4, 3).Value = 80.0;
        }, path =>
        {
            var importer = new Importer<SimpleDto>(path);
            var rows = importer.Import();

            Assert.Equal(2, rows.Count);
        });
    }

    [Fact]
    public void Import_CustomDataStartRow()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "Age";
            ws.Cell(1, 3).Value = "Score";
            ws.Cell(2, 1).Value = "SkippedRow";
            ws.Cell(2, 2).Value = 0;
            ws.Cell(2, 3).Value = 0.0;
            ws.Cell(3, 1).Value = "Alice";
            ws.Cell(3, 2).Value = 30;
            ws.Cell(3, 3).Value = 95.0;
        }, path =>
        {
            var importer = new Importer<SimpleDto>(path, dataStartRow: 3);
            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal("Alice", rows[0].Name);
        });
    }

    [Fact]
    public void Import_ReturnsReadOnlyCollection()
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
            var rows = importer.Import();

            Assert.IsType<System.Collections.ObjectModel.ReadOnlyCollection<SimpleDto>>(rows);
        });
    }

    [Fact]
    public void Import_EmptyWorksheet_ReturnsEmptyCollection()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "Age";
            ws.Cell(1, 3).Value = "Score";
            // No data rows
        }, path =>
        {
            var importer = new Importer<SimpleDto>(path);
            var rows = importer.Import();

            Assert.Empty(rows);
        });
    }

    [Fact]
    public void Import_MissingNamedColumn_ThrowsInvalidOperationException()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Wrong";
            ws.Cell(1, 2).Value = "Headers";
            ws.Cell(2, 1).Value = "data";
            ws.Cell(2, 2).Value = 1;
        }, path =>
        {
            var importer = new Importer<SimpleDto>(path);

            Assert.Throws<InvalidOperationException>(() => importer.Import());
        });
    }

    [Fact]
    public void Validate_BeforeImport_ThrowsInvalidOperationException()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "Age";
            ws.Cell(1, 3).Value = "Score";
        }, path =>
        {
            var importer = new Importer<SimpleDto>(path);

            Assert.Throws<InvalidOperationException>(() => importer.Validate());
        });
    }

    [Fact]
    public void GetValidRows_BeforeValidate_ThrowsInvalidOperationException()
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

            Assert.Throws<InvalidOperationException>(() => importer.GetValidRows());
        });
    }

    [Fact]
    public void CreateReportGenerator_BeforeValidate_ThrowsInvalidOperationException()
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

            Assert.Throws<InvalidOperationException>(() => importer.CreateReportGenerator());
        });
    }

    [Fact]
    public void Import_SpecificWorksheetName()
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

            var importer = new Importer<SimpleDto>(path, worksheetName: "Target");
            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal("Alice", rows[0].Name);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
