using ArxRiver.DataImporters.Core.Attributes;
using ArxRiver.DataImporters.Json.Attributes;

namespace ArxRiver.DataImporters.Json.Tests.SampleData;

[Validator(typeof(EmployeeSalaryValidator), RuleName = "SalaryRangeByDepartment")]
[InlineValidation("Row.FirstName != Row.LastName", ErrorMessage = "First and last name cannot be identical")]
public sealed class EmployeeDto
{
    [JsonColumn("first_name")]
    public string FirstName { get; set; } = "";

    [JsonColumn("last_name")]
    public string LastName { get; set; } = "";

    [JsonColumn("email")]
    [InlineValidation("Row.Email.Contains(\"@\")", ErrorMessage = "Email must contain @", RuleName = "EmailFormat")]
    public string Email { get; set; } = "";

    [JsonColumn("age")]
    [InlineValidation("Row.Age >= 18 && Row.Age <= 120", ErrorMessage = "Age must be between 18 and 120", RuleName = "AgeRange")]
    public int Age { get; set; }

    [JsonColumn("department")]
    public string Department { get; set; } = "";

    [JsonColumn("salary")]
    [InlineValidation("Row.Salary > 0", ErrorMessage = "Salary must be positive", RuleName = "PositiveSalary")]
    public decimal Salary { get; set; }

    [JsonColumn("start_date")]
    public DateTime StartDate { get; set; }
}
