using ArxRiver.DataImporters.Core.Reporting;
using ArxRiver.DataImporters.Tsv.Importing;
using ArxRiver.DataImporters.Tsv.Tests.SampleData;

namespace ArxRiver.DataImporters.Tsv.Tests;

/// <summary>
/// Integration tests using the EmployeeDto with TsvImporter and all validation layers.
/// </summary>
public class TsvSampleDataTests
{
    [Fact]
    public void EmployeeTsvGenerator_CreatesValidTsvFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.tsv");
        try
        {
            EmployeeTsvGenerator.Generate(path);
            Assert.True(File.Exists(path));

            var importer = new TsvImporter<EmployeeDto>(path);
            var rows = importer.Import();

            Assert.Equal(9, rows.Count);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void EmployeeDto_Tsv_ValidRows_PassAllValidation()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.tsv");
        try
        {
            EmployeeTsvGenerator.Generate(path);
            var importer = new TsvImporter<EmployeeDto>(path);
            importer.Import();
            importer.Validate();

            var validRows = importer.GetValidRows();
            Assert.Equal(5, validRows.Count);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void EmployeeDto_Tsv_InvalidRows_DetectedCorrectly()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.tsv");
        try
        {
            EmployeeTsvGenerator.Generate(path);
            var importer = new TsvImporter<EmployeeDto>(path);
            importer.Import();
            importer.Validate();

            var invalidRows = importer.GetInvalidRows();
            Assert.Equal(4, invalidRows.Count);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void EmployeeDto_Tsv_FrankRow_HasMultipleErrors()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.tsv");
        try
        {
            EmployeeTsvGenerator.Generate(path);
            var importer = new TsvImporter<EmployeeDto>(path);
            importer.Import();
            var errors = importer.Validate();

            // Frank is row 6 (1-based index in data rows)
            var frankErrors = errors.Where(e => e.RowNumber == 6).ToList();
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
    public void EmployeeDto_Tsv_FluentValidation_AddsExtraErrors()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.tsv");
        try
        {
            EmployeeTsvGenerator.Generate(path);
            var importer = new TsvImporter<EmployeeDto>(path)
                .ForColumn(x => x.Email,
                           (email, _) => email.EndsWith("@acme.com", StringComparison.OrdinalIgnoreCase),
                           "Email must be an @acme.com address");

            importer.Import();
            var errors = importer.Validate();

            var emailErrors = errors.Where(e => e.RuleName == "ForColumn:Email").ToList();
            Assert.Equal(2, emailErrors.Count);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void EmployeeDto_Tsv_FullPipeline_GeneratesBothReports()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.tsv");
        var jsonReportPath = Path.Combine(Path.GetTempPath(), $"report_{Guid.NewGuid():N}.json");
        var htmlReportPath = Path.Combine(Path.GetTempPath(), $"report_{Guid.NewGuid():N}.html");
        try
        {
            EmployeeTsvGenerator.Generate(path);
            var importer = new TsvImporter<EmployeeDto>(path);
            importer.Import();
            importer.Validate();

            var report = importer.CreateReportGenerator();
            report.Generate(ReportFormat.Json, ReportDestination.File, outputFilePath: jsonReportPath);
            report.Generate(ReportFormat.Html, ReportDestination.File, outputFilePath: htmlReportPath);

            Assert.True(File.Exists(jsonReportPath));
            Assert.True(File.Exists(htmlReportPath));
            Assert.True(new FileInfo(jsonReportPath).Length > 100);
            Assert.True(new FileInfo(htmlReportPath).Length > 100);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(jsonReportPath)) File.Delete(jsonReportPath);
            if (File.Exists(htmlReportPath)) File.Delete(htmlReportPath);
        }
    }
}
