using System.CommandLine;
using System.CommandLine.Invocation;
using A2lEditor.Core.Diff;
using A2lEditor.Core.Merge;
using A2lEditor.Core.Model;
using A2lEditor.Core.Serialization;

namespace A2lEditor.Cli.Commands.Merge;

public static class MergeApplyCommand
{
    public static Command Create()
    {
        var baselineArg = new Argument<string>("baseline", "Baseline .a2l file");
        var modifiedArg = new Argument<string>("modified", "Modified .a2l file (compared-wins)");
        var outputOpt = new Option<string?>("--output", "Output path (default: <baseline>_merged.a2l)");
        var dryRunOpt = new Option<bool>("--dry-run", () => false, "Preview without writing");

        var cmd = new Command("apply", "Merge changes from modified into baseline")
        {
            baselineArg, modifiedArg, dryRunOpt, outputOpt
        };

        cmd.SetHandler((InvocationContext ctx) =>
        {
            var baselinePath = ctx.ParseResult.GetValueForArgument(baselineArg);
            var modifiedPath = ctx.ParseResult.GetValueForArgument(modifiedArg);
            var dryRun = ctx.ParseResult.GetValueForOption(dryRunOpt);
            var output = ctx.ParseResult.GetValueForOption(outputOpt);

            if (!File.Exists(baselinePath))
            {
                Console.Error.WriteLine($"Baseline file not found: {baselinePath}");
                ctx.ExitCode = 2; return Task.CompletedTask;
            }
            if (!File.Exists(modifiedPath))
            {
                Console.Error.WriteLine($"Modified file not found: {modifiedPath}");
                ctx.ExitCode = 2; return Task.CompletedTask;
            }

            try
            {
                var docBase = A2lDocument.LoadFromFile(baselinePath);
                var docMod = A2lDocument.LoadFromFile(modifiedPath);

                var service = new A2lMergeService(new A2lDiffService());
                var result = service.Merge(docBase, docMod, baselinePath, modifiedPath);

                Console.WriteLine($"A2L Merge: {Path.GetFileName(baselinePath)} ← {Path.GetFileName(modifiedPath)}");

                foreach (var msg in result.Messages)
                    Console.Error.WriteLine(msg);

                if (dryRun || result.MergedDocument is null)
                {
                    Console.WriteLine($"  Applied: {result.AppliedCount} changes (dry-run)");
                    ctx.ExitCode = 0; return Task.CompletedTask;
                }

                var outPath = output ?? Path.ChangeExtension(baselinePath, null) + "_merged.a2l";
                new A2lDocumentWriter().WriteToFile(result.MergedDocument, outPath);

                Console.WriteLine($"  Applied: {result.AppliedCount} changes");
                Console.WriteLine($"  Written to: {outPath}");
                ctx.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Merge error: {ex.Message}");
                ctx.ExitCode = 1;
            }

            return Task.CompletedTask;
        });

        return cmd;
    }
}
