using ArxRiver.DataImporters.Core.Attributes;
using ArxRiver.DataImporters.Csv.Attributes;

namespace ArxRiver.DataImporters.Csv.Tests.SampleData;

[Validator(typeof(EmployeeSalaryValidator), RuleName = "SalaryRangeByDepartment")]
[InlineValidation("Row.FirstName != Row.LastName", ErrorMessage = "First and last name cannot be identical")]
public sealed class EmployeeDto
{
    [CsvColumn("first_name")]
    public string FirstName { get; set; } = "";

    [CsvColumn("last_name")]
    public string LastName { get; set; } = "";

    [CsvColumn("email")]
    [InlineValidation("Row.Email.Contains(\"@\")", ErrorMessage = "Email must contain @", RuleName = "EmailFormat")]
    public string Email { get; set; } = "";

    [CsvColumn("age")]
    [InlineValidation("Row.Age >= 18 && Row.Age <= 120", ErrorMessage = "Age must be between 18 and 120", RuleName = "AgeRange")]
    public int Age { get; set; }

    [CsvColumn("department")]
    public string Department { get; set; } = "";

    [CsvColumn("salary")]
    [InlineValidation("Row.Salary > 0", ErrorMessage = "Salary must be positive", RuleName = "PositiveSalary")]
    public decimal Salary { get; set; }

    [CsvColumn("start_date")]
    public DateTime StartDate { get; set; }
}
