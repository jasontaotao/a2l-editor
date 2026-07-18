using System;
using System.IO;
using ClosedXML.Excel;

namespace A2lEditor.Core.Tests.Skeleton;

/// <summary>
/// 创建 Excel 测试用临时文件。用 Dispose 清理。
/// </summary>
internal sealed class TempExcelFile : IDisposable
{
    public string Path { get; }

    public TempExcelFile(Action<IXLWorksheet> populate, string? sheetName = null)
    {
        Path = System.IO.Path.GetTempFileName() + ".xlsx";
        using var workbook = new XLWorkbook();
        var ws = sheetName is not null ? workbook.Worksheets.Add(sheetName) : workbook.Worksheets.Add("Sheet1");
        populate(ws);
        workbook.SaveAs(Path);
    }

    public void Dispose()
    {
        try { if (File.Exists(Path)) File.Delete(Path); } catch { }
    }
}
