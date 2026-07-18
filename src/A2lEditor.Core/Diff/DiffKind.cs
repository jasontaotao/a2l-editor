namespace A2lEditor.Core.Diff;

/// <summary>
/// 块级别的变动类型
/// </summary>
public enum DiffKind
{
    Unchanged,
    Added,
    Removed,
    Modified,
}
