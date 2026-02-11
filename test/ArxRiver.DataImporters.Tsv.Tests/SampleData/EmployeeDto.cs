using ArxRiver.DataImporters.Core.Attributes;
using ArxRiver.DataImporters.Tsv.Attributes;

namespace ArxRiver.DataImporters.Tsv.Tests.SampleData;

[Validator(typeof(EmployeeSalaryValidator), RuleName = "SalaryRangeByDepartment")]
[InlineValidation("Row.FirstName != Row.LastName", ErrorMessage = "First and last name cannot be identical")]
public sealed class EmployeeDto
{
    [TsvColumn("first_name")]
    public string FirstName { get; set; } = "";

    [TsvColumn("last_name")]
    public string LastName { get; set; } = "";

    [TsvColumn("email")]
    [InlineValidation("Row.Email.Contains(\"@\")", ErrorMessage = "Email must contain @", RuleName = "EmailFormat")]
    public string Email { get; set; } = "";

    [TsvColumn("age")]
    [InlineValidation("Row.Age >= 18 && Row.Age <= 120", ErrorMessage = "Age must be between 18 and 120", RuleName = "AgeRange")]
    public int Age { get; set; }

    [TsvColumn("department")]
    public string Department { get; set; } = "";

    [TsvColumn("salary")]
    [InlineValidation("Row.Salary > 0", ErrorMessage = "Salary must be positive", RuleName = "PositiveSalary")]
    public decimal Salary { get; set; }

    [TsvColumn("start_date")]
    public DateTime StartDate { get; set; }
}
