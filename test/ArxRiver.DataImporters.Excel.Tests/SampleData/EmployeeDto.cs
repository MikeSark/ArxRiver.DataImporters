using ArxRiver.DataImporters.Core.Attributes;
using ArxRiver.DataImporters.Excel.Attributes;

namespace ArxRiver.DataImporters.Excel.Tests.SampleData;

[Validator(typeof(EmployeeSalaryValidator), RuleName = "SalaryRangeByDepartment")]
[InlineValidation("row.FirstName != row.LastName", ErrorMessage = "First and last name cannot be identical")]
public sealed class EmployeeDto
{
    [ExcelColumn("First Name")]
    public string FirstName { get; set; } = "";

    [ExcelColumn("Last Name")]
    public string LastName { get; set; } = "";

    [ExcelColumn("Email")]
    [InlineValidation("row.Email.Contains(\"@\")", ErrorMessage = "Email must contain @", RuleName = "EmailFormat")]
    public string Email { get; set; } = "";

    [ExcelColumn(4)]
    [InlineValidation("row.Age >= 18 && row.Age <= 120", ErrorMessage = "Age must be between 18 and 120", RuleName = "AgeRange")]
    public int Age { get; set; }

    [ExcelColumn("Department")]
    public string Department { get; set; } = "";

    [ExcelColumn("Salary")]
    [InlineValidation("row.Salary > 0", ErrorMessage = "Salary must be positive", RuleName = "PositiveSalary")]
    public decimal Salary { get; set; }

    [ExcelColumn("Start Date")]
    public DateTime StartDate { get; set; }
}
