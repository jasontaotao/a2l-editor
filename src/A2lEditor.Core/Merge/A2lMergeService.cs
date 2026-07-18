using System;
using System.Collections.Generic;
using System.Linq;
using A2lEditor.Core.Diff;
using A2lEditor.Core.Model;

namespace A2lEditor.Core.Merge;

/// <summary>
/// 两路 A2L merge 引擎。compared-wins 策略。
/// </summary>
public sealed class A2lMergeService : IA2lMergeService
{
    private readonly IA2lDiffService _diffService;

    public A2lMergeService(IA2lDiffService diffService)
    {
        _diffService = diffService ?? throw new ArgumentNullException(nameof(diffService));
    }

    public A2lMergeResult Merge(
        A2lDocument baseline,
        A2lDocument modified,
        string? baselinePath = null,
        string? modifiedPath = null)
    {
        if (baseline is null)
            throw new ArgumentNullException(nameof(baseline));
        if (modified is null)
            throw new ArgumentNullException(nameof(modified));

        var diff = _diffService.DiffDocuments(baseline, modified, baselinePath, modifiedPath);

        var messages = new List<string>();
        int applied = 0, skipped = 0;

        // 1. 文档级字段合并（compared wins）
        var mergedVersion = modified.Version;
        var mergedProjectName = modified.ProjectName;
        var mergedProjectComment = modified.ProjectComment;
        var mergedHeaderComment = modified.HeaderComment;
        var mergedModCommon = modified.ModCommon ?? baseline.ModCommon;

        // 2. 模块合并
        var baselineMods = baseline.Modules.ToDictionary(m => m.Name, StringComparer.Ordinal);
        var modifiedMods = modified.Modules.ToDictionary(m => m.Name, StringComparer.Ordinal);
        var mergedModules = new List<A2lModule>();

        // 优先处理 baseline 中的模块（保留顺序）
        foreach (var baseMod in baseline.Modules)
        {
            if (!modifiedMods.TryGetValue(baseMod.Name, out var modMod))
            {
                // Removed — 保留 baseline 版本（two-way 不删除）
                mergedModules.Add(baseMod);
                skipped++;
                messages.Add($"  [SKIP] Module {baseMod.Name}: kept baseline (removed in modified)");
                continue;
            }

            var modDiff = diff.ModuleDiffs.FirstOrDefault(d => d.ModuleName == baseMod.Name);
            var mergedMod = MergeModule(baseMod, modMod, modDiff, messages, ref applied, ref skipped);
            mergedModules.Add(mergedMod);
        }

        // 追加 only-in-modified 的模块（Added）
        foreach (var modMod in modified.Modules)
        {
            if (!baselineMods.ContainsKey(modMod.Name))
            {
                mergedModules.Add(modMod);
                applied++;
                messages.Add($"  [ADD] Module {modMod.Name}: added from modified");
            }
        }

        // 3. 构造 merged document
        var mergedDoc = new A2lDocument(
            mergedVersion, mergedProjectName, mergedProjectComment,
            mergedHeaderComment, mergedModCommon, mergedModules,
            "", 0);  // RawText="" → writer 走 semantic emit

        return new A2lMergeResult(mergedDoc, applied, skipped, messages);
    }

    // ====================================================================
    // 模块级合并
    // ====================================================================

