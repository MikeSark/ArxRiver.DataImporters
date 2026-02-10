using ClosedXML.Excel;

namespace ArxRiver.DataImporters.Excel.Tests.SampleData;

/// <summary>
/// Generates a sample employees.xlsx file with a mix of valid and invalid rows for testing.
/// </summary>
public static class SampleDataGenerator
{
    public static void Generate(string filePath)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Employees");

        // Headers (row 1)
        ws.Cell(1, 1).Value = "First Name";
        ws.Cell(1, 2).Value = "Last Name";
        ws.Cell(1, 3).Value = "Email";
        ws.Cell(1, 4).Value = "Age";       // Column 4 — mapped by number in EmployeeDto
        ws.Cell(1, 5).Value = "Department";
        ws.Cell(1, 6).Value = "Salary";
        ws.Cell(1, 7).Value = "Start Date";

        // Style header row
        var headerRange = ws.Range(1, 1, 1, 7);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.CornflowerBlue;
        headerRange.Style.Font.FontColor = XLColor.White;

        // Valid rows
        AddRow(ws, 2, "Alice", "Johnson", "alice.johnson@acme.com", 32, "Engineering", 120000m, new DateTime(2020, 3, 15));
        AddRow(ws, 3, "Bob", "Smith", "bob.smith@acme.com", 45, "Marketing", 95000m, new DateTime(2018, 7, 1));
        AddRow(ws, 4, "Carol", "Williams", "carol.w@acme.com", 28, "Sales", 72000m, new DateTime(2022, 1, 10));
        AddRow(ws, 5, "David", "Brown", "david.brown@acme.com", 38, "HR", 85000m, new DateTime(2019, 11, 20));
        AddRow(ws, 6, "Eva", "Garcia", "eva.garcia@acme.com", 55, "Engineering", 180000m, new DateTime(2015, 5, 5));

        // Invalid rows (various validation failures)
        AddRow(ws, 7, "Frank", "Frank", "frank.invalid", 15, "Engineering", -5000m, new DateTime(2023, 6, 1));
        //           ^ same first/last name   ^ missing @    ^ under 18      ^ negative salary

        AddRow(ws, 8, "Grace", "Lee", "grace.lee@acme.com", 29, "Engineering", 300000m, new DateTime(2021, 9, 15));
        //                                                                      ^ exceeds Engineering max (250k)

        AddRow(ws, 9, "Henry", "Taylor", "henry-no-at-sign", 130, "Sales", 50000m, new DateTime(2020, 2, 28));
        //                                ^ missing @           ^ age > 120

        AddRow(ws, 10, "Ivy", "Martinez", "ivy.m@acme.com", 25, "Marketing", 200000m, new DateTime(2022, 4, 12));
        //                                                                    ^ exceeds Marketing max (180k)

        ws.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }

    private static void AddRow(IXLWorksheet ws, int row,
        string firstName, string lastName, string email, int age,
        string department, decimal salary, DateTime startDate)
    {
        ws.Cell(row, 1).Value = firstName;
        ws.Cell(row, 2).Value = lastName;
        ws.Cell(row, 3).Value = email;
        ws.Cell(row, 4).Value = age;
        ws.Cell(row, 5).Value = department;
        ws.Cell(row, 6).Value = salary;
        ws.Cell(row, 7).Value = startDate;
    }
}
