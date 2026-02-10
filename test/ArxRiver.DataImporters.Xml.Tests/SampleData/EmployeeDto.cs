using ArxRiver.DataImporters.Core.Attributes;
using ArxRiver.DataImporters.Xml.Attributes;

namespace ArxRiver.DataImporters.Xml.Tests.SampleData;

[Validator(typeof(EmployeeSalaryValidator), RuleName = "SalaryRangeByDepartment")]
[InlineValidation("row.FirstName != row.LastName", ErrorMessage = "First and last name cannot be identical")]
public sealed class EmployeeDto
{
    [XmlColumn("first_name")]
    public string FirstName { get; set; } = "";

    [XmlColumn("last_name")]
    public string LastName { get; set; } = "";

    [XmlColumn("email")]
    [InlineValidation("row.Email.Contains(\"@\")", ErrorMessage = "Email must contain @", RuleName = "EmailFormat")]
    public string Email { get; set; } = "";

    [XmlColumn("age")]
    [InlineValidation("row.Age >= 18 && row.Age <= 120", ErrorMessage = "Age must be between 18 and 120", RuleName = "AgeRange")]
    public int Age { get; set; }

    [XmlColumn("department")]
    public string Department { get; set; } = "";

    [XmlColumn("salary")]
    [InlineValidation("row.Salary > 0", ErrorMessage = "Salary must be positive", RuleName = "PositiveSalary")]
    public decimal Salary { get; set; }

    [XmlColumn("start_date")]
    public DateTime StartDate { get; set; }
}
