namespace A2lEditor.Core.Diff;

/// <summary>
/// 模块级别的 diff 结果。
/// </summary>
public sealed record ModuleDiff(
    string ModuleName,
    DiffKind Kind,
    IReadOnlyList<BlockDiff> MeasurementDiffs,
    IReadOnlyList<BlockDiff> CharacteristicDiffs,
    IReadOnlyList<BlockDiff> AxisPtsDiffs,
    IReadOnlyList<BlockDiff> AxisPtsXDiffs,
    IReadOnlyList<BlockDiff> CompuMethodDiffs,
    IReadOnlyList<BlockDiff> RecordLayoutDiffs,
    IReadOnlyList<BlockDiff> GroupDiffs,
    IReadOnlyList<BlockDiff> AxisDescrDiffs,
    IReadOnlyList<BlockDiff> UserRightsDiffs,
    IReadOnlyList<BlockDiff> VersionInfoDiffs,
    FieldChange? ModParChange);
