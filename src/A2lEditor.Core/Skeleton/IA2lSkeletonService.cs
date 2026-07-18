using A2lEditor.Core.Model;

namespace A2lEditor.Core.Skeleton;

/// <summary>
/// 从 Excel 文件生成 A2L 骨架文档。
/// </summary>
public interface IA2lSkeletonService
{
    A2lDocument GenerateFromExcel(string excelPath, SkeletonGenerateOptions? options = null);
}
