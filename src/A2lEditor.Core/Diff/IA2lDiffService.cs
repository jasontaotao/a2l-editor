using A2lEditor.Core.Model;

namespace A2lEditor.Core.Diff;

/// <summary>
/// 结构化 A2L 文档对比服务。
/// 在两个解析好的 A2lDocument 之间做块级按名字匹配的差异分析。
/// </summary>
public interface IA2lDiffService
{
    /// <summary>
    /// 对比两个 A2lDocument，返回结构化 diff 报告。
    /// 如果 baseline/compared 为 null，返回空报告（DiffKind=Unchanged）。
    /// </summary>
    A2lDiffReport DiffDocuments(
        A2lDocument? baseline,
        A2lDocument? compared,
        string? baselinePath = null,
        string? comparedPath = null);
}
