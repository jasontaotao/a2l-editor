using System.CommandLine;

namespace A2lEditor.Cli.Commands.Map;

public static class MapRootCommand
{
    public static Command Create()
    {
        var cmd = new Command("map", "MAP/ELF alignment commands (closes v0.9 deferred)");
        cmd.Add(MapDumpSymbolsCommand.Create());
        cmd.Add(MapUpdateCommand.Create());
        cmd.Add(MapValidateCommand.Create());
        return cmd;
    }
}
