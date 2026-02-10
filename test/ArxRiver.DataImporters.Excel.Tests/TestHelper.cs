using ClosedXML.Excel;

namespace ArxRiver.DataImporters.Excel.Tests;

/// <summary>
/// Utilities for creating temporary Excel files during tests.
/// </summary>
internal static class TestHelper
{
    /// <summary>
    /// Creates a temporary .xlsx file and returns its path. Caller is responsible for cleanup.
    /// </summary>
    public static string CreateTempExcel(Action<IXLWorksheet> configure)
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.xlsx");
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sheet1");
        configure(ws);
        workbook.SaveAs(path);
        return path;
    }

    /// <summary>
    /// Wraps a test body so the temp file is always cleaned up.
    /// </summary>
    public static async Task WithTempExcel(Action<IXLWorksheet> configure, Func<string, Task> test)
    {
        var path = CreateTempExcel(configure);
        try
        {
            await test(path);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    public static void WithTempExcel(Action<IXLWorksheet> configure, Action<string> test)
    {
        var path = CreateTempExcel(configure);
        try
        {
            test(path);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
