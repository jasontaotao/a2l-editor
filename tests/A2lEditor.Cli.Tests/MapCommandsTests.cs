using System.CommandLine;
using FluentAssertions;
using Xunit;

namespace A2lEditor.Cli.Tests;

public class MapCommandsTests
{
    [Fact]
    public async Task MapUpdate_DryRun_ExitsZero_DoesNotWriteFile()
    {
        var a2lPath = Path.Combine("samples", "BmsModel.a2l");
        var mapPath = Path.Combine("samples", "MiniMapFixture.iar.map");
        var backupPath = a2lPath + ".bak";

        var before = await File.ReadAllBytesAsync(a2lPath);
        var exitCode = await InvokeCli("map", "update", a2lPath, mapPath, "--dry-run");
        var after = await File.ReadAllBytesAsync(a2lPath);

        exitCode.Should().Be(0);
        after.Should().Equal(before, "dry-run must not modify the a2l file");
        File.Exists(backupPath).Should().BeFalse("dry-run must not create a backup");
    }

    [Fact]
    public async Task MapUpdate_MapNotFound_ExitsTwo()
    {
        var a2lPath = Path.Combine("samples", "BmsModel.a2l");
        var exitCode = await InvokeCli("map", "update", a2lPath, "samples/does_not_exist.map", "--dry-run");
        exitCode.Should().Be(2);
    }

    [Fact]
    public async Task MapDumpSymbols_OutputsAlignedTable()
    {
        var stdout = new System.IO.StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(stdout);
        try
        {
            var exitCode = await InvokeCli("map", "dump-symbols", "samples/MiniMapFixture.iar.map");
            exitCode.Should().Be(0);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
        var output = stdout.ToString();
        output.Should().Contain("Battery_Voltage");
        output.Should().Contain("0x8000000");
    }

    private static async Task<int> InvokeCli(params string[] args)
    {
        var root = new System.CommandLine.RootCommand
        {
            A2lEditor.Cli.Commands.Map.MapRootCommand.Create()
        };
        // Also register validate so help prints in standard shape.
        root.Add(A2lEditor.Cli.Commands.ValidateCommand.Create());
        return await root.InvokeAsync(args);
    }
}
