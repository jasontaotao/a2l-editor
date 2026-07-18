using System.CommandLine;

namespace A2lEditor.Cli.Commands.Skeleton;

public static class SkeletonRootCommand
{
    public static Command Create()
    {
        var cmd = new Command("skeleton", "Generate A2L skeleton files (v0.14)");
        cmd.Add(SkeletonGenerateCommand.Create());
        return cmd;
    }
}
