using System.IO;
using A2lEditor.Core.Testing;
using FluentAssertions;
using Xunit;

namespace A2lEditor.Core.Tests.Testing;

public class CoberturaReportTests
{
    private const string SampleCoberturaXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<coverage line-rate=""0.85"" branch-rate=""0.70"" lines-covered=""850"" lines-valid=""1000"" branches-covered=""70"" branches-valid=""100"" version=""1.9"" timestamp=""1700000000"">
  <packages>
    <package name=""A2lEditor.Core"" line-rate=""0.85"" branch-rate=""0.70"" />
  </packages>
</coverage>";

    [Fact]
    public void Parse_ValidXml_ExtractsLineRate()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, SampleCoberturaXml);
        try
        {
            var stats = CoberturaReport.Parse(path);
            stats.LineRate.Should().Be(0.85);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Parse_ValidXml_ExtractsBranchRate()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, SampleCoberturaXml);
        try
        {
            var stats = CoberturaReport.Parse(path);
            stats.BranchRate.Should().Be(0.70);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void MeetsThreshold_AboveThreshold_ReturnsTrue()
    {
        var stats = new CoverageStats(0.85, 0.75, 850, 1000, 75, 100);
        CoberturaReport.MeetsThreshold(stats, 0.80, 0.70).Should().BeTrue();
    }

    [Fact]
    public void MeetsThreshold_BelowThreshold_ReturnsFalse()
    {
        var stats = new CoverageStats(0.75, 0.65, 750, 1000, 65, 100);
        CoberturaReport.MeetsThreshold(stats, 0.80, 0.70).Should().BeFalse();
    }

    [Fact]
    public void MeetsThreshold_BothChecks_Required()
    {
        var stats = new CoverageStats(0.85, 0.65, 850, 1000, 65, 100);
        // Line 0.85 OK, branch 0.65 < 0.70 → false
        CoberturaReport.MeetsThreshold(stats, 0.80, 0.70).Should().BeFalse();
    }
}
