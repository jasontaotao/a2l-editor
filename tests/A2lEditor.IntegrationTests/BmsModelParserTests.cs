using System.IO;
using A2lEditor.Core.Model;
using A2lEditor.Core.Parsing;
using FluentAssertions;
using Xunit;

namespace A2lEditor.IntegrationTests;

public class BmsModelParserTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [Fact]
    public void Parse_BmsModel_ZeroWarnings_ModCommonExtracted()
    {
        // v0.3 acceptance gate: BmsModel.a2l must parse with 0 warnings
        // (v0.2 emitted "Warning L7:C16 Unknown block MOD_PAR, skipped").
        var sample = Path.Combine(RepoRoot, "samples", "BmsModel.a2l");
        var result = Asap131Parser.ParseFile(sample);
        result.HasErrors.Should().BeFalse(
            $"BmsModel.a2l should parse cleanly after v0.3; actual errors: {string.Join(", ", result.Errors.Select(e => $"L{e.Line}:{e.Message}"))}");
        result.Value.Should().NotBeNull();
        result.Value!.ModCommon.Should().NotBeNull("BmsModel.a2l contains MOD_COMMON block");
        result.Value!.ModCommon!.ByteOrder.Should().Be(A2lByteOrder.MSB_LAST);
        result.Value!.Modules[0].ModPar.Should().NotBeNullOrEmpty(
            "BmsModel.a2l's first MODULE contains a MOD_PAR block");
    }
}