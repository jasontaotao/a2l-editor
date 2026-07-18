using System.CommandLine;

namespace A2lEditor.Cli.Commands.Diff;

public static class DiffRootCommand
{
    public static Command Create()
    {
        var cmd = new Command("diff", "Compare two .a2l files structurally (v0.10)");
        cmd.Add(DiffCommand.Create());
        return cmd;
    }
}
