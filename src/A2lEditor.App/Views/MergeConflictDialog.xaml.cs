using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using A2lEditor.Core.Diff;

namespace A2lEditor.App.Views;

public partial class MergeConflictDialog : Window
{
    private readonly ObservableCollection<ChangeItem> _items = new();

    public MergeConflictDialog(A2lDiffReport report)
    {
        InitializeComponent();
        LoadReport(report);
    }

    /// <summary>返回用户接受的变更集合（键格式 "BlockType:BlockName"）。</summary>
    public HashSet<string> GetAcceptedChanges()
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in _items)
        {
            if (item.Accepted && item.ChangeKey is not null)
                set.Add(item.ChangeKey);
        }
        return set;
    }

    private void LoadReport(A2lDiffReport report)
    {
        foreach (var mod in report.ModuleDiffs)
        {
            int added, removed, modified;
            CountChanges(mod, out added, out removed, out modified);
            if (added == 0 && removed == 0 && modified == 0 && mod.ModParChange is null)
                continue;

            // 每个区块类型的变更
            AddBlockDiffs(mod.ModuleName, mod.MeasurementDiffs);
            AddBlockDiffs(mod.ModuleName, mod.CharacteristicDiffs);
            AddBlockDiffs(mod.ModuleName, mod.AxisPtsDiffs);
            AddBlockDiffs(mod.ModuleName, mod.AxisPtsXDiffs);
            AddBlockDiffs(mod.ModuleName, mod.CompuMethodDiffs);
            AddBlockDiffs(mod.ModuleName, mod.RecordLayoutDiffs);
            AddBlockDiffs(mod.ModuleName, mod.GroupDiffs);
            AddBlockDiffs(mod.ModuleName, mod.AxisDescrDiffs);
            AddBlockDiffs(mod.ModuleName, mod.UserRightsDiffs);
            AddBlockDiffs(mod.ModuleName, mod.VersionInfoDiffs);

            // MOD_PAR
            if (mod.ModParChange is not null)
            {
                _items.Add(new ChangeItem
                {
                    Module = mod.ModuleName,
                    ChangeKey = $"MOD_PAR:{mod.ModuleName}",
                    Title = $"MOD_PAR ({mod.ModuleName})",
                    KindLabel = "MODIFIED",
                    Details = $"{mod.ModParChange.OldValue} → {mod.ModParChange.NewValue}",
                    Accepted = true,
                });
            }
        }

        ChangeList.ItemsSource = _items;

        var totalModified = _items.Count(i => i.KindLabel == "MODIFIED");
        var totalAdded = _items.Count(i => i.KindLabel == "ADDED");
        var totalRemoved = _items.Count(i => i.KindLabel == "REMOVED");

        if (_items.Count == 0)
        {
            SummaryBlock.Text = "No changes detected. Everything is up to date.";
        }
        else
        {
            SummaryBlock.Text =
                $"{_items.Count} change(s): {totalModified} Modified, {totalAdded} Added, {totalRemoved} Removed " +
                $"(unchecked changes will keep baseline version)";
        }
    }

    private void AddBlockDiffs(string moduleName, IReadOnlyList<BlockDiff> diffs)
    {
        foreach (var diff in diffs)
        {
            if (diff.Kind == DiffKind.Unchanged) continue;

            var details = diff.Kind == DiffKind.Modified
                ? string.Join("; ", diff.FieldChanges.Select(fc => $"{fc.FieldName}: {fc.OldValue} → {fc.NewValue}"))
                : "";

            // Removed 和 Added 以外的变更默认选中
            var isAccepted = diff.Kind != DiffKind.Removed;

            _items.Add(new ChangeItem
            {
                Module = moduleName,
                ChangeKey = $"{diff.BlockType}:{diff.BlockName}",
                Title = $"{diff.BlockType} {diff.BlockName}",
                KindLabel = diff.Kind switch
                {
                    DiffKind.Added => "ADDED",
                    DiffKind.Removed => "REMOVED",
                    DiffKind.Modified => "MODIFIED",
                    _ => diff.Kind.ToString(),
                },
                Details = details,
                Accepted = isAccepted,
            });
        }
    }

    private static void CountChanges(ModuleDiff mod,
        out int added, out int removed, out int modified)
    {
        added = CountKind(mod.MeasurementDiffs, DiffKind.Added);
        removed = CountKind(mod.MeasurementDiffs, DiffKind.Removed);
        modified = CountKind(mod.MeasurementDiffs, DiffKind.Modified);
    }

    private static int CountKind(IReadOnlyList<BlockDiff> diffs, DiffKind kind) =>
        diffs.Count(d => d.Kind == kind);

    private void OnAccept(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

/// <summary>
/// 对话框中的单条变更条目（ViewModel）。
/// </summary>
public sealed class ChangeItem : INotifyPropertyChanged
{
    private bool _accepted = true;

    public string? Module { get; set; }
    public string? ChangeKey { get; set; }
    public string? Title { get; set; }
    public string? KindLabel { get; set; }
    public string? Details { get; set; }

    public bool Accepted
    {
        get => _accepted;
        set { _accepted = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Accepted))); }
    }

    public Brush KindColor => KindLabel switch
    {
        "ADDED" => Brushes.Green,
        "REMOVED" => Brushes.Red,
        "MODIFIED" => Brushes.DodgerBlue,
        _ => Brushes.Gray,
    };

    public event PropertyChangedEventHandler? PropertyChanged;
}
