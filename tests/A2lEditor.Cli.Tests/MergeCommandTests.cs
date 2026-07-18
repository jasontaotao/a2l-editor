using System.CommandLine;
using FluentAssertions;
using Xunit;

namespace A2lEditor.Cli.Tests;

public class MergeCommandTests
{
    [Fact]
    public async Task MergeApply_DryRun_ExitsZero()
    {
        var exitCode = await InvokeMerge("--dry-run");
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task MergeApply_FileNotFound_ExitsTwo()
    {
        var root = new RootCommand
        {
            A2lEditor.Cli.Commands.Merge.MergeRootCommand.Create()
        };
        var exitCode = await root.InvokeAsync(new[]
            { "merge", "apply", "samples/nonexistent.a2l", "samples/minimal-diff.a2l" });
        exitCode.Should().Be(2);
    }

    [Fact]
    public async Task MergeApply_OutputExists_FileWritten()
    {
        var outPath = Path.GetTempFileName();
        try
        {
            var root = new RootCommand
            {
                A2lEditor.Cli.Commands.Merge.MergeRootCommand.Create()
            };
            var exitCode = await root.InvokeAsync(new[]
                { "merge", "apply", "samples/minimal-diff.a2l", "samples/minimal-diff-modified.a2l",
                  "--output", outPath });
            exitCode.Should().Be(0);
            File.Exists(outPath).Should().BeTrue();
            var content = File.ReadAllText(outPath);
            content.Should().Contain("ASAP2_VERSION");
        }
        finally
        {
            if (File.Exists(outPath)) File.Delete(outPath);
        }
    }

    private static async Task<int> InvokeMerge(params string[] extraArgs)
    {
        var root = new RootCommand
        {
            A2lEditor.Cli.Commands.Merge.MergeRootCommand.Create()
        };
        var args = new List<string> { "merge", "apply",
            "samples/minimal-diff.a2l", "samples/minimal-diff-modified.a2l" };
        args.AddRange(extraArgs);
        return await root.InvokeAsync(args.ToArray());
    }
}
