using ArxRiver.DataImporters.Core.Reporting;
using ArxRiver.DataImporters.Csv.Importing;
using ArxRiver.DataImporters.Csv.Tests.SampleData;

namespace ArxRiver.DataImporters.Csv.Tests;

/// <summary>
/// Integration tests using the EmployeeDto with CsvImporter and all validation layers.
/// </summary>
public class CsvSampleDataTests
{
    [Fact]
    public void EmployeeCsvGenerator_CreatesValidCsvFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.csv");
        try
        {
            EmployeeCsvGenerator.Generate(path);
            Assert.True(File.Exists(path));

            var importer = new CsvImporter<EmployeeDto>(path);
            var rows = importer.Import();

            Assert.Equal(9, rows.Count);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void EmployeeDto_Csv_ValidRows_PassAllValidation()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.csv");
        try
        {
            EmployeeCsvGenerator.Generate(path);
            var importer = new CsvImporter<EmployeeDto>(path);
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
    public void EmployeeDto_Csv_InvalidRows_DetectedCorrectly()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.csv");
        try
        {
            EmployeeCsvGenerator.Generate(path);
            var importer = new CsvImporter<EmployeeDto>(path);
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
    public void EmployeeDto_Csv_FrankRow_HasMultipleErrors()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.csv");
        try
        {
            EmployeeCsvGenerator.Generate(path);
            var importer = new CsvImporter<EmployeeDto>(path);
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
    public void EmployeeDto_Csv_FluentValidation_AddsExtraErrors()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.csv");
        try
        {
            EmployeeCsvGenerator.Generate(path);
            var importer = new CsvImporter<EmployeeDto>(path)
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
    public void EmployeeDto_Csv_FullPipeline_GeneratesBothReports()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.csv");
        var jsonReportPath = Path.Combine(Path.GetTempPath(), $"report_{Guid.NewGuid():N}.json");
        var htmlReportPath = Path.Combine(Path.GetTempPath(), $"report_{Guid.NewGuid():N}.html");
        try
        {
            EmployeeCsvGenerator.Generate(path);
            var importer = new CsvImporter<EmployeeDto>(path);
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
