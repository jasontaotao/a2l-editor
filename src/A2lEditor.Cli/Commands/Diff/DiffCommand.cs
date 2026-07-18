using System.CommandLine;
using System.CommandLine.Invocation;
using A2lEditor.Core.Diff;
using A2lEditor.Core.Model;
using A2lEditor.Core.Parsing;
using A2lEditor.Core.Serialization;

namespace A2lEditor.Cli.Commands.Diff;

public static class DiffCommand
{
    public static Command Create()
    {
        var file1Arg = new Argument<string>("file1", "First .a2l file (baseline)");
        var file2Arg = new Argument<string>("file2", "Second .a2l file (compared)");
        var briefOpt = new Option<bool>("--brief", () => false, "Only show summary, skip detail");
        var showUnchangedOpt = new Option<bool>("--unchanged", () => false, "Show unchanged entries too");

        var cmd = new Command("compare", "Compare two .a2l files")
        {
            file1Arg, file2Arg, briefOpt, showUnchangedOpt
        };

        cmd.SetHandler((InvocationContext ctx) =>
        {
            var file1 = ctx.ParseResult.GetValueForArgument(file1Arg);
            var file2 = ctx.ParseResult.GetValueForArgument(file2Arg);
            var brief = ctx.ParseResult.GetValueForOption(briefOpt);
            var showUnchanged = ctx.ParseResult.GetValueForOption(showUnchangedOpt);

            // 验证文件存在
            if (!File.Exists(file1))
            {
                Console.Error.WriteLine($"File not found: {file1}");
                ctx.ExitCode = 2;
                return Task.CompletedTask;
            }
            if (!File.Exists(file2))
            {
                Console.Error.WriteLine($"File not found: {file2}");
                ctx.ExitCode = 2;
                return Task.CompletedTask;
            }

            // 解析两个文件
            A2lDocument doc1, doc2;
            try
            {
                doc1 = A2lDocument.LoadFromFile(file1);
                doc2 = A2lDocument.LoadFromFile(file2);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Parse error: {ex.Message}");
                ctx.ExitCode = 1;
                return Task.CompletedTask;
            }

            // 对比
            var service = new A2lDiffService();
            var report = service.DiffDocuments(doc1, doc2, file1, file2);

            // 输出
            WriteReport(report, brief, showUnchanged);

            ctx.ExitCode = report.HasChanges ? 1 : 0;
            return Task.CompletedTask;
        });

        return cmd;
    }

    private static void WriteReport(A2lDiffReport report, bool brief, bool showUnchanged)
    {
        var short1 = Path.GetFileName(report.BaselinePath ?? "?");
        var short2 = Path.GetFileName(report.ComparedPath ?? "?");

        Console.WriteLine($"A2L Diff: {short1} ↔ {short2}");
        Console.WriteLine(new string('=', 50));
        Console.WriteLine();

        // 文档级变化
        var docChanges = report.DocumentChanges.Where(fc => !fc.IsUnchanged).ToList();
        if (docChanges.Count > 0)
        {
            Console.WriteLine("[DOCUMENT] — Modified");
            foreach (var fc in docChanges)
                Console.WriteLine($"    {fc.FieldName}: {fc.OldValue} → {fc.NewValue}");
            Console.WriteLine();
        }

        // 模块级
        foreach (var mod in report.ModuleDiffs)
        {
            var label = mod.Kind switch
            {
                DiffKind.Added => "Added",
                DiffKind.Removed => "Removed",
                DiffKind.Modified => "Modified",
                _ => "Unchanged",
            };

            // 统计本模块变化
            int added, removed, modified;
            CountModuleChanges(mod, out added, out removed, out modified);

            if (!showUnchanged && mod.Kind == DiffKind.Unchanged && added == 0 && removed == 0 && modified == 0)
                continue;

            Console.WriteLine($"[MODULE] {mod.ModuleName} — {label}");

            if (brief)
            {
                var parts = new List<string>();
                if (added > 0) parts.Add($"{added} Added");
                if (removed > 0) parts.Add($"{removed} Removed");
                if (modified > 0) parts.Add($"{modified} Modified");
                Console.WriteLine($"    {string.Join(", ", parts)}");
                Console.WriteLine();
                continue;
            }

            // MOD_PAR 变化
            if (mod.ModParChange is not null)
            {
                Console.WriteLine($"    ─ MOD_PAR — Modified");
                Console.WriteLine($"        {mod.ModParChange.OldValue} → {mod.ModParChange.NewValue}");
            }

            WriteBlockDiffs("MEASUREMENT", mod.MeasurementDiffs, showUnchanged);
            WriteBlockDiffs("CHARACTERISTIC", mod.CharacteristicDiffs, showUnchanged);
            WriteBlockDiffs("AXIS_PTS", mod.AxisPtsDiffs, showUnchanged);
            WriteBlockDiffs("AXIS_PTS_X", mod.AxisPtsXDiffs, showUnchanged);
            WriteBlockDiffs("COMPU_METHOD", mod.CompuMethodDiffs, showUnchanged);
            WriteBlockDiffs("RECORD_LAYOUT", mod.RecordLayoutDiffs, showUnchanged);
            WriteBlockDiffs("GROUP", mod.GroupDiffs, showUnchanged);
            WriteBlockDiffs("AXIS_DESCR", mod.AxisDescrDiffs, showUnchanged);
            WriteBlockDiffs("USER_RIGHTS", mod.UserRightsDiffs, showUnchanged);
            WriteBlockDiffs("VERSION", mod.VersionInfoDiffs, showUnchanged);

            Console.WriteLine();
        }

        // 汇总
        Console.WriteLine($"Summary: {report.TotalAdded} Added, {report.TotalRemoved} Removed, {report.TotalModified} Modified");
        if (!report.HasChanges)
            Console.WriteLine("All blocks unchanged.");
    }

