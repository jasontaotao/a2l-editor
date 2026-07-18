using System.IO;
using System.Linq;
using A2lEditor.Core.Model;
using A2lEditor.Core.Skeleton;
using A2lEditor.Core.Tests.Diff;
using ClosedXML.Excel;
using FluentAssertions;
using Xunit;

namespace A2lEditor.Core.Tests.Skeleton;

public class A2lExcelExporterTests
{
    [Fact]
    public void Export_Measurements_WritesCorrectCells()
    {
        var doc = DiffFixtures.DocWith(
            measurements: new[] { DiffFixtures.Meas("V"), DiffFixtures.Meas("I") });

        var xlsxPath = Path.GetTempFileName() + ".xlsx";
        try
        {
            new A2lExcelExporter().Export(doc, xlsxPath);

            using var wb = new XLWorkbook(xlsxPath);
            var ws = wb.Worksheets.First();
            ws.Cell(1, 1).GetString().Should().Be("Name");
            ws.Cell(1, 3).GetString().Should().Be("BlockType");

            ws.Cell(2, 1).GetString().Should().Be("V");
            ws.Cell(2, 3).GetString().Should().Be("MEASUREMENT");
            ws.Cell(2, 4).GetString().Should().Be("UBYTE");
            ws.Cell(2, 8).GetString().Should().Be("0x1000");

            ws.Cell(3, 1).GetString().Should().Be("I");
            ws.Cell(3, 3).GetString().Should().Be("MEASUREMENT");
            ws.Cell(3, 8).GetString().Should().Be("0x1000");
        }
        finally
        {
            if (File.Exists(xlsxPath)) File.Delete(xlsxPath);
        }
    }

    [Fact]
    public void Export_Characteristics_WritesCorrectCells()
    {
        var doc = DiffFixtures.DocWith(
            characteristics: new[] { DiffFixtures.Char("Gain", 0x2000) });

        var xlsxPath = Path.GetTempFileName() + ".xlsx";
        try
        {
            new A2lExcelExporter().Export(doc, xlsxPath);

            using var wb = new XLWorkbook(xlsxPath);
            var ws = wb.Worksheets.First();

            ws.Cell(2, 1).GetString().Should().Be("Gain");
            ws.Cell(2, 3).GetString().Should().Be("CHARACTERISTIC");
            ws.Cell(2, 4).GetString().Should().Be("");  // Characteristics 无 DataType
            ws.Cell(2, 8).GetString().Should().Be("0x2000");
        }
        finally
        {
            if (File.Exists(xlsxPath)) File.Delete(xlsxPath);
        }
    }

    [Fact]
    public void Export_Mixed_RowOrderPreserved()
    {
        var doc = DiffFixtures.DocWith(
            measurements: new[] { DiffFixtures.Meas("V"), DiffFixtures.Meas("I") },
            characteristics: new[] { DiffFixtures.Char("C1") });

        var xlsxPath = Path.GetTempFileName() + ".xlsx";
        try
        {
            new A2lExcelExporter().Export(doc, xlsxPath);

            using var wb = new XLWorkbook(xlsxPath);
            var ws = wb.Worksheets.First();

            // 前两个 MEASUREMENT，后一个 CHARACTERISTIC
            ws.Cell(2, 1).GetString().Should().Be("V");
            ws.Cell(3, 1).GetString().Should().Be("I");
            ws.Cell(4, 1).GetString().Should().Be("C1");
            ws.LastRowUsed()!.RowNumber().Should().Be(4);
        }
        finally
        {
            if (File.Exists(xlsxPath)) File.Delete(xlsxPath);
        }
    }

    [Fact]
    public void Export_EmptyModule_ProducesHeaderOnly()
    {
        var doc = DiffFixtures.DocWith();

        var xlsxPath = Path.GetTempFileName() + ".xlsx";
        try
        {
            new A2lExcelExporter().Export(doc, xlsxPath);

            using var wb = new XLWorkbook(xlsxPath);
            var ws = wb.Worksheets.First();

            ws.Cell(1, 1).GetString().Should().Be("Name");
            ws.LastRowUsed()!.RowNumber().Should().Be(1);
        }
        finally
        {
            if (File.Exists(xlsxPath)) File.Delete(xlsxPath);
        }
    }
}
