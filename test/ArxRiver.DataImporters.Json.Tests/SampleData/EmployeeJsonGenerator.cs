using System.Text.Json;

namespace ArxRiver.DataImporters.Json.Tests.SampleData;

/// <summary>
/// Generates a sample employees.json file with the same data as the Excel sample generator.
/// </summary>
public static class EmployeeJsonGenerator
{
    public static void Generate(string filePath)
    {
        var employees = new object[]
        {
            new { first_name = "Alice", last_name = "Johnson", email = "alice.johnson@acme.com", age = 32, department = "Engineering", salary = 120000m, start_date = "2020-03-15" },
            new { first_name = "Bob", last_name = "Smith", email = "bob.smith@acme.com", age = 45, department = "Marketing", salary = 95000m, start_date = "2018-07-01" },
            new { first_name = "Carol", last_name = "Williams", email = "carol.w@acme.com", age = 28, department = "Sales", salary = 72000m, start_date = "2022-01-10" },
            new { first_name = "David", last_name = "Brown", email = "david.brown@acme.com", age = 38, department = "HR", salary = 85000m, start_date = "2019-11-20" },
            new { first_name = "Eva", last_name = "Garcia", email = "eva.garcia@acme.com", age = 55, department = "Engineering", salary = 180000m, start_date = "2015-05-05" },
            // Invalid rows
            new { first_name = "Frank", last_name = "Frank", email = "frank.invalid", age = 15, department = "Engineering", salary = -5000m, start_date = "2023-06-01" },
            new { first_name = "Grace", last_name = "Lee", email = "grace.lee@acme.com", age = 29, department = "Engineering", salary = 300000m, start_date = "2021-09-15" },
            new { first_name = "Henry", last_name = "Taylor", email = "henry-no-at-sign", age = 130, department = "Sales", salary = 50000m, start_date = "2020-02-28" },
            new { first_name = "Ivy", last_name = "Martinez", email = "ivy.m@acme.com", age = 25, department = "Marketing", salary = 200000m, start_date = "2022-04-12" },
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(employees, options);
        File.WriteAllText(filePath, json);
    }
}
