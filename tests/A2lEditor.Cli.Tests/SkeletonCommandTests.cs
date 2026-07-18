using System.CommandLine;
using ClosedXML.Excel;
using FluentAssertions;
using Xunit;

namespace A2lEditor.Cli.Tests;

public class SkeletonCommandTests : IDisposable
{
    private readonly string _excelPath;

    public SkeletonCommandTests()
    {
        _excelPath = System.IO.Path.GetTempFileName() + ".xlsx";
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "Name";
        ws.Cell(2, 1).Value = "V";
        ws.Cell(2, 4).Value = "UBYTE";
        ws.Cell(3, 1).Value = "I";
        ws.Cell(3, 4).Value = "UWORD";
        workbook.SaveAs(_excelPath);
    }

    public void Dispose()
    {
        try { if (File.Exists(_excelPath)) File.Delete(_excelPath); } catch { }
    }

    [Fact]
    public async Task SkeletonGenerate_ExitsZero()
    {
        var outputPath = System.IO.Path.GetTempFileName() + ".a2l";
        try
        {
            var exitCode = await InvokeSkeleton(new[]
                { "skeleton", "generate", _excelPath, "--output", outputPath });
            exitCode.Should().Be(0);
            File.Exists(outputPath).Should().BeTrue();
            var content = File.ReadAllText(outputPath);
            content.Should().Contain("ASAP2_VERSION");
            content.Should().Contain("V");
        }
        finally
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task SkeletonGenerate_FileNotFound_ExitsTwo()
    {
        var root = new RootCommand
        {
            A2lEditor.Cli.Commands.Skeleton.SkeletonRootCommand.Create()
        };
        var exitCode = await root.InvokeAsync(new[]
            { "skeleton", "generate", "samples/nonexistent.xlsx" });
        exitCode.Should().Be(2);
    }

    [Fact]
    public async Task SkeletonExport_ExitsZero()
    {
        var outputPath = System.IO.Path.GetTempFileName() + ".xlsx";
        try
        {
            var exitCode = await InvokeSkeleton(new[]
                { "skeleton", "export", "samples/minimal-diff.a2l", "--output", outputPath });
            exitCode.Should().Be(0);
            File.Exists(outputPath).Should().BeTrue();
            new FileInfo(outputPath).Length.Should().BeGreaterThan(0);
        }
        finally
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task SkeletonExport_FileNotFound_ExitsTwo()
    {
        var exitCode = await InvokeSkeleton(new[]
            { "skeleton", "export", "samples/nonexistent.a2l" });
        exitCode.Should().Be(2);
    }

    private static async Task<int> InvokeSkeleton(string[] args)
    {
        var root = new RootCommand
        {
            A2lEditor.Cli.Commands.Skeleton.SkeletonRootCommand.Create()
        };
        return await root.InvokeAsync(args);
    }
}
