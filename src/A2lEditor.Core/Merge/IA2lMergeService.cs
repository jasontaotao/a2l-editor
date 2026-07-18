using A2lEditor.Core.Model;

namespace A2lEditor.Core.Merge;

/// <summary>
/// A2L 文档合并结果。
/// MergedDocument 为 null 时表示无变更。
/// </summary>
public sealed record A2lMergeResult(
    A2lDocument? MergedDocument,
    int AppliedCount,
    int SkippedCount,
    IReadOnlyList<string> Messages);

/// <summary>
/// 两路合并服务：将 modified 文档的变更合并到 baseline 上。
/// 策略：compared-wins（Modified/Added 用 compared，Unchanged/Removed 用 baseline）。
/// </summary>
public interface IA2lMergeService
{
    A2lMergeResult Merge(
        A2lDocument baseline,
        A2lDocument modified,
        string? baselinePath = null,
        string? modifiedPath = null);
}
