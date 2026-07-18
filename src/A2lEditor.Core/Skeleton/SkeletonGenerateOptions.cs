namespace A2lEditor.Core.Skeleton;

/// <summary>
/// Excel→A2L 骨架生成选项。
/// </summary>
public sealed record SkeletonGenerateOptions(
    string? SheetName = null,
    string ModuleName = "ImportedModule",
    string ModuleComment = "Generated from Excel");
