using System.CommandLine;

namespace A2lEditor.Cli.Commands.Skeleton;

public static class SkeletonRootCommand
{
    public static Command Create()
    {
        var cmd = new Command("skeleton", "Generate A2L skeleton files / export to Excel (v0.17)");
        cmd.Add(SkeletonGenerateCommand.Create());
        cmd.Add(SkeletonExportCommand.Create());
        return cmd;
    }
}
