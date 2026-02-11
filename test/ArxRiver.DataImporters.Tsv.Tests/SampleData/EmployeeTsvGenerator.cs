using System.Text;

namespace ArxRiver.DataImporters.Tsv.Tests.SampleData;

/// <summary>
/// Generates a sample employees.tsv file with the same data as the JSON/Excel/CSV sample generators.
/// </summary>
public static class EmployeeTsvGenerator
{
    public static void Generate(string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("first_name\tlast_name\temail\tage\tdepartment\tsalary\tstart_date");
        sb.AppendLine("Alice\tJohnson\talice.johnson@acme.com\t32\tEngineering\t120000\t2020-03-15");
        sb.AppendLine("Bob\tSmith\tbob.smith@acme.com\t45\tMarketing\t95000\t2018-07-01");
        sb.AppendLine("Carol\tWilliams\tcarol.w@acme.com\t28\tSales\t72000\t2022-01-10");
        sb.AppendLine("David\tBrown\tdavid.brown@acme.com\t38\tHR\t85000\t2019-11-20");
        sb.AppendLine("Eva\tGarcia\teva.garcia@acme.com\t55\tEngineering\t180000\t2015-05-05");
        // Invalid rows
        sb.AppendLine("Frank\tFrank\tfrank.invalid\t15\tEngineering\t-5000\t2023-06-01");
        sb.AppendLine("Grace\tLee\tgrace.lee@acme.com\t29\tEngineering\t300000\t2021-09-15");
        sb.AppendLine("Henry\tTaylor\thenry-no-at-sign\t130\tSales\t50000\t2020-02-28");
        sb.AppendLine("Ivy\tMartinez\tivy.m@acme.com\t25\tMarketing\t200000\t2022-04-12");

        File.WriteAllText(filePath, sb.ToString());
    }
}
