using System.CommandLine;

namespace A2lEditor.Cli.Commands.Merge;

public static class MergeRootCommand
{
    public static Command Create()
    {
        var cmd = new Command("merge", "A2L merge commands (v0.13)");
        cmd.Add(MergeApplyCommand.Create());
        return cmd;
    }
}
