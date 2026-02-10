using System.Text;

namespace ArxRiver.DataImporters.Csv.Tests.SampleData;

/// <summary>
/// Generates a sample employees.csv file with the same data as the JSON/Excel sample generators.
/// </summary>
public static class EmployeeCsvGenerator
{
    public static void Generate(string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("first_name,last_name,email,age,department,salary,start_date");
        sb.AppendLine("Alice,Johnson,alice.johnson@acme.com,32,Engineering,120000,2020-03-15");
        sb.AppendLine("Bob,Smith,bob.smith@acme.com,45,Marketing,95000,2018-07-01");
        sb.AppendLine("Carol,Williams,carol.w@acme.com,28,Sales,72000,2022-01-10");
        sb.AppendLine("David,Brown,david.brown@acme.com,38,HR,85000,2019-11-20");
        sb.AppendLine("Eva,Garcia,eva.garcia@acme.com,55,Engineering,180000,2015-05-05");
        // Invalid rows
        sb.AppendLine("Frank,Frank,frank.invalid,15,Engineering,-5000,2023-06-01");
        sb.AppendLine("Grace,Lee,grace.lee@acme.com,29,Engineering,300000,2021-09-15");
        sb.AppendLine("Henry,Taylor,henry-no-at-sign,130,Sales,50000,2020-02-28");
        sb.AppendLine("Ivy,Martinez,ivy.m@acme.com,25,Marketing,200000,2022-04-12");

        File.WriteAllText(filePath, sb.ToString());
    }
}
