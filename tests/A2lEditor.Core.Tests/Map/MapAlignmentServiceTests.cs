using A2lEditor.Core.Model;
using A2lEditor.Core.Parsing;
using A2lEditor.Core.Serialization;
using A2lEditor.Core.Services;
using A2lEditor.Reuse;
using FluentAssertions;
using Xunit;

namespace A2lEditor.Core.Tests.Map;

public class MapAlignmentServiceTests
{
    private readonly MapAlignmentService _sut = new(new NoopAdapter());

    private sealed class NoopAdapter : IMapSymbolTableAdapter
    {
        public IReadOnlyList<MapSymbol> LoadSymbols(string mapPath) =>
            Array.Empty<MapSymbol>();
    }

    private static A2lDocument DocWith(params A2lMeasurement[] ms) =>
        new(
            A2lVersion.V1_31, "P", "", "", null,
            new[]
            {
                new A2lModule(
                    "M", "", ms,
                    Array.Empty<A2lCharacteristic>(),
                    Array.Empty<A2lAxisPts>(),
                    Array.Empty<A2lCompuMethod>(),
                    Array.Empty<A2lRecordLayout>(),
                    Array.Empty<A2lGroup>(),
                    null,
                    Array.Empty<A2lAxisDescr>(),
                    Array.Empty<A2lUserRights>(),
                    Array.Empty<A2lVersionInfo>(),
                    Array.Empty<A2lAxisPtsX>(),
                    new LineRange(0, 0))
            },
            "", 0);

    [Fact]
    public void ValidateCoverage_AllMatched_ReturnsZeroMissing()
    {
        var syms = new[] { new MapSymbol("Battery_Voltage", 0x1000), new MapSymbol("Cell_Temp", 0x2000) };
        var doc = DocWith(
            new A2lMeasurement("Battery_Voltage", "", A2lDataType.UBYTE, "", "1", "1", "0", "100", 0, new LineRange(0, 0)),
            new A2lMeasurement("Cell_Temp", "", A2lDataType.UBYTE, "", "1", "1", "0", "100", 0, new LineRange(0, 0)));
        var report = _sut.ValidateCoverage(syms, doc);
        report.TotalMapSymbols.Should().Be(2);
        report.MatchedInA2l.Should().Be(2);
        report.MissingFromA2l.Should().Be(0);
        report.ExtraInA2l.Should().BeEmpty();
    }

    [Fact]
    public void ValidateCoverage_PartialMatch_ReturnsMissingAndExtra()
    {
        var syms = new[] { new MapSymbol("Battery_Voltage", 0x1000), new MapSymbol("Ghost_Symbol", 0x9000) };
        var doc = DocWith(
            new A2lMeasurement("Battery_Voltage", "", A2lDataType.UBYTE, "", "1", "1", "0", "100", 0, new LineRange(0, 0)),
            new A2lMeasurement("Unrelated_Measurement", "", A2lDataType.UBYTE, "", "1", "1", "0", "100", 0, new LineRange(0, 0)));
        var report = _sut.ValidateCoverage(syms, doc);
        report.TotalMapSymbols.Should().Be(2);
        report.MatchedInA2l.Should().Be(1);
        report.MissingFromA2l.Should().Be(1);
        report.ExtraInA2l.Should().Contain("Unrelated_Measurement");
    }

    [Fact]
    public void ApplyAddresses_MatchedMeasurements_UpdatesEcuAddress()
    {
        var syms = new[] { new MapSymbol("Battery_Voltage", 0xABCD) };
        var doc = DocWith(
            new A2lMeasurement("Battery_Voltage", "", A2lDataType.UBYTE, "", "1", "1", "0", "100", 0x0000, new LineRange(0, 0)));
        var result = _sut.ApplyAddresses(doc, syms, new MapApplyOptions(false, false, null));
        result.UpdatedCount.Should().Be(1);
        result.SkippedCount.Should().Be(0);
        result.NewDocument.Should().NotBeNull();
        result.NewDocument!.Modules[0].Measurements[0].EcuAddress.Should().Be(0xABCDUL);
    }