    private static A2lModule MergeModule(
        A2lModule baseline, A2lModule modified,
        ModuleDiff? modDiff,
        List<string> messages, ref int applied, ref int skipped)
    {
        if (modDiff is null)
            return baseline;  // 无 diff → 完整保留

        var mergedMeas = MergeBlocks(
            baseline.Measurements, modified.Measurements,
            modDiff.MeasurementDiffs, m => m.Name, "MEASUREMENT", messages, ref applied, ref skipped);

        var mergedChars = MergeBlocks(
            baseline.Characteristics, modified.Characteristics,
            modDiff.CharacteristicDiffs, c => c.Name, "CHARACTERISTIC", messages, ref applied, ref skipped);

        var mergedAxisPts = MergeBlocks(
            baseline.AxisPts, modified.AxisPts,
            modDiff.AxisPtsDiffs, a => a.Name, "AXIS_PTS", messages, ref applied, ref skipped);

        var mergedAxisPtsX = MergeBlocks(
            baseline.AxisPtsX, modified.AxisPtsX,
            modDiff.AxisPtsXDiffs, a => a.Name, "AXIS_PTS_X", messages, ref applied, ref skipped);

        var mergedCompu = MergeBlocks(
            baseline.CompuMethods, modified.CompuMethods,
            modDiff.CompuMethodDiffs, c => c.Name, "COMPU_METHOD", messages, ref applied, ref skipped);

        var mergedLayouts = MergeBlocks(
            baseline.RecordLayouts, modified.RecordLayouts,
            modDiff.RecordLayoutDiffs, r => r.Name, "RECORD_LAYOUT", messages, ref applied, ref skipped);

        var mergedGroups = MergeBlocks(
            baseline.Groups, modified.Groups,
            modDiff.GroupDiffs, g => g.Name, "GROUP", messages, ref applied, ref skipped);

        // AXIS_DESCR — 按索引合并
        var mergedAxisDescr = MergeBlocksByIndex(
            baseline.AxisDescr, modified.AxisDescr,
            modDiff.AxisDescrDiffs, "AXIS_DESCR", messages, ref applied, ref skipped);

        var mergedRights = MergeBlocks(
            baseline.UserRights, modified.UserRights,
            modDiff.UserRightsDiffs, u => u.UserId, "USER_RIGHTS", messages, ref applied, ref skipped);

        var mergedVersions = MergeBlocks(
            baseline.VersionInfo, modified.VersionInfo,
            modDiff.VersionInfoDiffs, v => $"{v.Vendor}:{v.VersionNo}", "VERSION", messages, ref applied, ref skipped);

        // MOD_PAR — compared wins
        string? mergedModPar = modDiff.ModParChange is not null
            ? modified.ModPar  // compared wins
            : baseline.ModPar;

        // 如果完全无变化，返回原始 baseline（避免不必要的副本和集合引用差异）
        if (applied == 0 && skipped == 0)
            return baseline;

        var mergedModule = baseline with
        {
            Measurements = mergedMeas,
            Characteristics = mergedChars,
            AxisPts = mergedAxisPts,
            AxisPtsX = mergedAxisPtsX,
            CompuMethods = mergedCompu,
            RecordLayouts = mergedLayouts,
            Groups = mergedGroups,
            AxisDescr = mergedAxisDescr,
            UserRights = mergedRights,
            VersionInfo = mergedVersions,
            ModPar = mergedModPar,
        };

        return mergedModule;
    }

    // ====================================================================
    // 泛型块级合并
    // ====================================================================

    private static List<T> MergeBlocks<T>(
        IReadOnlyList<T> baseline, IReadOnlyList<T> modified,
        IReadOnlyList<BlockDiff> diffs,
        Func<T, string> keySelector, string blockType,
        List<string> messages, ref int applied, ref int skipped)
    {
        var diffByName = diffs.ToDictionary(d => d.BlockName, StringComparer.Ordinal);
        var modifiedByKey = modified.ToDictionary(keySelector, StringComparer.Ordinal);
        var baselineByKey = baseline.ToDictionary(keySelector, StringComparer.Ordinal);
        var result = new List<T>(baseline.Count + diffs.Count(d => d.Kind == DiffKind.Added));

        // 先处理 baseline 顺序（Unchanged/Modified/Removed 保持原位置）
        foreach (var item in baseline)
        {
            var name = keySelector(item);
            if (diffByName.TryGetValue(name, out var diff) && diff.Kind == DiffKind.Modified
                && modifiedByKey.TryGetValue(name, out var m))
            {
                result.Add(m);
                applied++;
                messages.Add($"  [UPD] {blockType} {name}: applied from modified");
            }
            else
            {
                result.Add(item);  // Unchanged 或 Removed → 保留 baseline
            }
        }

        // 追加 only-in-modified 的 block（Added）
        foreach (var item in modified)
        {
            var name = keySelector(item);
            if (!baselineByKey.ContainsKey(name))
            {
                result.Add(item);
                applied++;
                messages.Add($"  [ADD] {blockType} {name}: added from modified");
            }
        }

        return result;
    }

    /// <summary>按索引合并（AXIS_DESCR）。</summary>
    private static List<T> MergeBlocksByIndex<T>(
        IReadOnlyList<T> baseline, IReadOnlyList<T> modified,
        IReadOnlyList<BlockDiff> diffs, string blockType,
        List<string> messages, ref int applied, ref int skipped)
    {
        var result = new List<T>(diffs.Count);
        int idx = 0;

        foreach (var diff in diffs)
        {
            switch (diff.Kind)
            {
                case DiffKind.Unchanged:
                case DiffKind.Removed:
                    if (idx < baseline.Count)
                        result.Add(baseline[idx]);
                    break;
                case DiffKind.Modified:
                case DiffKind.Added:
                    if (idx < modified.Count)
                    {
                        result.Add(modified[idx]);
                        applied++;
                        messages.Add($"  [UPD] {blockType} #{idx} ({diff.BlockName}): applied from modified");
                    }
                    break;
            }
            idx++;
        }

        return result;
    }
}
