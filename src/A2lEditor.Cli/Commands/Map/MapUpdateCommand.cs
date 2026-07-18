using System.CommandLine;
using System.CommandLine.Invocation;
using A2lEditor.Core.Model;
using A2lEditor.Core.Serialization;
using A2lEditor.Core.Services;
using A2lEditor.Reuse;
using Microsoft.Extensions.DependencyInjection;

namespace A2lEditor.Cli.Commands.Map;

public static class MapUpdateCommand
{
    public static Command Create()
    {
        var a2lArg = new Argument<string>("a2l", "Path to .a2l file");
        var mapArg = new Argument<string>("map", "Path to MAP or ELF file");
        var dryRunOpt = new Option<bool>("--dry-run", () => true, "Preview without writing (default: true)");
        var noDryRunOpt = new Option<bool>("--no-dry-run", "Disable dry-run; actually write the file");
        var backupOpt = new Option<bool>("--backup", "Write <a2l>.bak before applying");
        var outputOpt = new Option<string?>("--output", "Output path (default: in-place)");

        var cmd = new Command("update", "Apply MAP/ELF addresses to an .a2l file")
        {
            a2lArg, mapArg, dryRunOpt, noDryRunOpt, backupOpt, outputOpt
        };

        cmd.SetHandler((InvocationContext ctx) =>
        {
            var a2lPath = ctx.ParseResult.GetValueForArgument(a2lArg);
            var mapPath = ctx.ParseResult.GetValueForArgument(mapArg);
            var dryRun = ctx.ParseResult.GetValueForOption(dryRunOpt);
            var noDryRun = ctx.ParseResult.GetValueForOption(noDryRunOpt);
            var backup = ctx.ParseResult.GetValueForOption(backupOpt);
            var output = ctx.ParseResult.GetValueForOption(outputOpt);

            if (dryRun && noDryRun)
            {
                Console.Error.WriteLine("Cannot combine --dry-run and --no-dry-run.");
                ctx.ExitCode = 2;
                return Task.CompletedTask;
            }

            if (!File.Exists(a2lPath))
            {
                Console.Error.WriteLine($"A2L file not found: {a2lPath}");
                ctx.ExitCode = 2;
                return Task.CompletedTask;
            }

            if (!File.Exists(mapPath))
            {
                Console.Error.WriteLine($"MAP file not found: {mapPath}");
                ctx.ExitCode = 2;
                return Task.CompletedTask;
            }

            var sc = new ServiceCollection();
            sc.AddSingleton<IMapSymbolTableAdapter, MapSymbolTableAdapter>();
            sc.AddSingleton<IMapAlignmentService, MapAlignmentService>();
            var sp = sc.BuildServiceProvider();

            try
            {
                var svc = sp.GetRequiredService<IMapAlignmentService>();
                var symbols = svc.LoadMapSymbols(mapPath);
                var doc = A2lDocument.LoadFromFile(a2lPath);

                var report = svc.ValidateCoverage(symbols, doc);
                Console.Error.WriteLine($"[INFO] Loaded {symbols.Count} symbols from {mapPath}");
                Console.Error.WriteLine($"[INFO] Coverage: {report.MatchedInA2l}/{report.TotalMapSymbols} matched ({(report.TotalMapSymbols > 0 ? 100.0 * report.MatchedInA2l / report.TotalMapSymbols : 0):F1}%); {report.MissingFromA2l} missing, {report.ExtraInA2l.Count} extra");

                var effectiveDryRun = !noDryRun;
                var result = svc.ApplyAddresses(doc, symbols, new MapApplyOptions(effectiveDryRun, backup, output));

                if (effectiveDryRun)
                {
                    Console.Error.WriteLine($"[INFO] Dry-run: would update {result.UpdatedCount} MEASUREMENT.ECU_ADDRESS fields; {result.SkippedCount} skipped");
                    ctx.ExitCode = 0;
                    return Task.CompletedTask;
                }

                if (result.NewDocument is null)
                {
                    Console.Error.WriteLine("Apply returned no document.");
                    ctx.ExitCode = 1;
                    return Task.CompletedTask;
                }

                if (backup && string.Equals(output ?? a2lPath, a2lPath, StringComparison.OrdinalIgnoreCase))
                    File.Copy(a2lPath, a2lPath + ".bak", overwrite: true);

                var writer = new A2lDocumentWriter();
                var target = output ?? a2lPath;
                using (var fs = File.CreateText(target))
                    writer.WriteToString(result.NewDocument, fs);

                Console.Error.WriteLine($"[OK] Updated {result.UpdatedCount} fields; written to {target}");
                ctx.ExitCode = 0;
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine($"File not found: {ex.FileName}");
                ctx.ExitCode = 2;
            }
            catch (InvalidMapException ex)
            {
                Console.Error.WriteLine(ex.Message);
                ctx.ExitCode = 1;
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine($"I/O error: {ex.Message}");
                ctx.ExitCode = 2;
            }
            return Task.CompletedTask;
        });

        return cmd;
    }
}
