using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using A2lEditor.Core.Model;
using A2lEditor.Core.Serialization;
using A2lEditor.Core.Skeleton;

namespace A2lEditor.App.Views;

public partial class ImportExcelDialog : Window
{
    private A2lDocument? _generatedDoc;
    private string? _excelPath;

    public ImportExcelDialog()
    {
        InitializeComponent();
    }

    // ====================================================================
    // 事件处理
    // ====================================================================

    private void OnBrowseExcel(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Excel files|*.xlsx|All files|*.*",
            Title = "Select .xlsx file"
        };
        if (dlg.ShowDialog() != true) return;

        _excelPath = dlg.FileName;
        ExcelPathBox.Text = dlg.FileName;
        PreviewBlock.Text = "Click Preview to scan the file.";
        DetailBox.Text = "";
        _generatedDoc = null;
    }

    private void OnPreview(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_excelPath))
        {
            PreviewBlock.Text = "Please select an .xlsx file first.";
            return;
        }

        if (!File.Exists(_excelPath))
        {
            PreviewBlock.Text = $"File not found: {_excelPath}";
            return;
        }

        try
        {
            Mouse.OverrideCursor = Cursors.Wait;

            var sheet = string.IsNullOrWhiteSpace(SheetBox.Text) ? null : SheetBox.Text.Trim();
            var moduleName = string.IsNullOrWhiteSpace(ModuleBox.Text) ? "ImportedModule" : ModuleBox.Text.Trim();
            var comment = string.IsNullOrWhiteSpace(CommentBox.Text) ? "Generated from Excel" : CommentBox.Text.Trim();

            var options = new SkeletonGenerateOptions(sheet, moduleName, comment);
            var service = new A2lSkeletonService();
            _generatedDoc = service.GenerateFromExcel(_excelPath, options);

            var module = _generatedDoc.Modules is [var m, ..]
                ? m : throw new InvalidOperationException("Generated document has no modules.");
            var preview = FormatPreview(
                Path.GetFileName(_excelPath),
                moduleName,
                module.Measurements.Count,
                module.Characteristics.Count,
                module.CompuMethods.Count);

            PreviewBlock.Text = $"Module: {moduleName} — " +
                $"{module.Measurements.Count} MEASUREMENTs, " +
                $"{module.Characteristics.Count} CHARACTERISTICs, " +
                $"{module.CompuMethods.Count} COMPU_METHODs";

            var detail = BuildDetailText(module);
            DetailBox.Text = detail;
        }
        catch (Exception ex)
        {
            PreviewBlock.Text = $"Error: {ex.Message}";
            DetailBox.Text = "";
            _generatedDoc = null;
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (_generatedDoc is null)
        {
            PreviewBlock.Text = "Please run Preview first.";
            return;
        }

        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "ASAP2 files|*.a2l|All files|*.*",
            FileName = "skeleton.a2l"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            Mouse.OverrideCursor = Cursors.Wait;

            new A2lDocumentWriter().WriteToFile(_generatedDoc, dlg.FileName);

            PreviewBlock.Text = $"A2L skeleton saved to: {Path.GetFileName(dlg.FileName)}";
        }
        catch (Exception ex)
        {
            PreviewBlock.Text = $"Save error: {ex.Message}";
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    // ====================================================================
    // 格式化（测试用 public static）
    // ====================================================================

    /// <summary>封面概要文本（测试用，无需 Window 实例化）。</summary>
    internal static string FormatPreview(
        string fileName,
        string moduleName,
        int measurementCount,
        int characteristicCount,
        int compuMethodCount)
    {
        return $"File: {fileName}\n" +
               $"Module: {moduleName}\n" +
               $"MEASUREMENTS:   {measurementCount}\n" +
               $"CHARACTERISTICS: {characteristicCount}\n" +
               $"COMPU_METHODS:  {compuMethodCount}";
    }

    /// <summary>构建详细信号列表文本。</summary>
    private static string BuildDetailText(A2lModule module)
    {
        using var w = new StringWriter();

        if (module.Measurements.Count > 0)
        {
            w.WriteLine("[MEASUREMENTS]");
            foreach (var m in module.Measurements)
                w.WriteLine($"  {m.Name}  ({m.DataType})  ECUA=0x{m.EcuAddress:X}");
        }

        if (module.Characteristics.Count > 0)
        {
            w.WriteLine();
            w.WriteLine("[CHARACTERISTICS]");
            foreach (var c in module.Characteristics)
                w.WriteLine($"  {c.Name}  ECUA=0x{c.EcuAddress:X}");
        }

        if (module.CompuMethods.Count > 0)
        {
            w.WriteLine();
            w.WriteLine("[COMPU_METHODS]");
            foreach (var cm in module.CompuMethods)
                w.WriteLine($"  {cm.Name}  ({cm.ConversionType})");
        }

        return w.ToString();
    }
}
