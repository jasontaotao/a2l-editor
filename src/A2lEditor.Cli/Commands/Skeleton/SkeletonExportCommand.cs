using System.CommandLine;
using System.CommandLine.Invocation;
using A2lEditor.Core.Model;
using A2lEditor.Core.Parsing;
using A2lEditor.Core.Skeleton;

namespace A2lEditor.Cli.Commands.Skeleton;

public static class SkeletonExportCommand
{
    public static Command Create()
    {
        var a2lArg = new Argument<string>("a2l", "Path to .a2l file");
        var outputOpt = new Option<string?>(
            "--output", () => null, "Output .xlsx path (default: <a2l>.xlsx)");
        var sheetOpt = new Option<string?>(
            "--sheet", () => null, "Sheet name (default: Signals)");

        var cmd = new Command("export", "Export A2L signal definitions to Excel (.xlsx)")
        {
            a2lArg, outputOpt, sheetOpt
        };

        cmd.SetHandler((InvocationContext ctx) =>
        {
            var a2lPath = ctx.ParseResult.GetValueForArgument(a2lArg);
            var output = ctx.ParseResult.GetValueForOption(outputOpt);
            var sheet = ctx.ParseResult.GetValueForOption(sheetOpt);

            if (!File.Exists(a2lPath))
            {
                Console.Error.WriteLine($"File not found: {a2lPath}");
                ctx.ExitCode = 2; return Task.CompletedTask;
            }

            try
            {
                var doc = A2lDocument.LoadFromFile(a2lPath);

                var outPath = output ?? Path.ChangeExtension(a2lPath, ".xlsx");
                new A2lExcelExporter().Export(doc, outPath, sheet);

                var totalMeas = doc.TotalMeasurementCount;
                var totalChar = doc.Modules.Sum(m => m.Characteristics.Count);
                Console.WriteLine($"Exported {totalMeas} MEASUREMENTs, " +
                    $"{totalChar} CHARACTERISTICs from " +
                    $"{doc.Modules.Count} module(s)");
                Console.WriteLine($"Written to: {outPath}");
                ctx.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                ctx.ExitCode = 1;
            }

            return Task.CompletedTask;
        });

        return cmd;
    }
}
