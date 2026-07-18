using A2lEditor.Reuse;
using FluentAssertions;
using Xunit;

namespace A2lEditor.Core.Tests.Map;

public class MapCoverageReportTests
{
    [Fact]
    public void MapCoverageReport_Constructor_StoresFields()
    {
        var extras = new[] { "A", "B" };
        var report = new MapCoverageReport(10, 8, 2, extras);
        report.TotalMapSymbols.Should().Be(10);
        report.MatchedInA2l.Should().Be(8);
        report.MissingFromA2l.Should().Be(2);
        report.ExtraInA2l.Should().BeEquivalentTo(extras);
    }

    [Fact]
    public void MapCoverageReport_EmptyInputs_HasZeroCounts()
    {
        var report = new MapCoverageReport(0, 0, 0, Array.Empty<string>());
        report.TotalMapSymbols.Should().Be(0);
        report.MatchedInA2l.Should().Be(0);
        report.MissingFromA2l.Should().Be(0);
        report.ExtraInA2l.Should().BeEmpty();
    }
}
