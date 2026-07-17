using A2lEditor.Core.Model;
using A2lEditor.Core.Parsing;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace A2lEditor.Core.Parsing.Tests;

public class Asap131ParserTests
{
    private readonly ITestOutputHelper _out;
    public Asap131ParserTests(ITestOutputHelper o) { _out = o; }

    [Fact]
    public void DIAG_LexerMinimalInputTokens()
    {
        var text = """ASAP2_VERSION  1 31""";
        var lexer = new Asap131Lexer(text);
        var tokens = lexer.Tokenize();
        foreach (var t in tokens)
            _out.WriteLine($"  [{t.Line}:{t.Column}] {t.Kind} '{t.Text}'");
    }

    [Fact]
    public void DIAG_LexerFullMinimalProject()
    {
        var text = """
            ASAP2_VERSION  1 31
            /begin PROJECT P "comment"
             /begin MODULE M ""
              /begin RECORD_LAYOUT R
               FNC_VALUES 1 UBYTE COLUMN_DIR DIRECT
              /end RECORD_LAYOUT
              /begin MEASUREMENT m1 "desc" UBYTE CM 0 0 0 255 ECU_ADDRESS 0x1000
              /end MEASUREMENT
             /end MODULE
            /end PROJECT
            """;
        var lexer = new Asap131Lexer(text);
        var tokens = lexer.Tokenize();
        foreach (var t in tokens)
            _out.WriteLine($"  [{t.Line}:{t.Column}] {t.Kind} '{t.Text}'");
    }

    [Fact]
    public void DIAG_JustKeywordTokens()
    {
        var text = "/begin PROJECT P /end";
        var lexer = new Asap131Lexer(text);
        var tokens = lexer.Tokenize();
        foreach (var t in tokens)
            _out.WriteLine($"  [{t.Line}:{t.Column}] {t.Kind} '{t.Text}'");
        // Check: is "PROJECT" Keyword or Identifier?
    }

    [Fact]
    public void DIAG_TracePositionAfterParse()
    {
        var text = """
            ASAP2_VERSION  1 31
            /begin PROJECT P "comment"
             /begin MODULE M ""
              /begin MEASUREMENT m1 "desc"
              /end MEASUREMENT
             /end MODULE
            /end PROJECT
            """;
        var result = Asap131Parser.ParseText(text);
        _out.WriteLine($"HasErrors={result.HasErrors}");
        _out.WriteLine($"Modules.Count={result.Value?.Modules?.Count ?? -1}");
        _out.WriteLine($"ProjectName={result.Value?.ProjectName ?? "<null>"}");
    }

