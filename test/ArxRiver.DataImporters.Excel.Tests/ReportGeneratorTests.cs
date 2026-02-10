using System.Text.Json;
using ArxRiver.DataImporters.Core.Reporting;
using ArxRiver.DataImporters.Excel.Importing;

namespace ArxRiver.DataImporters.Excel.Tests;

public class ReportGeneratorTests
{
    [Fact]
    public void Generate_JsonReport_ContainsSummary()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Min";
            ws.Cell(1, 2).Value = "Max";
            ws.Cell(2, 1).Value = 10;
            ws.Cell(2, 2).Value = 50;
            ws.Cell(3, 1).Value = 100;
            ws.Cell(3, 2).Value = 5; // invalid
        }, path =>
        {
            var importer = new Importer<CrossFieldDto>(path);
            importer.Import();
            importer.Validate();

            var reportPath = Path.Combine(Path.GetTempPath(), $"report_{Guid.NewGuid():N}.json");
            try
            {
                var report = importer.CreateReportGenerator();
                report.Generate(ReportFormat.Json, ReportDestination.File, outputFilePath: reportPath);

                var json = File.ReadAllText(reportPath);
                var doc = JsonDocument.Parse(json);
                var summary = doc.RootElement.GetProperty("Summary");

                Assert.Equal(2, summary.GetProperty("TotalRows").GetInt32());
                Assert.Equal(1, summary.GetProperty("ValidRows").GetInt32());
                Assert.Equal(1, summary.GetProperty("InvalidRows").GetInt32());
            }
            finally
            {
                if (File.Exists(reportPath)) File.Delete(reportPath);
            }
        });
    }

    [Fact]
    public void Generate_JsonReport_ContainsValidationErrors()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Min";
            ws.Cell(1, 2).Value = "Max";
            ws.Cell(2, 1).Value = 100;
            ws.Cell(2, 2).Value = 5;
        }, path =>
        {
            var importer = new Importer<CrossFieldDto>(path);
            importer.Import();
            importer.Validate();

            var reportPath = Path.Combine(Path.GetTempPath(), $"report_{Guid.NewGuid():N}.json");
            try
            {
                var report = importer.CreateReportGenerator();
                report.Generate(ReportFormat.Json, ReportDestination.File, outputFilePath: reportPath);

                var json = File.ReadAllText(reportPath);
                Assert.Contains("CrossFieldCheck", json);
                Assert.Contains("Min must be less than or equal to Max", json);
            }
            finally
            {
                if (File.Exists(reportPath)) File.Delete(reportPath);
            }
        });
    }

    [Fact]
    public void Generate_JsonReport_FilterValidOnly()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Min";
            ws.Cell(1, 2).Value = "Max";
            ws.Cell(2, 1).Value = 10;
            ws.Cell(2, 2).Value = 50;  // valid
            ws.Cell(3, 1).Value = 100;
            ws.Cell(3, 2).Value = 5;   // invalid
        }, path =>
        {
            var importer = new Importer<CrossFieldDto>(path);
            importer.Import();
            importer.Validate();

            var reportPath = Path.Combine(Path.GetTempPath(), $"report_{Guid.NewGuid():N}.json");
            try
            {
                var report = importer.CreateReportGenerator();
                report.Generate(ReportFormat.Json, ReportDestination.File,
                    ReportGenerator<CrossFieldDto>.RowFilter.Valid, reportPath);

                var json = File.ReadAllText(reportPath);
                var doc = JsonDocument.Parse(json);
                var rows = doc.RootElement.GetProperty("Rows");

                Assert.Equal(1, rows.GetArrayLength());
                var firstRow = rows[0];
                Assert.Equal("Valid", firstRow.GetProperty("Status").GetString());
            }
            finally
            {
                if (File.Exists(reportPath)) File.Delete(reportPath);
            }
        });
    }

    [Fact]
    public void Generate_JsonReport_FilterInvalidOnly()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Min";
            ws.Cell(1, 2).Value = "Max";
            ws.Cell(2, 1).Value = 10;
            ws.Cell(2, 2).Value = 50;
            ws.Cell(3, 1).Value = 100;
            ws.Cell(3, 2).Value = 5;
        }, path =>
        {
            var importer = new Importer<CrossFieldDto>(path);
            importer.Import();
            importer.Validate();

            var reportPath = Path.Combine(Path.GetTempPath(), $"report_{Guid.NewGuid():N}.json");
            try
            {
                var report = importer.CreateReportGenerator();
                report.Generate(ReportFormat.Json, ReportDestination.File,
                    ReportGenerator<CrossFieldDto>.RowFilter.Invalid, reportPath);

                var json = File.ReadAllText(reportPath);
                var doc = JsonDocument.Parse(json);
                var rows = doc.RootElement.GetProperty("Rows");

                Assert.Equal(1, rows.GetArrayLength());
                var firstRow = rows[0];
                Assert.Equal("Invalid", firstRow.GetProperty("Status").GetString());
                Assert.True(firstRow.TryGetProperty("Errors", out _));
            }
            finally
            {
                if (File.Exists(reportPath)) File.Delete(reportPath);
            }
        });
    }

    [Fact]
    public void Generate_HtmlReport_ContainsExpectedStructure()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Min";
            ws.Cell(1, 2).Value = "Max";
            ws.Cell(2, 1).Value = 10;
            ws.Cell(2, 2).Value = 50;
            ws.Cell(3, 1).Value = 100;
            ws.Cell(3, 2).Value = 5;
        }, path =>
        {
            var importer = new Importer<CrossFieldDto>(path);
            importer.Import();
            importer.Validate();

            var reportPath = Path.Combine(Path.GetTempPath(), $"report_{Guid.NewGuid():N}.html");
            try
            {
                var report = importer.CreateReportGenerator();
                report.Generate(ReportFormat.Html, ReportDestination.File, outputFilePath: reportPath);

                var html = File.ReadAllText(reportPath);

                Assert.Contains("<!DOCTYPE html>", html);
                Assert.Contains("Import Validation Report", html);
                Assert.Contains("valid-row", html);
                Assert.Contains("invalid-row", html);
                Assert.Contains("CrossFieldCheck", html);
                Assert.Contains("<table>", html);
            }
            finally
            {
                if (File.Exists(reportPath)) File.Delete(reportPath);
            }
        });
    }

    [Fact]
    public void Generate_HtmlReport_ShowsRowNumbers()
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
            importer.Validate();

            var reportPath = Path.Combine(Path.GetTempPath(), $"report_{Guid.NewGuid():N}.html");
            try
            {
                var report = importer.CreateReportGenerator();
                report.Generate(ReportFormat.Html, ReportDestination.File, outputFilePath: reportPath);

                var html = File.ReadAllText(reportPath);
                Assert.Contains("Row#", html);
                Assert.Contains("<th>Status</th>", html);
            }
            finally
            {
                if (File.Exists(reportPath)) File.Delete(reportPath);
            }
        });
    }

    [Fact]
    public void Generate_JsonReport_InvalidRowsIncludeErrorDetails()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Min";
            ws.Cell(1, 2).Value = "Max";
            ws.Cell(2, 1).Value = 100;
            ws.Cell(2, 2).Value = 5;
        }, path =>
        {
            var importer = new Importer<CrossFieldDto>(path);
            importer.Import();
            importer.Validate();

            var reportPath = Path.Combine(Path.GetTempPath(), $"report_{Guid.NewGuid():N}.json");
            try
            {
                var report = importer.CreateReportGenerator();
                report.Generate(ReportFormat.Json, ReportDestination.File, outputFilePath: reportPath);

                var json = File.ReadAllText(reportPath);
                var doc = JsonDocument.Parse(json);
                var rows = doc.RootElement.GetProperty("Rows");
                var row = rows[0];

                Assert.Equal("Invalid", row.GetProperty("Status").GetString());
                var errors = row.GetProperty("Errors");
                Assert.Equal(1, errors.GetArrayLength());
                Assert.Equal("CrossFieldCheck", errors[0].GetProperty("RuleName").GetString());
            }
            finally
            {
                if (File.Exists(reportPath)) File.Delete(reportPath);
            }
        });
    }

    [Fact]
    public void Generate_NoErrors_HtmlStillProducesValidOutput()
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
            importer.Validate();

            var reportPath = Path.Combine(Path.GetTempPath(), $"report_{Guid.NewGuid():N}.html");
            try
            {
                var report = importer.CreateReportGenerator();
                report.Generate(ReportFormat.Html, ReportDestination.File, outputFilePath: reportPath);

                var html = File.ReadAllText(reportPath);
                Assert.Contains("<!DOCTYPE html>", html);
                Assert.Contains("valid-row", html);
                // Check that no data rows are marked invalid (CSS style block contains "invalid-row" as a class name)
                var tbody = html[(html.IndexOf("<tbody>"))..];
                Assert.DoesNotContain("invalid-row", tbody);
            }
            finally
            {
                if (File.Exists(reportPath)) File.Delete(reportPath);
            }
        });
    }
}
