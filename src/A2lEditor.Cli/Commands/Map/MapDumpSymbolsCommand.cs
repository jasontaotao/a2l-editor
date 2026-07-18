using System.CommandLine;
using System.CommandLine.Invocation;
using A2lEditor.Core.Services;
using A2lEditor.Reuse;
using Microsoft.Extensions.DependencyInjection;

namespace A2lEditor.Cli.Commands.Map;

public static class MapDumpSymbolsCommand
{
    public static Command Create()
    {
        var mapArg = new Argument<string>("map", "Path to MAP or ELF file");
        var cmd = new Command("dump-symbols", "List symbols from a MAP/ELF file") { mapArg };

        cmd.SetHandler((InvocationContext ctx) =>
        {
            var mapPath = ctx.ParseResult.GetValueForArgument(mapArg);

            var sc = new ServiceCollection();
            sc.AddSingleton<IMapSymbolTableAdapter, MapSymbolTableAdapter>();
            sc.AddSingleton<IMapAlignmentService, MapAlignmentService>();
            var sp = sc.BuildServiceProvider();

            try
            {
                var svc = sp.GetRequiredService<IMapAlignmentService>();
                var symbols = svc.LoadMapSymbols(mapPath);
                Console.WriteLine($"{"Name",-40}  {"Address",-18}");
                Console.WriteLine(new string('-', 60));
                foreach (var s in symbols.OrderBy(s => s.Name, StringComparer.Ordinal))
                    Console.WriteLine($"{s.Name,-40}  0x{s.Address:X}");
                ctx.ExitCode = 0;
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine($"MAP file not found: {ex.FileName}");
                ctx.ExitCode = 2;
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
