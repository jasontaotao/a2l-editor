namespace A2lEditor.Core.Diff;

/// <summary>
/// 单个字段的变动。
/// OldValue / NewValue 为空字符串时视为无变化（仅 Modified 时两者均非 null）。
/// </summary>
public sealed record FieldChange(
    string FieldName,
    string? OldValue,
    string? NewValue)
{
    public bool IsUnchanged =>
        string.Equals(OldValue ?? "", NewValue ?? "", System.StringComparison.Ordinal);
}
