using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using A2lEditor.Core.Model;

namespace A2lEditor.Core.Diff;

/// <summary>
/// 结构化 A2L diff 引擎。
/// 利用 C# record 内置值相等性做块级按名字匹配的差异分析。
/// </summary>
public sealed class A2lDiffService : IA2lDiffService
{
    private const int MaxFieldValueLength = 120;

    /// <summary>排除比较的属性名。</summary>
    private static readonly HashSet<string> ExcludeProperties = new(StringComparer.Ordinal)
    {
        "SourceLines",
        "EqualityContract",
    };

    /// <summary>默认格式化：null → "null"，否则截断。</summary>
    private static string Fmt(string? v) =>
        v is null ? "null" : Truncate(v);

    /// <summary>截断长值（适合字段值的显示）。</summary>
    internal static string Truncate(string s)
    {
        if (s.Length <= MaxFieldValueLength) return s;
        var truncated = s.Substring(0, MaxFieldValueLength);
        return truncated.Replace("\r\n", "↵").Replace("\n", "↵") + "...";
    }

    // ====================================================================
    // 顶层入口
    // ====================================================================

    public A2lDiffReport DiffDocuments(
        A2lDocument? baseline,
        A2lDocument? compared,
        string? baselinePath = null,
        string? comparedPath = null)
    {
        if (baseline is null && compared is null)
            return EmptyReport(baselinePath, comparedPath);

        if (baseline is null)
        {
            var added = AllModulesAdded(compared!);
            return new A2lDiffReport(
                baselinePath, comparedPath, DiffKind.Added,
                Array.Empty<FieldChange>(), added);
        }

        if (compared is null)
        {
            var removed = AllModulesRemoved(baseline);
            return new A2lDiffReport(
                baselinePath, comparedPath, DiffKind.Removed,
                Array.Empty<FieldChange>(), removed);
        }

        // 1. 文档级字段对比
        var docChanges = CompareDocumentFields(baseline, compared);

        // 2. 模块匹配
        var baselineMods = baseline.Modules.ToDictionary(m => m.Name, StringComparer.Ordinal);
        var comparedMods = compared.Modules.ToDictionary(m => m.Name, StringComparer.Ordinal);
        var allNames = new HashSet<string>(baselineMods.Keys.Concat(comparedMods.Keys), StringComparer.Ordinal);
        var moduleDiffs = new List<ModuleDiff>(allNames.Count);

        foreach (var name in allNames.OrderBy(n => n, StringComparer.Ordinal))
        {
            var hasLeft = baselineMods.TryGetValue(name, out var left);
            var hasRight = comparedMods.TryGetValue(name, out var right);

            if (!hasLeft)
            {
                // 右新增
                moduleDiffs.Add(BuildModuleAdded(right!));
            }
            else if (!hasRight)
            {
                // 左删除
                moduleDiffs.Add(BuildModuleRemoved(left!));
            }
            else
            {
                // 两边都有 → 内部对比
                moduleDiffs.Add(CompareModuleBlocks(left!, right!));
            }
        }

        var anyChange = docChanges.Any(c => !c.IsUnchanged)
                        || moduleDiffs.Any(m => m.Kind != DiffKind.Unchanged);
        var overallKind = anyChange ? DiffKind.Modified : DiffKind.Unchanged;

        return new A2lDiffReport(
            baselinePath, comparedPath, overallKind,
            docChanges, moduleDiffs);
    }

    // ====================================================================
    // 空/全增/全删 辅助
    // ====================================================================

    private static A2lDiffReport EmptyReport(string? bp, string? cp) =>
        new(bp, cp, DiffKind.Unchanged, Array.Empty<FieldChange>(), Array.Empty<ModuleDiff>());

