using ArxRiver.DataImporters.Core.Reporting;
using ArxRiver.DataImporters.Excel.Importing;
using ArxRiver.DataImporters.Excel.Tests.SampleData;

namespace ArxRiver.DataImporters.Excel.Tests;

/// <summary>
/// Integration tests using the EmployeeDto with all validation layers (Roslyn + IRowValidator + fluent).
/// </summary>
public class SampleDataTests
{
    [Fact]
    public void SampleDataGenerator_CreatesValidExcelFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.xlsx");
        try
        {
            SampleDataGenerator.Generate(path);
            Assert.True(File.Exists(path));

            var importer = new Importer<EmployeeDto>(path);
            var rows = importer.Import();

            Assert.Equal(9, rows.Count);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void EmployeeDto_ValidRows_PassAllValidation()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.xlsx");
        try
        {
            SampleDataGenerator.Generate(path);
            var importer = new Importer<EmployeeDto>(path);
            importer.Import();
            importer.Validate();

            var validRows = importer.GetValidRows();

            // Sample data has 5 valid rows (Alice, Bob, Carol, David, Eva)
            Assert.Equal(5, validRows.Count);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void EmployeeDto_InvalidRows_DetectedCorrectly()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.xlsx");
        try
        {
            SampleDataGenerator.Generate(path);
            var importer = new Importer<EmployeeDto>(path);
            importer.Import();
            importer.Validate();

            var invalidRows = importer.GetInvalidRows();

            // Sample data has 4 invalid rows (Frank, Grace, Henry, Ivy)
            Assert.Equal(4, invalidRows.Count);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void EmployeeDto_FrankRow_HasMultipleErrors()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.xlsx");
        try
        {
            SampleDataGenerator.Generate(path);
            var importer = new Importer<EmployeeDto>(path);
            importer.Import();
            var errors = importer.Validate();

            // Frank (row 7) has: salary out of range, same first/last, bad email, under 18, negative salary
            var frankErrors = errors.Where(e => e.RowNumber == 7).ToList();
            Assert.True(frankErrors.Count >= 4);
            Assert.Contains(frankErrors, e => e.RuleName == "SalaryRangeByDepartment");
            Assert.Contains(frankErrors, e => e.RuleName == "EmailFormat");
            Assert.Contains(frankErrors, e => e.RuleName == "AgeRange");
            Assert.Contains(frankErrors, e => e.RuleName == "PositiveSalary");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void EmployeeDto_FluentValidation_AddsExtraErrors()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.xlsx");
        try
        {
            SampleDataGenerator.Generate(path);
            var importer = new Importer<EmployeeDto>(path)
                .ForColumn(x => x.Email,
                    (email, _) => email.EndsWith("@acme.com", StringComparison.OrdinalIgnoreCase),
                    "Email must be an @acme.com address");

            importer.Import();
            var errors = importer.Validate();

            // Frank and Henry have non-@acme.com emails
            var emailErrors = errors.Where(e => e.RuleName == "ForColumn:Email").ToList();
            Assert.Equal(2, emailErrors.Count);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void EmployeeDto_FullPipeline_GeneratesBothReports()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.xlsx");
        var jsonPath = Path.Combine(Path.GetTempPath(), $"report_{Guid.NewGuid():N}.json");
        var htmlPath = Path.Combine(Path.GetTempPath(), $"report_{Guid.NewGuid():N}.html");
        try
        {
            SampleDataGenerator.Generate(path);
            var importer = new Importer<EmployeeDto>(path);
            importer.Import();
            importer.Validate();

            var report = importer.CreateReportGenerator();
            report.Generate(ReportFormat.Json, ReportDestination.File, outputFilePath: jsonPath);
            report.Generate(ReportFormat.Html, ReportDestination.File, outputFilePath: htmlPath);

            Assert.True(File.Exists(jsonPath));
            Assert.True(File.Exists(htmlPath));
            Assert.True(new FileInfo(jsonPath).Length > 100);
            Assert.True(new FileInfo(htmlPath).Length > 100);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(jsonPath)) File.Delete(jsonPath);
            if (File.Exists(htmlPath)) File.Delete(htmlPath);
        }
    }
}
