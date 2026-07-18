using System;
using System.Globalization;
using System.Linq;
using A2lEditor.Core.Model;
using ClosedXML.Excel;

namespace A2lEditor.Core.Skeleton;

/// <summary>
/// 将 <see cref="A2lDocument"/> 中的信号定义导出为 Excel (.xlsx)。
/// 输出格式与 <see cref="A2lSkeletonService"/> 的输入列映射一致：
///   A=Name, B=LongIdentifier, C=BlockType, D=DataType, E=CompuMethod,
///   F=LowerLimit, G=UpperLimit, H=EcuAddressHex, I=Resolution, J=Accuracy
/// </summary>
public sealed class A2lExcelExporter
{
    /// <summary>
    /// 将 A2L 文档导出为 .xlsx 文件。
    /// </summary>
    /// <param name="doc">要导出的文档。</param>
    /// <param name="outputPath">输出的 .xlsx 文件路径。</param>
    /// <param name="sheetName">工作表名（默认 "Signals"）。</param>
    public void Export(A2lDocument doc, string outputPath, string? sheetName = null)
    {
        if (doc is null) throw new ArgumentNullException(nameof(doc));
        if (string.IsNullOrEmpty(outputPath))
            throw new ArgumentException("Output path must not be empty.", nameof(outputPath));

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(sheetName ?? "Signals");

        // Header
        ws.Cell(1, 1).Value = "Name";
        ws.Cell(1, 2).Value = "LongIdentifier";
        ws.Cell(1, 3).Value = "BlockType";
        ws.Cell(1, 4).Value = "DataType";
        ws.Cell(1, 5).Value = "CompuMethod";
        ws.Cell(1, 6).Value = "LowerLimit";
        ws.Cell(1, 7).Value = "UpperLimit";
        ws.Cell(1, 8).Value = "EcuAddressHex";
        ws.Cell(1, 9).Value = "Resolution";
        ws.Cell(1, 10).Value = "Accuracy";

        int row = 2;
        foreach (var module in doc.Modules)
        {
            foreach (var m in module.Measurements)
            {
                ws.Cell(row, 1).Value = m.Name;
                ws.Cell(row, 2).Value = m.LongIdentifier;
                ws.Cell(row, 3).Value = "MEASUREMENT";
                ws.Cell(row, 4).Value = m.DataType.ToString();
                ws.Cell(row, 5).Value = m.CompuMethod;
                ws.Cell(row, 6).Value = m.LowerLimit;
                ws.Cell(row, 7).Value = m.UpperLimit;
                ws.Cell(row, 8).Value = $"0x{m.EcuAddress:X}";
                ws.Cell(row, 9).Value = m.Resolution;
                ws.Cell(row, 10).Value = m.Accuracy;
                row++;
            }

            foreach (var c in module.Characteristics)
            {
                ws.Cell(row, 1).Value = c.Name;
                ws.Cell(row, 2).Value = c.LongIdentifier;
                ws.Cell(row, 3).Value = "CHARACTERISTIC";
                ws.Cell(row, 4).Value = "";  // Characteristics 无 DataType
                ws.Cell(row, 5).Value = "";  // Characteristics 无 CompuMethod
                ws.Cell(row, 6).Value = c.LowerLimit;
                ws.Cell(row, 7).Value = c.UpperLimit;
                ws.Cell(row, 8).Value = $"0x{c.EcuAddress:X}";
                ws.Cell(row, 9).Value = "";
                ws.Cell(row, 10).Value = "";
                row++;
            }
        }

        // 自动调整列宽
        ws.Columns().AdjustToContents();

        workbook.SaveAs(outputPath);
    }
}