    [Fact]
    public void DIAG_BmsModelOutcome()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "samples", "BmsModel.a2l");
        var result = Asap131Parser.ParseFile(path);
        _out.WriteLine($"HasErrors={result.HasErrors} Errors={result.Errors.Count}");
        _out.WriteLine($"Modules.Count={result.Value?.Modules?.Count ?? -1}");
        _out.WriteLine($"ProjectName={result.Value?.ProjectName ?? "<null>"}");
        var m = result.Value?.Modules?.FirstOrDefault();
        if (m != null)
        {
            _out.WriteLine($"  Measurements={m.Measurements.Count} Chars={m.Characteristics.Count} RecordLayouts={m.RecordLayouts.Count}");
        }
        foreach (var e in result.Errors.Take(30))
            _out.WriteLine($"  [{e.Line}:{e.Column}] {e.Severity} {e.Message}");
    }

    [Fact]
    public void DIAG_SimpleModuleWithUnknownBlock()
    {
        var text = """
            ASAP2_VERSION  1 31
            /begin PROJECT P "cmt"
             /begin MODULE M "cmt"
              /begin MOD_PAR "x"
              /end MOD_PAR
              /begin MEASUREMENT m1 "d" UBYTE CM 0 0 0 255
              /end MEASUREMENT
             /end MODULE
            /end PROJECT
            """;
        var result = Asap131Parser.ParseText(text);
        _out.WriteLine($"HasErrors={result.HasErrors} Modules={result.Value?.Modules?.Count} Measurements={result.Value?.Modules?.FirstOrDefault()?.Measurements?.Count}");
    }

    [Fact]
    public void ParseText_MinimalProject_ReturnsSuccess()
    {
        var text = """
            ASAP2_VERSION  1 31
            /begin PROJECT P "comment"
             /begin MODULE M ""
              /begin RECORD_LAYOUT R
               FNC_VALUES 1 UBYTE COLUMN_DIR DIRECT
              /end RECORD_LAYOUT
              /begin MEASUREMENT m1 "desc" UBYTE CM 0 0 0 255 ECU_ADDRESS 0x1000
              /end MEASUREMENT
             /end MODULE
            /end PROJECT
            """;
        var result = Asap131Parser.ParseText(text);
        result.HasErrors.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value!.ProjectName.Should().Be("P");
        result.Value.Modules.Should().HaveCount(1);
        result.Value.Modules[0].Measurements.Should().HaveCount(1);
        result.Value.Modules[0].Measurements[0].Name.Should().Be("m1");
        result.Value.Modules[0].Measurements[0].EcuAddress.Should().Be(0x1000UL);
    }

    [Fact]
    public void ParseFile_BmsModel_ReturnsValidDocument()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "samples", "BmsModel.a2l");
        var result = Asap131Parser.ParseFile(path);
        result.Value.Should().NotBeNull();
        result.Value!.Modules.Should().NotBeEmpty();
    }

    [Fact]
    public void ParseFile_InvalidSample_ReturnsErrors()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "samples", "invalid-sample.a2l");
        var result = Asap131Parser.ParseFile(path);
        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public void ParseText_PreservesRawText()
    {
        var text = "ASAP2_VERSION  1 31\n";
        var result = Asap131Parser.ParseText(text);
        result.Value!.RawText.Should().Be(text);
    }

    [Fact]
    public void Parse_ModuleWithModPar_StoresModParComment()
    {
        const string text = "ASAP2_VERSION 1 31\n"
            + "/begin PROJECT P\n"
            + " /begin MODULE M \"\"\n"
            + "  /begin MOD_PAR \"my mod par comment\" /end MOD_PAR\n"
            + " /end MODULE\n"
            + "/end PROJECT\n";
        var result = Asap131Parser.ParseText(text);
        result.HasErrors.Should().BeFalse(
            $"no errors expected; actual: {string.Join("; ", result.Errors.Select(e => $"L{e.Line}:{e.Message}"))}");
        result.Value!.Modules.Should().HaveCount(1);
        result.Value!.Modules[0].ModPar.Should().Be("my mod par comment");
    }

    [Fact]
    public void Parse_SkipToMatchingEnd_ConsumesEndBlockName_NoLeak()
    {
        // Regression test for Plan v0.1.1 verify-bug.md Risks #1:
        // SkipToMatchingEnd previously exited with pos on the terminating /end
        // token, leaking the block-name. After fix, pos should be past /end BLOCK_NAME,
        // and the MEASUREMENT after the unknown block must still parse correctly.
        //
        // Note: parser switch-default at L148 emits ErrorSeverity.Warning for any
        // unknown block (UNKNOWN_X, UNKNOWN_Y). Those warnings are EXPECTED per
        // spec section 1.3 / 4.5 (only MOD_PAR/MOD_COMMON warnings are closed in
        // v0.3; arbitrary unknown blocks like UNKNOWN_X remain as warnings).
        // This test asserts on the FIX's real symptom: MEASUREMENT count.
        const string text = "ASAP2_VERSION 1 31\n"
            + "/begin PROJECT P\n"
            + " /begin MODULE M \"\"\n"
            + "  /begin UNKNOWN_X\n"
            + "   /begin UNKNOWN_Y /end UNKNOWN_Y\n"
            + "  /end UNKNOWN_X\n"
            + "  /begin MEASUREMENT meas1 \"\" UBYTE CM 0 0 0 255 /end MEASUREMENT\n"
            + " /end MODULE\n"
            + "/end PROJECT\n";
        var result = Asap131Parser.ParseText(text);
        result.Value!.Modules.Should().HaveCount(1);
        result.Value!.Modules[0].Measurements.Should().HaveCount(1,
            "MEASUREMENT after skipped UNKNOWN_X block must be parsed correctly (verify-bug.md Risks #1)");
        result.Value!.Modules[0].Measurements[0].Name.Should().Be("meas1");
    }

    [Fact]
    public void Parse_ProjectWithModCommon_StoresModCommonAndByteOrder()
    {
        const string text = "ASAP2_VERSION 1 31\n"
            + "/begin PROJECT P \"\"\n"
            + " /begin MOD_COMMON \"common comment\" BYTE_ORDER MSB_LAST /end MOD_COMMON\n"
            + " /begin MODULE M \"\" /end MODULE\n"
            + "/end PROJECT\n";
        var result = Asap131Parser.ParseText(text);
        result.HasErrors.Should().BeFalse(
            $"no errors expected; actual: {string.Join("; ", result.Errors.Select(e => $"L{e.Line}:{e.Message}"))}");
        result.Value!.ModCommon.Should().NotBeNull();
        result.Value!.ModCommon!.Comment.Should().Be("common comment");
        result.Value!.ModCommon!.ByteOrder.Should().Be(A2lByteOrder.MSB_LAST);
    }

    [Fact]
    public void Parse_ModCommon_DefaultByteOrder_IsMsbLast()
    {
        const string text = "ASAP2_VERSION 1 31\n"
            + "/begin PROJECT P\n"
            + " /begin MOD_COMMON \"\" /end MOD_COMMON\n"
            + " /begin MODULE M \"\" /end MODULE\n"
            + "/end PROJECT\n";
        var result = Asap131Parser.ParseText(text);
        result.HasErrors.Should().BeFalse(
            $"no errors expected; actual: {string.Join("; ", result.Errors.Select(e => $"L{e.Line}:{e.Message}"))}");
        result.Value!.ModCommon.Should().NotBeNull();
        result.Value!.ModCommon!.ByteOrder.Should().Be(A2lByteOrder.MSB_LAST,
            "ASAP2 1.31 spec default is MSB_LAST when BYTE_ORDER is omitted");
    }
}