    private static List<ModuleDiff> AllModulesAdded(A2lDocument doc) =>
        doc.Modules.Select(m => new ModuleDiff(
            m.Name, DiffKind.Added,
            AllBlocksAdded(m.Measurements, "MEASUREMENT", x => x.Name),
            AllBlocksAdded(m.Characteristics, "CHARACTERISTIC", x => x.Name),
            AllBlocksAdded(m.AxisPts, "AXIS_PTS", x => x.Name),
            AllBlocksAdded(m.AxisPtsX, "AXIS_PTS_X", x => x.Name),
            AllBlocksAdded(m.CompuMethods, "COMPU_METHOD", x => x.Name),
            AllBlocksAdded(m.RecordLayouts, "RECORD_LAYOUT", x => x.Name),
            AllBlocksAdded(m.Groups, "GROUP", x => x.Name),
            AllBlocksAdded(m.AxisDescr, "AXIS_DESCR", ad => ad.Attribute),
            AllBlocksAdded(m.UserRights, "USER_RIGHTS", x => x.UserId),
            AllBlocksAdded(m.VersionInfo, "VERSION", v => $"{v.Vendor}:{v.VersionNo}"),
            m.ModPar is null ? null : new FieldChange("ModPar", null, Truncate(m.ModPar)))
        ).ToList();

    private static List<ModuleDiff> AllModulesRemoved(A2lDocument doc) =>
        doc.Modules.Select(m => new ModuleDiff(
            m.Name, DiffKind.Removed,
            AllBlocksRemoved(m.Measurements, "MEASUREMENT", x => x.Name),
            AllBlocksRemoved(m.Characteristics, "CHARACTERISTIC", x => x.Name),
            AllBlocksRemoved(m.AxisPts, "AXIS_PTS", x => x.Name),
            AllBlocksRemoved(m.AxisPtsX, "AXIS_PTS_X", x => x.Name),
            AllBlocksRemoved(m.CompuMethods, "COMPU_METHOD", x => x.Name),
            AllBlocksRemoved(m.RecordLayouts, "RECORD_LAYOUT", x => x.Name),
            AllBlocksRemoved(m.Groups, "GROUP", x => x.Name),
            AllBlocksRemoved(m.AxisDescr, "AXIS_DESCR", ad => ad.Attribute),
            AllBlocksRemoved(m.UserRights, "USER_RIGHTS", x => x.UserId),
            AllBlocksRemoved(m.VersionInfo, "VERSION", v => $"{v.Vendor}:{v.VersionNo}"),
            m.ModPar is null ? null : new FieldChange("ModPar", Truncate(m.ModPar), null))
        ).ToList();

    private static List<BlockDiff> AllBlocksAdded<T>(
        IReadOnlyList<T> items, string blockType, Func<T, string> keySelector) =>
        items.Select(x => new BlockDiff(blockType, keySelector(x), DiffKind.Added, Array.Empty<FieldChange>())).ToList();

    private static List<BlockDiff> AllBlocksRemoved<T>(
        IReadOnlyList<T> items, string blockType, Func<T, string> keySelector) =>
        items.Select(x => new BlockDiff(blockType, keySelector(x), DiffKind.Removed, Array.Empty<FieldChange>())).ToList();

    private static List<BlockDiff> AllBlocksAddedByIndex<T>(
        IReadOnlyList<T> items, string blockType) =>
        items.Select((x, i) => new BlockDiff(blockType, $"#{i}", DiffKind.Added, Array.Empty<FieldChange>())).ToList();

    private static List<BlockDiff> AllBlocksRemovedByIndex<T>(
        IReadOnlyList<T> items, string blockType) =>
        items.Select((x, i) => new BlockDiff(blockType, $"#{i}", DiffKind.Removed, Array.Empty<FieldChange>())).ToList();

    // ====================================================================
    // 文档级字段对比（Version / ProjectName / ProjectComment / HeaderComment / ModCommon）
    // ====================================================================

    private static List<FieldChange> CompareDocumentFields(A2lDocument left, A2lDocument right)
    {
        var changes = new List<FieldChange>();

        AddIfChanged(changes, "Version", left.Version.ToString(), right.Version.ToString());
        AddIfChanged(changes, "ProjectName", left.ProjectName, right.ProjectName);
        AddIfChanged(changes, "ProjectComment", left.ProjectComment, right.ProjectComment);
        AddIfChanged(changes, "HeaderComment", left.HeaderComment, right.HeaderComment);

        // ModCommon — 嵌套 record，逐字段对比
        CompareModCommon(changes, left.ModCommon, right.ModCommon);

        return changes;
    }