    private static void WriteBlockDiffs(string blockType, IReadOnlyList<Core.Diff.BlockDiff> diffs, bool showUnchanged)
    {
        // 过滤
        var filtered = showUnchanged
            ? diffs
            : diffs.Where(d => d.Kind != DiffKind.Unchanged).ToList();

        if (filtered.Count == 0) return;

        var includeBlockType = filtered.Any(d =>
            !string.Equals(d.BlockType, blockType, StringComparison.OrdinalIgnoreCase));

        foreach (var block in filtered)
        {
            var label = block.Kind switch
            {
                DiffKind.Added => "Added",
                DiffKind.Removed => "Removed",
                DiffKind.Modified => "Modified",
                _ => "",
            };

            var typeLabel = includeBlockType ? $"{block.BlockType} " : "";
            Console.WriteLine($"    ─ {typeLabel}{block.BlockName} — {label}");

            foreach (var fc in block.FieldChanges)
            {
                Console.WriteLine($"        {fc.FieldName}: {fc.OldValue} → {fc.NewValue}");
            }
        }
    }

    private static void CountModuleChanges(ModuleDiff mod,
        out int added, out int removed, out int modified)
    {
        added = CountKind(mod.MeasurementDiffs, DiffKind.Added)
              + CountKind(mod.CharacteristicDiffs, DiffKind.Added)
              + CountKind(mod.AxisPtsDiffs, DiffKind.Added)
              + CountKind(mod.AxisPtsXDiffs, DiffKind.Added)
              + CountKind(mod.CompuMethodDiffs, DiffKind.Added)
              + CountKind(mod.RecordLayoutDiffs, DiffKind.Added)
              + CountKind(mod.GroupDiffs, DiffKind.Added)
              + CountKind(mod.AxisDescrDiffs, DiffKind.Added)
              + CountKind(mod.UserRightsDiffs, DiffKind.Added)
              + CountKind(mod.VersionInfoDiffs, DiffKind.Added);

        removed = CountKind(mod.MeasurementDiffs, DiffKind.Removed)
                + CountKind(mod.CharacteristicDiffs, DiffKind.Removed)
                + CountKind(mod.AxisPtsDiffs, DiffKind.Removed)
                + CountKind(mod.AxisPtsXDiffs, DiffKind.Removed)
                + CountKind(mod.CompuMethodDiffs, DiffKind.Removed)
                + CountKind(mod.RecordLayoutDiffs, DiffKind.Removed)
                + CountKind(mod.GroupDiffs, DiffKind.Removed)
                + CountKind(mod.AxisDescrDiffs, DiffKind.Removed)
                + CountKind(mod.UserRightsDiffs, DiffKind.Removed)
                + CountKind(mod.VersionInfoDiffs, DiffKind.Removed);

        modified = CountKind(mod.MeasurementDiffs, DiffKind.Modified)
                 + CountKind(mod.CharacteristicDiffs, DiffKind.Modified)
                 + CountKind(mod.AxisPtsDiffs, DiffKind.Modified)
                 + CountKind(mod.AxisPtsXDiffs, DiffKind.Modified)
                 + CountKind(mod.CompuMethodDiffs, DiffKind.Modified)
                 + CountKind(mod.RecordLayoutDiffs, DiffKind.Modified)
                 + CountKind(mod.GroupDiffs, DiffKind.Modified)
                 + CountKind(mod.AxisDescrDiffs, DiffKind.Modified)
                 + CountKind(mod.UserRightsDiffs, DiffKind.Modified)
                 + CountKind(mod.VersionInfoDiffs, DiffKind.Modified);
    }

    private static int CountKind(IReadOnlyList<Core.Diff.BlockDiff> diffs, DiffKind kind) =>
        diffs.Count(d => d.Kind == kind);
}