    [Fact]
    public void ApplyAddresses_DryRun_ReturnsNullNewDocument()
    {
        var syms = new[] { new MapSymbol("Battery_Voltage", 0xABCD) };
        var doc = DocWith(
            new A2lMeasurement("Battery_Voltage", "", A2lDataType.UBYTE, "", "1", "1", "0", "100", 0x0000, new LineRange(0, 0)));
        var result = _sut.ApplyAddresses(doc, syms, new MapApplyOptions(true, false, null));
        result.UpdatedCount.Should().Be(1);
        result.NewDocument.Should().BeNull();
    }

    [Fact]
    public void ApplyAddresses_Immutability_OriginalDocUnchanged()
    {
        var syms = new[] { new MapSymbol("Battery_Voltage", 0xABCD) };
        var origMeas = new A2lMeasurement("Battery_Voltage", "", A2lDataType.UBYTE, "", "1", "1", "0", "100", 0x0000, new LineRange(0, 0));
        var doc = DocWith(origMeas);
        _sut.ApplyAddresses(doc, syms, new MapApplyOptions(false, false, null));
        // Original doc's measurement EcuAddress unchanged.
        doc.Modules[0].Measurements[0].EcuAddress.Should().Be(0x0000UL);
    }

    // ====================================================================
    //  MAP2 regression — when the source document was loaded from a real
    //  .a2l file it carries the full RawText. A2lDocumentWriter short-circuits
    //  on non-empty RawText (Asap131DocumentWriter L18-22) and emits the
    //  original bytes verbatim. MapAlignmentService.ApplyAddresses used to
    //  spread results via "doc with { Modules = ... }" which PRESERVES
    //  RawText, so a real "map update --no-dry-run" wrote the file back
    //  UNCHANGED and the ECU_ADDRESS edits were silently lost.
    //  Existing unit tests missed this because their helper (DocWith) sets
    //  RawText="" — the writer then walks the semantic path and the loss
    //  never happens. This test loads a real doc with RawText to close the
    //  blind spot.
    // ====================================================================

    private const string RawDocWithEcuAddress =
        "ASAP2_VERSION  1 31\n"
        + "/begin PROJECT P \"comment\"\n"
        + " /begin MODULE M \"\"\n"
        + "  /begin MEASUREMENT VBatt \"\" UBYTE CM 0 0 0 255 ECU_ADDRESS 0x1000 /end MEASUREMENT\n"
        + " /end MODULE\n"
        + "/end PROJECT\n";

    [Fact]
    public void ApplyAddresses_OnDocumentWithRawText_PersistsEcuAddress_InWriterOutput()
    {
        // Real-world path: open a .a2l file (RawText populated) → apply → write.
        var doc = Asap131Parser.ParseText(RawDocWithEcuAddress).Value!;
        doc.Modules[0].Measurements[0].EcuAddress.Should().Be(0x1000UL);

        // Supply a map symbol that forces a NEW distinct address (0xDEAD).
        var syms = new[] { new MapSymbol("VBatt", 0xDEAD) };
        var result = _sut.ApplyAddresses(doc, syms, new MapApplyOptions(false, false, null));
        result.UpdatedCount.Should().Be(1);
        result.NewDocument.Should().NotBeNull();

        var sw = new System.IO.StringWriter();
        new A2lDocumentWriter().WriteToString(result.NewDocument!, sw);
        var written = sw.ToString();

        written.Should().Contain("ECU_ADDRESS 0xDEAD",
            "MAP2 regression: with RawText preserved, the writer short-circuited to "
            + "the original text and the address update was silently lost");
        written.Should().NotContain("ECU_ADDRESS 0x1000",
            "the old address must be replaced, not duplicated");
        // Round-trip sanity: semantic emit must still produce A2L the parser
        // can re-read with the new address intact (writer did not corrupt the
        // structure when forced off the RawText path).
        Asap131Parser.ParseText(written).Value!.Modules[0].Measurements[0]
            .EcuAddress.Should().Be(0xDEADUL);
    }
}