    private static void CompareModCommon(
        List<FieldChange> changes, A2lModCommon? left, A2lModCommon? right)
    {
        if (left is null && right is null) return;
        if (left is null)
        {
            changes.Add(new FieldChange("ModCommon", "null", "present"));
            return;
        }
        if (right is null)
        {
            changes.Add(new FieldChange("ModCommon", "present", "null"));
            return;
        }

        AddIfChanged(changes, "ModCommon.Comment", left.Comment, right.Comment);
        AddIfChanged(changes, "ModCommon.ByteOrder", left.ByteOrder.ToString(), right.ByteOrder.ToString());
        AddIfChanged(changes, "ModCommon.DataSize",
            left.DataSize?.ToString() ?? "null",
            right.DataSize?.ToString() ?? "null");
        AddIfChanged(changes, "ModCommon.AlignmentByteOrder",
            left.AlignmentByteOrder?.ToString() ?? "null",
            right.AlignmentByteOrder?.ToString() ?? "null");
        AddIfChanged(changes, "ModCommon.AlignmentOffset",
            left.AlignmentOffset?.ToString() ?? "null",
            right.AlignmentOffset?.ToString() ?? "null");
    }

    // ====================================================================
    // 模块内部块级对比
    // ====================================================================

    private static ModuleDiff CompareModuleBlocks(A2lModule left, A2lModule right)
    {
        var meas = CompareBlockCollection(
            left.Measurements, right.Measurements,
            "MEASUREMENT", m => m.Name, CompareMeasurement);

        var chars = CompareBlockCollection(
            left.Characteristics, right.Characteristics,
            "CHARACTERISTIC", c => c.Name, CompareCharacteristic);

        var axisPts = CompareBlockCollection(
            left.AxisPts, right.AxisPts,
            "AXIS_PTS", a => a.Name, CompareAxisPts);

        var axisPtsX = CompareBlockCollection(
            left.AxisPtsX, right.AxisPtsX,
            "AXIS_PTS_X", a => a.Name, CompareAxisPtsX);

        var compu = CompareBlockCollection(
            left.CompuMethods, right.CompuMethods,
            "COMPU_METHOD", c => c.Name, CompareCompuMethod);

        var layouts = CompareBlockCollection(
            left.RecordLayouts, right.RecordLayouts,
            "RECORD_LAYOUT", r => r.Name, CompareRecordLayout);

        var groups = CompareBlockCollection(
            left.Groups, right.Groups,
            "GROUP", g => g.Name, CompareGroup);

        var axisDescr = CompareBlockCollection(
            left.AxisDescr, right.AxisDescr,
            "AXIS_DESCR", ad => ad.Attribute, CompareAxisDescr);

        var rights = CompareBlockCollection(
            left.UserRights, right.UserRights,
            "USER_RIGHTS", u => u.UserId, CompareUserRights);

        var versions = CompareBlockCollection(
            left.VersionInfo, right.VersionInfo,
            "VERSION", v => $"{v.Vendor}:{v.VersionNo}", CompareVersionInfo);

        // ModPar 字符串对比
        FieldChange? modParChange = null;
        if (!string.Equals(left.ModPar ?? "", right.ModPar ?? "", StringComparison.Ordinal))
        {
            modParChange = new FieldChange(
                "ModPar", Fmt(left.ModPar), Fmt(right.ModPar));
        }

        var allUnchanged = meas.All(b => b.Kind == DiffKind.Unchanged)
                         && chars.All(b => b.Kind == DiffKind.Unchanged)
                         && axisPts.All(b => b.Kind == DiffKind.Unchanged)
                         && axisPtsX.All(b => b.Kind == DiffKind.Unchanged)
                         && compu.All(b => b.Kind == DiffKind.Unchanged)
                         && layouts.All(b => b.Kind == DiffKind.Unchanged)
                         && groups.All(b => b.Kind == DiffKind.Unchanged)
                         && axisDescr.All(b => b.Kind == DiffKind.Unchanged)
                         && rights.All(b => b.Kind == DiffKind.Unchanged)
                         && versions.All(b => b.Kind == DiffKind.Unchanged)
                         && modParChange is null;

        var kind = allUnchanged ? DiffKind.Unchanged : DiffKind.Modified;

        return new ModuleDiff(
            left.Name, kind,
            meas, chars, axisPts, axisPtsX, compu, layouts, groups,
            axisDescr, rights, versions, modParChange);
    }

    // ====================================================================
    // 通用块级对比
    // ====================================================================

