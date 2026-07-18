using System.CommandLine;
using System.CommandLine.Invocation;
using A2lEditor.Core.Model;
using A2lEditor.Core.Services;
using A2lEditor.Reuse;
using Microsoft.Extensions.DependencyInjection;

namespace A2lEditor.Cli.Commands.Map;

public static class MapValidateCommand
{
    public static Command Create()
    {
        var a2lArg = new Argument<string>("a2l", "Path to .a2l file");
        var mapArg = new Argument<string>("map", "Path to MAP or ELF file");
        var cmd = new Command("validate", "Validate coverage between .a2l and MAP/ELF") { a2lArg, mapArg };

        cmd.SetHandler((InvocationContext ctx) =>
        {
            var a2lPath = ctx.ParseResult.GetValueForArgument(a2lArg);
            var mapPath = ctx.ParseResult.GetValueForArgument(mapArg);

            if (!File.Exists(a2lPath)) { Console.Error.WriteLine($"A2L file not found: {a2lPath}"); ctx.ExitCode = 2; return Task.CompletedTask; }
            if (!File.Exists(mapPath)) { Console.Error.WriteLine($"MAP file not found: {mapPath}"); ctx.ExitCode = 2; return Task.CompletedTask; }

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

                Console.WriteLine($"Total MAP symbols: {report.TotalMapSymbols}");
                Console.WriteLine($"Matched in .a2l:   {report.MatchedInA2l}");
                Console.WriteLine($"Missing from .a2l: {report.MissingFromA2l}");
                Console.WriteLine($"Extra in .a2l:     {report.ExtraInA2l.Count}");
                if (report.ExtraInA2l.Count > 0)
                    foreach (var n in report.ExtraInA2l) Console.WriteLine($"  + {n}");

                ctx.ExitCode = report.MissingFromA2l == 0 ? 0 : 1;
            }
            catch (InvalidMapException ex)
            {
                Console.Error.WriteLine(ex.Message);
                ctx.ExitCode = 1;
            }
            return Task.CompletedTask;
        });

        return cmd;
    }
}
