using ArxRiver.DataImporters.Excel.Importing;

namespace ArxRiver.DataImporters.Excel.Tests;

public class TypeConversionTests
{
    [Fact]
    public void Import_ConvertsDateTimeBoolDecimalLong()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Date";
            ws.Cell(1, 2).Value = "Active";
            ws.Cell(1, 3).Value = "Amount";
            ws.Cell(1, 4).Value = "Count";
            ws.Cell(2, 1).Value = new DateTime(2024, 6, 15);
            ws.Cell(2, 2).Value = true;
            ws.Cell(2, 3).Value = 1234.56;
            ws.Cell(2, 4).Value = 999999999L;
        }, path =>
        {
            var importer = new Importer<TypesDto>(path);
            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Equal(new DateTime(2024, 6, 15), rows[0].Date);
            Assert.True(rows[0].Active);
            Assert.Equal(1234.56m, rows[0].Amount);
            Assert.Equal(999999999L, rows[0].Count);
        });
    }

    [Fact]
    public void Import_EmptyCells_ReturnDefaults()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Value";
            ws.Cell(1, 2).Value = "Label";
            // Row 2: leave both cells empty but have content further right so row isn't "empty"
            ws.Cell(2, 3).Value = "filler";
        }, path =>
        {
            var importer = new Importer<NullableDto>(path);
            var rows = importer.Import();

            Assert.Single(rows);
            Assert.Null(rows[0].Value);   // int? defaults to null for empty cell
            Assert.Equal("", rows[0].Label); // string defaults to "" for empty cell
        });
    }

    [Fact]
    public void Import_IntFromDoubleCell_Truncates()
    {
        TestHelper.WithTempExcel(ws =>
        {
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "Age";
            ws.Cell(1, 3).Value = "Score";
            ws.Cell(2, 1).Value = "Test";
            ws.Cell(2, 2).Value = 25.0; // stored as double in Excel
            ws.Cell(2, 3).Value = 77.7;
        }, path =>
        {
            var importer = new Importer<SimpleDto>(path);
            var rows = importer.Import();

            Assert.Equal(25, rows[0].Age);
        });
    }
}
