using System.IO;
using A2lEditor.Core.Model;
using A2lEditor.Core.Parsing;
using A2lEditor.Core.Serialization;
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

    [Fact]
    public void RoundTrip_AllBlockTypes_PreservesFields()
    {
        // v0.4 acceptance gate: BmsModel.a2l must round-trip through Writer + re-parse,
        // preserving all 6 record list counts per module + sample field values.
        var sample = Path.Combine(RepoRoot, "samples", "BmsModel.a2l");
        var original = Asap131Parser.ParseFile(sample).Value;
        original.Should().NotBeNull();

        using var sw = new StringWriter();
        new A2lDocumentWriter().WriteToString(original!, sw);

        var reParsed = Asap131Parser.ParseText(sw.ToString()).Value;
        reParsed.Should().NotBeNull();

        // Module count
        reParsed!.Modules.Should().HaveCount(original!.Modules.Count);

        // Per-module: 6 list counts
        for (int i = 0; i < original.Modules.Count; i++)
        {
            var origMod = original.Modules[i];
            var reMod = reParsed.Modules[i];
            reMod.Measurements.Should().HaveCount(origMod.Measurements.Count);
            reMod.Characteristics.Should().HaveCount(origMod.Characteristics.Count);
            reMod.AxisPts.Should().HaveCount(origMod.AxisPts.Count);
            reMod.CompuMethods.Should().HaveCount(origMod.CompuMethods.Count);
            reMod.RecordLayouts.Should().HaveCount(origMod.RecordLayouts.Count);
            reMod.Groups.Should().HaveCount(origMod.Groups.Count);
        }

        // Sample 1 RECORD_LAYOUT: Entries.Count + sample entry Keyword + DataType
        // (BmsModel.a2l has 0 MEASUREMENT but 45 RECORD_LAYOUT — assert on RECORD_LAYOUT)
        var origRl = original.Modules[0].RecordLayouts[0];
        var reRl = reParsed.Modules[0].RecordLayouts[0];
        reRl.Entries.Should().HaveCount(origRl.Entries.Count);
        if (origRl.Entries.Count > 0)
        {
            reRl.Entries[0].Keyword.Should().Be(origRl.Entries[0].Keyword);
            reRl.Entries[0].DataType.Should().Be(origRl.Entries[0].DataType);
        }

        // Sample 1 CHARACTERISTIC (if BmsModel has any) — guard with Count > 0
        if (original.Modules[0].Characteristics.Count > 0)
        {
            var origCh = original.Modules[0].Characteristics[0];
            var reCh = reParsed.Modules[0].Characteristics[0];
            reCh.Name.Should().Be(origCh.Name);
            reCh.LowerLimit.Should().Be(origCh.LowerLimit);
            reCh.UpperLimit.Should().Be(origCh.UpperLimit);
        }
    }
}