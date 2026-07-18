namespace A2lEditor.Core.Diff;

/// <summary>
/// 单个块的 diff 结果（MEASUREMENT / CHARACTERISTIC 等）。
/// FieldChanges 仅当 Kind==Modified 时非空。
/// </summary>
public sealed record BlockDiff(
    string BlockType,
    string BlockName,
    DiffKind Kind,
    IReadOnlyList<FieldChange> FieldChanges);
