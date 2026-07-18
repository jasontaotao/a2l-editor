namespace A2lEditor.Core.Diff;

/// <summary>
/// 顶层文档 diff 报告。包含文档级字段变化和模块级变化。
/// Kind 反映整体结果：仅所有块都 Unchanged 时 Kind=Unchanged，否则 HasDifferences。
/// </summary>
public sealed record A2lDiffReport(
    string? BaselinePath,
    string? ComparedPath,
    DiffKind Kind,
    IReadOnlyList<FieldChange> DocumentChanges,
    IReadOnlyList<ModuleDiff> ModuleDiffs)
{
    public int TotalAdded => CountByKind(DiffKind.Added);
    public int TotalRemoved => CountByKind(DiffKind.Removed);
    public int TotalModified => CountByKind(DiffKind.Modified);

    private int CountByKind(DiffKind kind) =>
        ModuleDiffs.Sum(m => m.MeasurementDiffs.Count(b => b.Kind == kind)
                           + m.CharacteristicDiffs.Count(b => b.Kind == kind)
                           + m.AxisPtsDiffs.Count(b => b.Kind == kind)
                           + m.AxisPtsXDiffs.Count(b => b.Kind == kind)
                           + m.CompuMethodDiffs.Count(b => b.Kind == kind)
                           + m.RecordLayoutDiffs.Count(b => b.Kind == kind)
                           + m.GroupDiffs.Count(b => b.Kind == kind)
                           + m.AxisDescrDiffs.Count(b => b.Kind == kind)
                           + m.UserRightsDiffs.Count(b => b.Kind == kind)
                           + m.VersionInfoDiffs.Count(b => b.Kind == kind));

    public bool HasChanges =>
        TotalAdded > 0 || TotalRemoved > 0 || TotalModified > 0
        || DocumentChanges.Any(fc => !fc.IsUnchanged);
}
