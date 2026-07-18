using System.CommandLine;
using System.CommandLine.Invocation;
using A2lEditor.Core.Serialization;
using A2lEditor.Core.Skeleton;

namespace A2lEditor.Cli.Commands.Skeleton;

public static class SkeletonGenerateCommand
{
    public static Command Create()
    {
        var excelArg = new Argument<string>("excel", "Path to .xlsx file");
        var outputOpt = new Option<string?>(
            "--output", () => null, "Output .a2l path (default: <excel>.a2l)");
        var sheetOpt = new Option<string?>(
            "--sheet", () => null, "Sheet name (default: first sheet)");
        var moduleOpt = new Option<string>(
            "--module", () => "ImportedModule", "Module name");
        var commentOpt = new Option<string>(
            "--comment", () => "Generated from Excel", "Module comment");

        var cmd = new Command("generate", "Generate A2L skeleton from Excel (.xlsx)")
        {
            excelArg, outputOpt, sheetOpt, moduleOpt, commentOpt
        };

        cmd.SetHandler((InvocationContext ctx) =>
        {
            var excelPath = ctx.ParseResult.GetValueForArgument(excelArg);
            var output = ctx.ParseResult.GetValueForOption(outputOpt);
            var sheet = ctx.ParseResult.GetValueForOption(sheetOpt);
            var moduleName = ctx.ParseResult.GetValueForOption(moduleOpt);
            var comment = ctx.ParseResult.GetValueForOption(commentOpt);

            if (!File.Exists(excelPath))
            {
                Console.Error.WriteLine($"File not found: {excelPath}");
                ctx.ExitCode = 2; return Task.CompletedTask;
            }

            try
            {
                var service = new A2lSkeletonService();
                var options = new SkeletonGenerateOptions(sheet, moduleName ?? "ImportedModule", comment ?? "Generated from Excel");
                var doc = service.GenerateFromExcel(excelPath, options);

                var outPath = output ?? Path.ChangeExtension(excelPath, ".a2l");
                new A2lDocumentWriter().WriteToFile(doc, outPath);

                Console.WriteLine($"Generated {doc.TotalMeasurementCount} MEASUREMENTs, " +
                    $"{doc.Modules[0].Characteristics.Count} CHARACTERISTICs, " +
                    $"{doc.Modules[0].CompuMethods.Count} COMPU_METHODs");
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
