using System.CommandLine;
using System.CommandLine.Invocation;
using A2lEditor.Cli.Commands;
using A2lEditor.Cli.Commands.Diff;
using A2lEditor.Cli.Commands.Map;
using A2lEditor.Cli.Commands.Merge;
using A2lEditor.Cli.Commands.Skeleton;

var root = new RootCommand("a2l-editor — ASAP2 file editor (CLI)")
{
    ValidateCommand.Create(),
    MapRootCommand.Create(),
    MergeRootCommand.Create(),
    DiffRootCommand.Create(),
    SkeletonRootCommand.Create()
};

// System.CommandLine 2.0 beta4 auto-attaches a built-in `--version` option
// (via the UseDefaults middleware used by Command.InvokeAsync). The built-in
// prints `AssemblyInformationalVersion` / `AssemblyVersion`, so we set the
// MSBuild `InformationalVersion` (and `Version`) in the csproj to "0.1.1" so
// the output matches the spec. We deliberately do NOT add another `--version`
// option here — that would collide with the built-in (duplicate-key throw).
//
// --help is also added by the built-in middleware; --version follows the same
// pattern and prints on its own when invoked.

root.SetHandler((InvocationContext context) =>
{
    // No-args invocation: print top-level help-style usage banner.
    Console.WriteLine("a2l-editor — ASAP2 file editor (CLI)");
    Console.WriteLine();
    Console.WriteLine("Usage: a2l-editor [global options] <command>");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  validate <file>             Validate an .a2l file");
    Console.WriteLine("  diff compare <file1> <file2> Compare two .a2l files structurally");
    Console.WriteLine("  merge apply <baseline> <modified>  Merge changes (compared-wins)");
    Console.WriteLine("  skeleton generate <excel>    Generate A2L skeleton from Excel (.xlsx)");
    Console.WriteLine("  skeleton export <a2l>        Export A2L signal definitions to Excel (.xlsx)");
    Console.WriteLine("  map dump-symbols|update|validate   MAP/ELF alignment");
    Console.WriteLine();
    Console.WriteLine("Run 'a2l-editor <command> --help' for more information about a command.");
    return Task.CompletedTask;
});

return await root.InvokeAsync(args);