    private static List<BlockDiff> CompareBlockCollection<T>(
        IReadOnlyList<T> left, IReadOnlyList<T> right,
        string blockType, Func<T, string> keySelector,
        Func<T, T, List<FieldChange>> fieldComparer)
    {
        var leftByKey = left.ToDictionary(keySelector, StringComparer.Ordinal);
        var rightByKey = right.ToDictionary(keySelector, StringComparer.Ordinal);
        var allKeys = new HashSet<string>(
            leftByKey.Keys.Concat(rightByKey.Keys), StringComparer.Ordinal);

        var results = new List<BlockDiff>(allKeys.Count);
        foreach (var key in allKeys.OrderBy(k => k, StringComparer.Ordinal))
        {
            bool hasLeft = leftByKey.TryGetValue(key, out var leftItem);
            bool hasRight = rightByKey.TryGetValue(key, out var rightItem);

            if (!hasLeft)
            {
                results.Add(new BlockDiff(blockType, key, DiffKind.Added, Array.Empty<FieldChange>()));
            }
            else if (!hasRight)
            {
                results.Add(new BlockDiff(blockType, key, DiffKind.Removed, Array.Empty<FieldChange>()));
            }
            else
            {
                // 两边都有 — 利用 record 值相等性快速判断
                if (EqualityComparer<T>.Default.Equals(leftItem!, rightItem!))
                {
                    results.Add(new BlockDiff(blockType, key, DiffKind.Unchanged, Array.Empty<FieldChange>()));
                }
                else
                {
                    var changes = fieldComparer(leftItem!, rightItem!);
                    // Field comparer 排除 SourceLines；如果 SourceLines 是唯一差异，降级 Unchanged
                    var kind = changes.Count > 0 ? DiffKind.Modified : DiffKind.Unchanged;
                    results.Add(new BlockDiff(blockType, key, kind, changes));
                }
            }
        }
        return results;
    }

    /// <summary>按索引位置对比（适用无唯一 Name 的块类型，如 AXIS_DESCR）。</summary>
    private static List<BlockDiff> CompareBlockCollectionByIndex<T>(
        IReadOnlyList<T> left, IReadOnlyList<T> right,
        string blockType, Func<T, T, List<FieldChange>> fieldComparer)
    {
        var maxCount = Math.Max(left.Count, right.Count);
        var results = new List<BlockDiff>(maxCount);

        for (int i = 0; i < maxCount; i++)
        {
            var key = $"#{i}";
            if (i >= left.Count)
            {
                results.Add(new BlockDiff(blockType, key, DiffKind.Added, Array.Empty<FieldChange>()));
            }
            else if (i >= right.Count)
            {
                results.Add(new BlockDiff(blockType, key, DiffKind.Removed, Array.Empty<FieldChange>()));
            }
            else
            {
                if (EqualityComparer<T>.Default.Equals(left[i], right[i]))
                {
                    results.Add(new BlockDiff(blockType, key, DiffKind.Unchanged, Array.Empty<FieldChange>()));
                }
                else
                {
                    var changes = fieldComparer(left[i], right[i]);
                    var kind = changes.Count > 0 ? DiffKind.Modified : DiffKind.Unchanged;
                    results.Add(new BlockDiff(blockType, key, kind, changes));
                }
            }
        }
        return results;
    }

    // ====================================================================
    // 逐块类型字段对比
    // ====================================================================

    private static List<FieldChange> CompareMeasurement(A2lMeasurement left, A2lMeasurement right)
    {
        var c = new List<FieldChange>(8);
        AddIfChanged(c, "Name", left.Name, right.Name);
        AddIfChanged(c, "LongIdentifier", left.LongIdentifier, right.LongIdentifier);
        AddIfChanged(c, "DataType", left.DataType.ToString(), right.DataType.ToString());
        AddIfChanged(c, "CompuMethod", left.CompuMethod, right.CompuMethod);
        AddIfChanged(c, "Resolution", left.Resolution, right.Resolution);
        AddIfChanged(c, "Accuracy", left.Accuracy, right.Accuracy);
        AddIfChanged(c, "LowerLimit", left.LowerLimit, right.LowerLimit);
        AddIfChanged(c, "UpperLimit", left.UpperLimit, right.UpperLimit);
        AddIfChangedHex(c, "EcuAddress", left.EcuAddress, right.EcuAddress);
        return c;
    }

