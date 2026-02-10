using System.Text;

namespace ArxRiver.DataImporters.Xml.Tests.SampleData;

/// <summary>
/// Generates a sample employees.xml file with the same data as the CSV/JSON/Excel sample generators.
/// </summary>
public static class EmployeeXmlGenerator
{
    public static void Generate(string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<Employees>");
        AppendEmployee(sb, "Alice", "Johnson", "alice.johnson@acme.com", 32, "Engineering", 120000, "2020-03-15");
        AppendEmployee(sb, "Bob", "Smith", "bob.smith@acme.com", 45, "Marketing", 95000, "2018-07-01");
        AppendEmployee(sb, "Carol", "Williams", "carol.w@acme.com", 28, "Sales", 72000, "2022-01-10");
        AppendEmployee(sb, "David", "Brown", "david.brown@acme.com", 38, "HR", 85000, "2019-11-20");
        AppendEmployee(sb, "Eva", "Garcia", "eva.garcia@acme.com", 55, "Engineering", 180000, "2015-05-05");
        // Invalid rows
        AppendEmployee(sb, "Frank", "Frank", "frank.invalid", 15, "Engineering", -5000, "2023-06-01");
        AppendEmployee(sb, "Grace", "Lee", "grace.lee@acme.com", 29, "Engineering", 300000, "2021-09-15");
        AppendEmployee(sb, "Henry", "Taylor", "henry-no-at-sign", 130, "Sales", 50000, "2020-02-28");
        AppendEmployee(sb, "Ivy", "Martinez", "ivy.m@acme.com", 25, "Marketing", 200000, "2022-04-12");
        sb.AppendLine("</Employees>");

        File.WriteAllText(filePath, sb.ToString());
    }

    private static void AppendEmployee(StringBuilder sb, string firstName, string lastName,
        string email, int age, string department, decimal salary, string startDate)
    {
        sb.AppendLine("  <Employee>");
        sb.AppendLine($"    <first_name>{firstName}</first_name>");
        sb.AppendLine($"    <last_name>{lastName}</last_name>");
        sb.AppendLine($"    <email>{email}</email>");
        sb.AppendLine($"    <age>{age}</age>");
        sb.AppendLine($"    <department>{department}</department>");
        sb.AppendLine($"    <salary>{salary}</salary>");
        sb.AppendLine($"    <start_date>{startDate}</start_date>");
        sb.AppendLine("  </Employee>");
    }
}
