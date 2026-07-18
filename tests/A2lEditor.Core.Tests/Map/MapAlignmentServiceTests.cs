using A2lEditor.Core.Model;
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
}
