using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using A2lEditor.Core.Diff;
using A2lEditor.Core.Merge;
using A2lEditor.Core.Model;
using A2lEditor.Core.Parsing;
using A2lEditor.Core.Serialization;

namespace A2lEditor.App.Views;

public partial class DiffReportDialog : Window
{
    private string? _baselinePath;
    private string? _comparedPath;
    private A2lDiffReport? _lastReport;

    public DiffReportDialog()
    {
        InitializeComponent();
    }

    private string? PickA2lFile()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "ASAP2 files|*.a2l|All files|*.*",
            Title = "Select .a2l file"
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    private void OnSelectBaseline(object sender, RoutedEventArgs e)
    {
        var path = PickA2lFile();
        if (path is null) return;
        _baselinePath = path;
        BaselinePathBox.Text = path;
    }

    private void OnSelectCompared(object sender, RoutedEventArgs e)
    {
        var path = PickA2lFile();
        if (path is null) return;
        _comparedPath = path;
        ComparedPathBox.Text = path;
    }

    private void OnCompare(object sender, RoutedEventArgs e)
    {
        if (_baselinePath is null || _comparedPath is null)
        {
            SummaryBlock.Text = "Please select both files first.";
            return;
        }

        if (!File.Exists(_baselinePath))
        {
            SummaryBlock.Text = $"File not found: {_baselinePath}";
            return;
        }
        if (!File.Exists(_comparedPath))
        {
            SummaryBlock.Text = $"File not found: {_comparedPath}";
            return;
        }

        try
        {
            Mouse.OverrideCursor = Cursors.Wait;

            var doc1 = A2lDocument.LoadFromFile(_baselinePath);
            var doc2 = A2lDocument.LoadFromFile(_comparedPath);

            var service = new A2lDiffService();
            _lastReport = service.DiffDocuments(doc1, doc2, _baselinePath, _comparedPath);

            ReportBox.Text = FormatReport(_lastReport);

            if (!_lastReport.HasChanges)
            {
                SummaryBlock.Text = "All blocks unchanged.";
            }
            else
            {
                SummaryBlock.Text = $"Summary: {_lastReport.TotalAdded} Added, " +
                    $"{_lastReport.TotalRemoved} Removed, " +
                    $"{_lastReport.TotalModified} Modified";
            }
        }
        catch (Exception ex)
        {
            SummaryBlock.Text = $"Error: {ex.Message}";
            ReportBox.Text = "";
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void OnSaveMerged(object sender, RoutedEventArgs e)
    {
        if (_baselinePath is null || _comparedPath is null)
        {
            SummaryBlock.Text = "Please run Compare first.";
            return;
        }

        try
        {
            Mouse.OverrideCursor = Cursors.Wait;

            var doc1 = A2lDocument.LoadFromFile(_baselinePath);
            var doc2 = A2lDocument.LoadFromFile(_comparedPath);

            // 交互式冲突审查
            Mouse.OverrideCursor = null;
            var conflictDlg = new MergeConflictDialog(_lastReport ?? CreateFreshReport(doc1, doc2))
            {
                Owner = this
            };
            if (conflictDlg.ShowDialog() != true)
            {
                SummaryBlock.Text = "Merge cancelled.";
                return;
            }
            var acceptedChanges = conflictDlg.GetAcceptedChanges();
            Mouse.OverrideCursor = Cursors.Wait;

            var service = new A2lMergeService(new A2lDiffService());
            var result = service.Merge(doc1, doc2, _baselinePath, _comparedPath, acceptedChanges);

            if (result.MergedDocument is null)
            {
                SummaryBlock.Text = "Merge produced no output.";
                return;
            }

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "ASAP2 files|*.a2l|All files|*.*",
                FileName = "merged.a2l"
            };
            if (dlg.ShowDialog() != true) return;

            new A2lDocumentWriter().WriteToFile(result.MergedDocument, dlg.FileName);
            SummaryBlock.Text = $"Merged file saved: {Path.GetFileName(dlg.FileName)} ({result.AppliedCount} changes applied)";
        }
        catch (Exception ex)
        {
            SummaryBlock.Text = $"Merge error: {ex.Message}";
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    /// <summary>在 _lastReport 为 null 时创建一个新报告。</summary>
    private static A2lDiffReport CreateFreshReport(A2lDocument doc1, A2lDocument doc2)
        => new A2lDiffService().DiffDocuments(doc1, doc2);

    private void OnCopyReport(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(ReportBox.Text)) return;
        try
        {
            Clipboard.SetText(ReportBox.Text);
            SummaryBlock.Text = "Report copied to clipboard.";
        }
        catch
        {
            SummaryBlock.Text = "Failed to copy to clipboard.";
        }
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    // ====================================================================
    // 报告格式化（与 CLI 输出格式一致）
    // ====================================================================

    internal static string FormatReport(A2lDiffReport report)
    {
        var short1 = report.BaselinePath is not null
            ? Path.GetFileName(report.BaselinePath) : "?";
        var short2 = report.ComparedPath is not null
            ? Path.GetFileName(report.ComparedPath) : "?";

        var w = new StringWriter();

        w.WriteLine($"A2L Diff: {short1} ↔ {short2}");
        w.WriteLine(new string('=', 50));
        w.WriteLine();

        // 文档级变化
        var docChanges = report.DocumentChanges.Where(fc => !fc.IsUnchanged).ToList();
        if (docChanges.Count > 0)
        {
            w.WriteLine("[DOCUMENT] — Modified");
            foreach (var fc in docChanges)
                w.WriteLine($"    {fc.FieldName}: {fc.OldValue} → {fc.NewValue}");
            w.WriteLine();
        }

        // 模块级
        foreach (var mod in report.ModuleDiffs)
        {
            int added, removed, modified;
            CountModuleChanges(mod, out added, out removed, out modified);

            if (added == 0 && removed == 0 && modified == 0 && mod.ModParChange is null)
                continue;

            var label = mod.Kind switch
            {
                DiffKind.Added => "Added",
                DiffKind.Removed => "Removed",
                DiffKind.Modified => "Modified",
                _ => "Unchanged",
            };

            w.WriteLine($"[MODULE] {mod.ModuleName} — {label}");

            // MOD_PAR
            if (mod.ModParChange is not null)
            {
                w.WriteLine($"    ─ MOD_PAR — Modified");
                w.WriteLine($"        {mod.ModParChange.OldValue} → {mod.ModParChange.NewValue}");
            }

            WriteBlockDiffs(w, mod.MeasurementDiffs);
            WriteBlockDiffs(w, mod.CharacteristicDiffs);
            WriteBlockDiffs(w, mod.AxisPtsDiffs);
            WriteBlockDiffs(w, mod.AxisPtsXDiffs);
            WriteBlockDiffs(w, mod.CompuMethodDiffs);
            WriteBlockDiffs(w, mod.RecordLayoutDiffs);
            WriteBlockDiffs(w, mod.GroupDiffs);
            WriteBlockDiffs(w, mod.AxisDescrDiffs);
            WriteBlockDiffs(w, mod.UserRightsDiffs);
            WriteBlockDiffs(w, mod.VersionInfoDiffs);

            w.WriteLine();
        }

        w.Write($"Summary: {report.TotalAdded} Added, {report.TotalRemoved} Removed, {report.TotalModified} Modified");
        if (!report.HasChanges)
            w.Write(" — all blocks unchanged.");

        return w.ToString();
    }

    private static void WriteBlockDiffs(TextWriter w, System.Collections.Generic.IReadOnlyList<BlockDiff> diffs)
    {
        foreach (var block in diffs)
        {
            if (block.Kind == DiffKind.Unchanged) continue;

            var label = block.Kind switch
            {
                DiffKind.Added => "Added",
                DiffKind.Removed => "Removed",
                DiffKind.Modified => "Modified",
                _ => "",
            };

            w.WriteLine($"    ─ {block.BlockType} {block.BlockName} — {label}");

            foreach (var fc in block.FieldChanges)
            {
                w.WriteLine($"        {fc.FieldName}: {fc.OldValue} → {fc.NewValue}");
            }
        }
    }

    private static void CountModuleChanges(ModuleDiff mod,
        out int added, out int removed, out int modified)
    {
        added = CountKind(mod.MeasurementDiffs, DiffKind.Added)
              + CountKind(mod.CharacteristicDiffs, DiffKind.Added)
              + CountKind(mod.AxisPtsDiffs, DiffKind.Added)
              + CountKind(mod.AxisPtsXDiffs, DiffKind.Added)
              + CountKind(mod.CompuMethodDiffs, DiffKind.Added)
              + CountKind(mod.RecordLayoutDiffs, DiffKind.Added)
              + CountKind(mod.GroupDiffs, DiffKind.Added)
              + CountKind(mod.AxisDescrDiffs, DiffKind.Added)
              + CountKind(mod.UserRightsDiffs, DiffKind.Added)
              + CountKind(mod.VersionInfoDiffs, DiffKind.Added);

        removed = CountKind(mod.MeasurementDiffs, DiffKind.Removed)
                + CountKind(mod.CharacteristicDiffs, DiffKind.Removed)
                + CountKind(mod.AxisPtsDiffs, DiffKind.Removed)
                + CountKind(mod.AxisPtsXDiffs, DiffKind.Removed)
                + CountKind(mod.CompuMethodDiffs, DiffKind.Removed)
                + CountKind(mod.RecordLayoutDiffs, DiffKind.Removed)
                + CountKind(mod.GroupDiffs, DiffKind.Removed)
                + CountKind(mod.AxisDescrDiffs, DiffKind.Removed)
                + CountKind(mod.UserRightsDiffs, DiffKind.Removed)
                + CountKind(mod.VersionInfoDiffs, DiffKind.Removed);

        modified = CountKind(mod.MeasurementDiffs, DiffKind.Modified)
                 + CountKind(mod.CharacteristicDiffs, DiffKind.Modified)
                 + CountKind(mod.AxisPtsDiffs, DiffKind.Modified)
                 + CountKind(mod.AxisPtsXDiffs, DiffKind.Modified)
                 + CountKind(mod.CompuMethodDiffs, DiffKind.Modified)
                 + CountKind(mod.RecordLayoutDiffs, DiffKind.Modified)
                 + CountKind(mod.GroupDiffs, DiffKind.Modified)
                 + CountKind(mod.AxisDescrDiffs, DiffKind.Modified)
                 + CountKind(mod.UserRightsDiffs, DiffKind.Modified)
                 + CountKind(mod.VersionInfoDiffs, DiffKind.Modified);
    }

    private static int CountKind(System.Collections.Generic.IReadOnlyList<BlockDiff> diffs, DiffKind kind) =>
        diffs.Count(d => d.Kind == kind);
}