    private static List<FieldChange> CompareCharacteristic(A2lCharacteristic left, A2lCharacteristic right)
    {
        var c = new List<FieldChange>(9);
        AddIfChanged(c, "Name", left.Name, right.Name);
        AddIfChanged(c, "LongIdentifier", left.LongIdentifier, right.LongIdentifier);
        AddIfChanged(c, "Type", left.Type, right.Type);
        AddIfChanged(c, "RecordLayout", left.RecordLayout, right.RecordLayout);
        AddIfChangedHex(c, "EcuAddress", left.EcuAddress, right.EcuAddress);
        AddIfChanged(c, "LowerLimit", left.LowerLimit, right.LowerLimit);
        AddIfChanged(c, "UpperLimit", left.UpperLimit, right.UpperLimit);
        AddIfChanged(c, "MaxDiff", left.MaxDiff ?? "", right.MaxDiff ?? "");
        AddIfChanged(c, "Conversion", left.Conversion ?? "", right.Conversion ?? "");
        return c;
    }

    private static List<FieldChange> CompareAxisPts(A2lAxisPts left, A2lAxisPts right)
    {
        var c = new List<FieldChange>(9);
        AddIfChanged(c, "Name", left.Name, right.Name);
        AddIfChanged(c, "LongIdentifier", left.LongIdentifier, right.LongIdentifier);
        AddIfChanged(c, "RecordLayout", left.RecordLayout, right.RecordLayout);
        AddIfChangedHex(c, "EcuAddress", left.EcuAddress, right.EcuAddress);
        AddIfChanged(c, "InputQuantity", left.InputQuantity, right.InputQuantity);
        AddIfChanged(c, "CompuMethod", left.CompuMethod, right.CompuMethod);
        AddIfChangedInt(c, "NumberOfAxisPts", left.NumberOfAxisPts, right.NumberOfAxisPts);
        AddIfChanged(c, "LowerLimit", left.LowerLimit, right.LowerLimit);
        AddIfChanged(c, "UpperLimit", left.UpperLimit, right.UpperLimit);
        return c;
    }

    private static List<FieldChange> CompareAxisPtsX(A2lAxisPtsX left, A2lAxisPtsX right)
    {
        var c = new List<FieldChange>(9);
        AddIfChanged(c, "Name", left.Name, right.Name);
        AddIfChanged(c, "LongIdentifier", left.LongIdentifier, right.LongIdentifier);
        AddIfChanged(c, "RecordLayout", left.RecordLayout, right.RecordLayout);
        AddIfChangedHex(c, "EcuAddress", left.EcuAddress, right.EcuAddress);
        AddIfChanged(c, "InputQuantity", left.InputQuantity, right.InputQuantity);
        AddIfChanged(c, "CompuMethod", left.CompuMethod, right.CompuMethod);
        AddIfChangedInt(c, "MaxAxisPoints", left.MaxAxisPoints, right.MaxAxisPoints);
        AddIfChanged(c, "LowerLimit", left.LowerLimit, right.LowerLimit);
        AddIfChanged(c, "UpperLimit", left.UpperLimit, right.UpperLimit);
        return c;
    }

    private static List<FieldChange> CompareCompuMethod(A2lCompuMethod left, A2lCompuMethod right)
    {
        var c = new List<FieldChange>(11);
        AddIfChanged(c, "Name", left.Name, right.Name);
        AddIfChanged(c, "LongIdentifier", left.LongIdentifier, right.LongIdentifier);
        AddIfChanged(c, "ConversionType", left.ConversionType, right.ConversionType);
        AddIfChanged(c, "Format", left.Format, right.Format);
        AddIfChanged(c, "Unit", left.Unit, right.Unit);
        AddIfChangedDouble(c, "CoeffA", left.CoeffA, right.CoeffA);
        AddIfChangedDouble(c, "CoeffB", left.CoeffB, right.CoeffB);
        AddIfChangedDouble(c, "CoeffC", left.CoeffC, right.CoeffC);
        AddIfChangedDouble(c, "CoeffD", left.CoeffD, right.CoeffD);
        AddIfChangedDouble(c, "CoeffE", left.CoeffE, right.CoeffE);
        AddIfChangedDouble(c, "CoeffF", left.CoeffF, right.CoeffF);
        return c;
    }

