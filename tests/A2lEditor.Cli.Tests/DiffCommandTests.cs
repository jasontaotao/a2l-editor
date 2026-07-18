using System.CommandLine;
using FluentAssertions;
using Xunit;

namespace A2lEditor.Cli.Tests;

public class DiffCommandTests
{
    [Fact]
    public async Task DiffCommand_IdenticalFiles_ExitsZero()
    {
        var exitCode = await InvokeDiff(MinimalPath, MinimalPath);
        exitCode.Should().Be(0, "identical files should exit 0");
    }

    [Fact]
    public async Task DiffCommand_DifferentFiles_ExitsOne()
    {
        var exitCode = await InvokeDiff(MinimalPath, ModifiedPath);
        exitCode.Should().Be(1, "different files should exit 1");
    }

    [Fact]
    public async Task DiffCommand_FileNotFound_ExitsTwo()
    {
        var exitCode = await InvokeDiff(MinimalPath, "samples/nonexistent.a2l");
        exitCode.Should().Be(2, "missing file should exit 2");
    }

    [Fact]
    public async Task DiffCommand_OutputContainsBlockName()
    {
        var stdout = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(stdout);
        try
        {
            var exitCode = await InvokeDiff(MinimalPath, ModifiedPath);
            exitCode.Should().Be(1);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
        var output = stdout.ToString();
        output.Should().Contain("V", "output should contain changed block name");
    }

    private static async Task<int> InvokeDiff(string file1, string file2)
    {
        var root = new RootCommand
        {
            A2lEditor.Cli.Commands.Diff.DiffRootCommand.Create()
        };
        return await root.InvokeAsync(new[] { "diff", "compare", file1, file2 });
    }

    private static readonly string MinimalPath =
        Path.Combine("samples", "minimal-diff.a2l");

    private static readonly string ModifiedPath =
        Path.Combine("samples", "minimal-diff-modified.a2l");
}