    private static List<FieldChange> CompareRecordLayout(A2lRecordLayout left, A2lRecordLayout right)
    {
        var c = new List<FieldChange>(2);
        AddIfChanged(c, "Name", left.Name, right.Name);

        // Entries — IReadOnlyList<RecordLayoutEntry>
        var leftStr = FormatRecordLayoutEntries(left.Entries);
        var rightStr = FormatRecordLayoutEntries(right.Entries);
        if (!string.Equals(leftStr, rightStr, StringComparison.Ordinal))
        {
            c.Add(new FieldChange("Entries", Truncate(leftStr), Truncate(rightStr)));
        }
        return c;
    }

    private static List<FieldChange> CompareGroup(A2lGroup left, A2lGroup right)
    {
        var c = new List<FieldChange>(5);
        AddIfChanged(c, "Name", left.Name, right.Name);
        AddIfChanged(c, "LongIdentifier", left.LongIdentifier, right.LongIdentifier);
        AddIfChanged(c, "IsRoot", left.IsRoot.ToString(), right.IsRoot.ToString());

        // REF_MEASUREMENT
        var leftRefs = FormatStringList(left.RefMeasurements);
        var rightRefs = FormatStringList(right.RefMeasurements);
        if (!string.Equals(leftRefs, rightRefs, StringComparison.Ordinal))
            c.Add(new FieldChange("RefMeasurements", Truncate(leftRefs), Truncate(rightRefs)));

        // REF_CHARACTERISTIC
        var leftChars = FormatStringList(left.RefCharacteristics);
        var rightChars = FormatStringList(right.RefCharacteristics);
        if (!string.Equals(leftChars, rightChars, StringComparison.Ordinal))
            c.Add(new FieldChange("RefCharacteristics", Truncate(leftChars), Truncate(rightChars)));

        return c;
    }

    private static List<FieldChange> CompareAxisDescr(A2lAxisDescr left, A2lAxisDescr right)
    {
        var c = new List<FieldChange>(6);
        AddIfChanged(c, "Attribute", left.Attribute, right.Attribute);
        AddIfChanged(c, "InputQuantity", left.InputQuantity, right.InputQuantity);
        AddIfChanged(c, "Conversion", left.Conversion, right.Conversion);
        AddIfChangedInt(c, "MaxNumberOfAxisPoints", (int)left.MaxNumberOfAxisPoints, (int)right.MaxNumberOfAxisPoints);
        AddIfChanged(c, "LowerLimit", left.LowerLimit, right.LowerLimit);
        AddIfChanged(c, "UpperLimit", left.UpperLimit, right.UpperLimit);
        return c;
    }

    private static List<FieldChange> CompareUserRights(A2lUserRights left, A2lUserRights right)
    {
        var c = new List<FieldChange>(4);
        AddIfChanged(c, "UserId", left.UserId, right.UserId);
        AddIfChanged(c, "ReadAccess", left.ReadAccess, right.ReadAccess);
        AddIfChanged(c, "WriteAccess", left.WriteAccess, right.WriteAccess);
        AddIfChanged(c, "AccessMethod", left.AccessMethod, right.AccessMethod);
        return c;
    }

    private static List<FieldChange> CompareVersionInfo(A2lVersionInfo left, A2lVersionInfo right)
    {
        var c = new List<FieldChange>(4);
        AddIfChanged(c, "VersionNo", left.VersionNo, right.VersionNo);
        AddIfChanged(c, "Date", left.Date.ToString("yyyy-MM-dd"), right.Date.ToString("yyyy-MM-dd"));
        AddIfChanged(c, "Vendor", left.Vendor, right.Vendor);
        AddIfChanged(c, "Description", left.Description, right.Description);
        return c;
    }

    // ====================================================================
    // 格式化
    // ====================================================================

    private static string FormatStringList(IReadOnlyList<string> items) =>
        items.Count == 0 ? "[]" : "[" + string.Join(", ", items) + "]";

    private static string FormatRecordLayoutEntries(IReadOnlyList<RecordLayoutEntry> entries)
    {
        var parts = new string[entries.Count];
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            var s = $"{e.Keyword} Pos={e.Position} DT={e.DataType} IM={e.IndexMode} AM={e.AddressingMode}";
            if (e.IndexIncr.HasValue) s += $" INC={e.IndexIncr.Value}";
            if (e.IndexDecr.HasValue) s += $" DEC={e.IndexDecr.Value}";
            parts[i] = s;
        }
        return "[" + string.Join("; ", parts) + "]";
    }

    /// <summary>将模块做成 Added 状态（空 DiffReport 时用）。</summary>
    private static ModuleDiff BuildModuleAdded(A2lModule m) => new(
        m.Name, DiffKind.Added,
        AllBlocksAdded(m.Measurements, "MEASUREMENT", x => x.Name),
        AllBlocksAdded(m.Characteristics, "CHARACTERISTIC", x => x.Name),
        AllBlocksAdded(m.AxisPts, "AXIS_PTS", x => x.Name),
        AllBlocksAdded(m.AxisPtsX, "AXIS_PTS_X", x => x.Name),
        AllBlocksAdded(m.CompuMethods, "COMPU_METHOD", x => x.Name),
        AllBlocksAdded(m.RecordLayouts, "RECORD_LAYOUT", x => x.Name),
        AllBlocksAdded(m.Groups, "GROUP", x => x.Name),
        AllBlocksAdded(m.AxisDescr, "AXIS_DESCR", ad => ad.Attribute),
        AllBlocksAdded(m.UserRights, "USER_RIGHTS", x => x.UserId),
        AllBlocksAdded(m.VersionInfo, "VERSION", v => $"{v.Vendor}:{v.VersionNo}"),
        m.ModPar is null ? null : new FieldChange("ModPar", null, Truncate(m.ModPar)));

    /// <summary>将模块做成 Removed 状态。</summary>
    private static ModuleDiff BuildModuleRemoved(A2lModule m) => new(
        m.Name, DiffKind.Removed,
        AllBlocksRemoved(m.Measurements, "MEASUREMENT", x => x.Name),
        AllBlocksRemoved(m.Characteristics, "CHARACTERISTIC", x => x.Name),
        AllBlocksRemoved(m.AxisPts, "AXIS_PTS", x => x.Name),
        AllBlocksRemoved(m.AxisPtsX, "AXIS_PTS_X", x => x.Name),
        AllBlocksRemoved(m.CompuMethods, "COMPU_METHOD", x => x.Name),
        AllBlocksRemoved(m.RecordLayouts, "RECORD_LAYOUT", x => x.Name),
        AllBlocksRemoved(m.Groups, "GROUP", x => x.Name),
        AllBlocksRemoved(m.AxisDescr, "AXIS_DESCR", ad => ad.Attribute),
        AllBlocksRemoved(m.UserRights, "USER_RIGHTS", x => x.UserId),
        AllBlocksRemoved(m.VersionInfo, "VERSION", v => $"{v.Vendor}:{v.VersionNo}"),
        m.ModPar is null ? null : new FieldChange("ModPar", Truncate(m.ModPar), null));

    // ====================================================================
    // 字段级 Diff 辅助
    // ====================================================================

    private static void AddIfChanged(List<FieldChange> changes, string name, string left, string right)
    {
        if (!string.Equals(left, right, StringComparison.Ordinal))
            changes.Add(new FieldChange(name, Fmt(left), Fmt(right)));
    }

    private static void AddIfChangedHex(List<FieldChange> changes, string name, ulong left, ulong right)
    {
        if (left != right)
            changes.Add(new FieldChange(name, $"0x{left:X}", $"0x{right:X}"));
    }

    private static void AddIfChangedInt(List<FieldChange> changes, string name, int left, int right)
    {
        if (left != right)
            changes.Add(new FieldChange(name, left.ToString(CultureInfo.InvariantCulture), right.ToString(CultureInfo.InvariantCulture)));
    }

    private static void AddIfChangedDouble(List<FieldChange> changes, string name, double left, double right)
    {
        // ReSharper disable once CompareOfFloatsByEqualityOperator — intentional IEEE 754 bitwise for record parity
        if (left != right)
            changes.Add(new FieldChange(name,
                left.ToString("G", CultureInfo.InvariantCulture),
                right.ToString("G", CultureInfo.InvariantCulture)));
    }
}
