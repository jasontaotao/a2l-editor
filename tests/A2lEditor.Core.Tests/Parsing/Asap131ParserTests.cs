using System;
using System.IO;
using A2lEditor.Core.Model;
using A2lEditor.Core.Parsing;
using A2lEditor.Core.Serialization;
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

    [Fact]
    public void Writer_RoundTrip_BmsModel_PreservesModParAndModCommon()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "samples", "BmsModel.a2l");
        var doc = Asap131Parser.ParseFile(path).Value;
        doc.Should().NotBeNull("BmsModel.a2l must parse");
        doc!.ModCommon.Should().NotBeNull();
        doc.ModCommon!.ByteOrder.Should().Be(A2lByteOrder.MSB_LAST);

        using var sw = new StringWriter();
        new A2lDocumentWriter().WriteToString(doc, sw);
        var reParsed = Asap131Parser.ParseText(sw.ToString()).Value;
        reParsed.Should().NotBeNull();
        reParsed!.ModCommon.Should().NotBeNull();
        reParsed.ModCommon!.ByteOrder.Should().Be(doc.ModCommon.ByteOrder);
        reParsed.Modules[0].ModPar.Should().Be(doc.Modules[0].ModPar);
    }

    [Fact]
    public void Parse_AxisDescr_StoresAttributeAndLimits()
    {
        const string text = "ASAP2_VERSION 1 61\n"
            + "/begin PROJECT P\n"
            + " /begin MODULE M \"\"\n"
            + "  /begin AXIS_DESCR \"CURVE_AXIS\" Time CM_Time 100 0 1000 /end AXIS_DESCR\n"
            + " /end MODULE\n"
            + "/end PROJECT\n";
        var result = Asap131Parser.ParseText(text);
        result.HasFatalErrors.Should().BeFalse(
            $"no fatal errors expected; actual: {string.Join("; ", result.Errors.Select(e => $"L{e.Line}:{e.Message}"))}");
        result.Value!.Modules[0].AxisDescr.Should().HaveCount(1);
        result.Value!.Modules[0].AxisDescr[0].Attribute.Should().Be("CURVE_AXIS");
        result.Value!.Modules[0].AxisDescr[0].InputQuantity.Should().Be("Time");
        result.Value!.Modules[0].AxisDescr[0].Conversion.Should().Be("CM_Time");
        result.Value!.Modules[0].AxisDescr[0].MaxNumberOfAxisPoints.Should().Be(100);
        result.Value!.Modules[0].AxisDescr[0].LowerLimit.Should().Be("0");
        result.Value!.Modules[0].AxisDescr[0].UpperLimit.Should().Be("1000");
    }

    [Fact]
    public void Parse_UserRights_StoresUserIdAndAccess()
    {
        const string text = "ASAP2_VERSION 1 61\n"
            + "/begin PROJECT P\n"
            + " /begin MODULE M \"\"\n"
            + "  /begin USER_RIGHTS ECU_MASTER LEVEL_1 LEVEL_2 CALC /end USER_RIGHTS\n"
            + " /end MODULE\n"
            + "/end PROJECT\n";
        var result = Asap131Parser.ParseText(text);
        result.HasFatalErrors.Should().BeFalse(
            $"no fatal errors expected; actual: {string.Join("; ", result.Errors.Select(e => $"L{e.Line}:{e.Message}"))}");
        result.Value!.Modules[0].UserRights.Should().HaveCount(1);
        result.Value!.Modules[0].UserRights[0].UserId.Should().Be("ECU_MASTER");
        result.Value!.Modules[0].UserRights[0].ReadAccess.Should().Be("LEVEL_1");
        result.Value!.Modules[0].UserRights[0].WriteAccess.Should().Be("LEVEL_2");
        result.Value!.Modules[0].UserRights[0].AccessMethod.Should().Be("CALC");
    }

    [Fact]
    public void Parse_Version_StoresVersionNoAndVendor()
    {
        const string text = "ASAP2_VERSION 1 61\n"
            + "/begin PROJECT P\n"
            + " /begin MODULE M \"\"\n"
            + "  /begin VERSION \"1.6.1\" 2024-01-15 \"ACME Corp\" \"Description\" /end VERSION\n"
            + " /end MODULE\n"
            + "/end PROJECT\n";
        var result = Asap131Parser.ParseText(text);
        result.HasFatalErrors.Should().BeFalse(
            $"no fatal errors expected; actual: {string.Join("; ", result.Errors.Select(e => $"L{e.Line}:{e.Message}"))}");
        result.Value!.Modules[0].VersionInfo.Should().HaveCount(1);
        result.Value!.Modules[0].VersionInfo[0].VersionNo.Should().Be("1.6.1");
        result.Value!.Modules[0].VersionInfo[0].Date.Should().Be(new DateTime(2024, 1, 15));
        result.Value!.Modules[0].VersionInfo[0].Vendor.Should().Be("ACME Corp");
        result.Value!.Modules[0].VersionInfo[0].Description.Should().Be("Description");
    }

    [Fact]
    public void Parse_ModCommonDataSize_StoresDataSize()
    {
        const string text = "ASAP2_VERSION 1 61\n"
            + "/begin PROJECT P\n"
            + " /begin MOD_COMMON \"\" BYTE_ORDER MSB_LAST DATA_SIZE 32 /end MOD_COMMON\n"
            + " /begin MODULE M \"\" /end MODULE\n"
            + "/end PROJECT\n";
        var result = Asap131Parser.ParseText(text);
        result.HasFatalErrors.Should().BeFalse(
            $"no fatal errors expected; actual: {string.Join("; ", result.Errors.Select(e => $"L{e.Line}:{e.Message}"))}");
        result.Value!.ModCommon.Should().NotBeNull();
        result.Value!.ModCommon!.DataSize.Should().Be(32);
    }

    [Fact]
    public void Parse_ModCommonAlignmentByteOrder_StoresAlignmentByteOrder()
    {
        const string text = "ASAP2_VERSION 1 61\n"
            + "/begin PROJECT P\n"
            + " /begin MOD_COMMON \"\" BYTE_ORDER MSB_LAST ALIGNMENT_BYTE_ORDER MSB_FIRST /end MOD_COMMON\n"
            + " /begin MODULE M \"\" /end MODULE\n"
            + "/end PROJECT\n";
        var result = Asap131Parser.ParseText(text);
        result.HasFatalErrors.Should().BeFalse(
            $"no fatal errors expected; actual: {string.Join("; ", result.Errors.Select(e => $"L{e.Line}:{e.Message}"))}");
        result.Value!.ModCommon.Should().NotBeNull();
        result.Value!.ModCommon!.AlignmentByteOrder.Should().Be(A2lByteOrder.MSB_FIRST);
    }

    [Fact]
    public void Tokenize_StringWithNewline_PreservesNewline()
    {
        // v0.5 multi-line verify: Asap131Lexer.ReadString L130 already supports
        // multi-line string literals via Advance(_text[_pos] == '\n'). This test
        // locks the invariant so a future Lexer refactor doesn't accidentally
        // break multi-line string support.
        const string text = "ASAP2_VERSION 1 61\n"
            + "/begin PROJECT P \"line1\nline2\nline3\" /end PROJECT\n";
        var result = Asap131Parser.ParseText(text);
        result.HasFatalErrors.Should().BeFalse(
            $"no fatal errors expected; actual: {string.Join("; ", result.Errors.Select(e => $"L{e.Line}:{e.Message}"))}");
        result.Value!.ProjectComment.Should().Be("line1\nline2\nline3");
    }

    // --- v0.6 Task 2 helpers (test-only) ---

    [Fact]
    public void Parse_AxisPtsX_StoresNameAndMaxAxisPoints()
    {
        const string text = "ASAP2_VERSION 1 61\n"
            + "/begin PROJECT P\n"
            + " /begin MODULE M \"\"\n"
            + "  /begin AXIS_PTS_X X_AXIS \"X axis points\" RL_Axis_X 0x1000 Time CM_Time 10 0.0 100.0 /end AXIS_PTS_X\n"
            + " /end MODULE\n"
            + "/end PROJECT\n";
        var result = Asap131Parser.ParseText(text);
        result.HasFatalErrors.Should().BeFalse(
            $"no fatal errors expected; actual: {string.Join("; ", result.Errors.Select(e => $"L{e.Line}: {e.Message}"))}");
        result.Value!.Modules[0].AxisPtsX.Should().HaveCount(1);
        var ax = result.Value!.Modules[0].AxisPtsX[0];
        ax.Name.Should().Be("X_AXIS");
        ax.LongIdentifier.Should().Be("X axis points");
        ax.RecordLayout.Should().Be("RL_Axis_X");
        ax.EcuAddress.Should().Be(0x1000UL);
        ax.MaxAxisPoints.Should().Be(10);
        ax.InputQuantity.Should().Be("Time");
        ax.CompuMethod.Should().Be("CM_Time");
    }

    [Fact]
    public void Parse_RecordLayoutIndexIncr_StoresIndexIncr()
    {
        const string text = "ASAP2_VERSION 1 61\n"
            + "/begin PROJECT P\n"
            + " /begin MODULE M \"\"\n"
            + "  /begin RECORD_LAYOUT RL1\n"
            + "   SRC_ADDR 0 UBYTE\n"
            + "   FNC_VALUES 1 UBYTE INDEX_INCR 5\n"
            + "  /end RECORD_LAYOUT\n"
            + " /end MODULE\n"
            + "/end PROJECT\n";
        var result = Asap131Parser.ParseText(text);
        result.HasFatalErrors.Should().BeFalse(
            $"no fatal errors expected; actual: {string.Join("; ", result.Errors.Select(e => $"L{e.Line}: {e.Message}"))}");
        var rl = result.Value!.Modules[0].RecordLayouts[0];
        rl.Entries.Should().HaveCount(2);
        rl.Entries[1].IndexIncr.Should().Be(5UL);
        rl.Entries[1].IndexDecr.Should().BeNull();
    }

    // ====================================================================
    //  P0 regression — ParseModule must not let an inner block's /end
    //  terminate the surrounding MODULE. On EB-tresos v1.71-style files
    //  (samples/BmsModel.a2l) the inner CHARACTERISTIC's "/end CHARACTERISTIC"
    //  used to be misread as "/end MODULE" and silently truncate module
    //  parsing — yielding M=0/C=1 (and 354 MEASUREMENT + 903 other
    //  CHARACTERISTIC silently dropped, with ZERO parse errors).
    //  These counts are the authoritative ground truth from the fixture
    //  (grep -c over the source file) and act as a sanity sentinel: if any
    //  block type silently underparses again, these assertions catch it
    //  even when record-level field correctness tests stay green.
    // ====================================================================

    private static readonly string BmsModelPath = Path.Combine(
        AppContext.BaseDirectory, "samples", "BmsModel.a2l");

    [Fact]
    public void Parse_BmsModel_BlockCounts_MatchSourceFileGroundTruth()
    {
        var result = Asap131Parser.ParseFile(BmsModelPath);
        result.Value.Should().NotBeNull("BmsModel.a2l must produce a document");
        result.Value!.Modules.Should().HaveCount(1, "BmsModel.a2l has exactly 1 MODULE");
        var m = result.Value!.Modules[0];

        m.Measurements.Should().HaveCount(354,
            "P0 regression: inner-block /end was misread as /end MODULE, silently dropping all measurements");
        m.Characteristics.Should().HaveCount(904,
            "P0 regression: only the first CHARACTERISTIC was parsed before MODULE was truncated");
        m.RecordLayouts.Should().HaveCount(45);
        m.CompuMethods.Should().HaveCount(8);
        m.AxisPts.Should().HaveCount(2);
        m.Groups.Should().HaveCount(3);
        // MEASUREMENT fields already match EB-tresos grammar, so once the
        // truncation is fixed their values must be correct too.
        var sample = m.Measurements.First(x => x.Name == "ACM_tDebugACChgEn");
        sample.DataType.Should().Be(A2lDataType.UBYTE);
        sample.CompuMethod.Should().Be("BMS_CM_uint8");
        sample.EcuAddress.Should().Be(0x0000UL);
        sample.UpperLimit.Should().Be("255");
    }

    [Fact]
    public void Parse_BmsModel_HasNoFatalErrors_AfterP0Fix()
    {
        // Before P0-A the parser reported ZERO errors despite dropping 903
        // blocks — the silent failure mode. After P0-A, parsing reaches
        // the full module body; we still expect no FATAL errors on the
        // fixture (field-level mismatches in CHARACTERISTIC/AXIS_PTS are
        // a separate P0-B concern tracked as a known gap, not fatal).
        var result = Asap131Parser.ParseFile(BmsModelPath);
        result.HasFatalErrors.Should().BeFalse(
            $"no fatal errors expected on BmsModel.a2l; actual: {string.Join("; ", result.Errors.Where(e => e.Severity == ErrorSeverity.Fatal).Select(e => $"L{e.Line}:{e.Message}"))}");
    }

    [Fact]
    public void Parse_ModCommon_StoresAlignmentOffset()
    {
        // MODULE nested inside PROJECT block; MOD_COMMON inside MODULE
        // matches the v1.61 _moduleModCommon lift fallback.
        const string text = "ASAP2_VERSION 1 61\n"
            + "/begin PROJECT P\n"
            + " /begin MODULE M \"\"\n"
            + "  /begin MOD_COMMON \"\" BYTE_ORDER MSB_LAST ALIGNMENT_OFFSET 8 /end MOD_COMMON\n"
            + " /end MODULE\n"
            + "/end PROJECT\n";
        var result = Asap131Parser.ParseText(text);
        result.HasFatalErrors.Should().BeFalse(
            $"no fatal errors expected; actual: {string.Join("; ", result.Errors.Select(e => $"L{e.Line}: {e.Message}"))}");
        result.Value!.ModCommon.Should().NotBeNull();
        result.Value!.ModCommon!.AlignmentOffset.Should().Be(8UL);
    }

    // ====================================================================
    //  P0-B regression — CHARACTERISTIC Type / MaxDiff / Conversion fields
    // ====================================================================

    [Fact]
    public void Parse_Characteristic_WithTypeMaxDiffConversionStoresFields()
    {
        const string text = "ASAP2_VERSION 1 31\n"
            + "/begin PROJECT P\n"
            + " /begin MODULE M \"\"\n"
            + "  /begin CHARACTERISTIC C1 \"desc\" CURVE Scalar_UBYTE 0x1000 0 100 0.05 CM_Curve\n"
            + "  /end CHARACTERISTIC\n"
            + "  /begin CHARACTERISTIC C2 \"desc\" VALUE Scalar_UBYTE 0x2000 0 255\n"
            + "  /end CHARACTERISTIC\n"
            + " /end MODULE\n"
            + "/end PROJECT\n";
        var result = Asap131Parser.ParseText(text);
        result.HasFatalErrors.Should().BeFalse();
        result.Value!.Modules.Should().HaveCount(1);
        var chars = result.Value!.Modules[0].Characteristics;
        chars.Should().HaveCount(2);

        // C1: full fields
        chars[0].Name.Should().Be("C1");
        chars[0].Type.Should().Be("CURVE");
        chars[0].MaxDiff.Should().Be("0.05");
        chars[0].Conversion.Should().Be("CM_Curve");

        // C2: minimal — no MaxDiff, no Conversion
        chars[1].Name.Should().Be("C2");
        chars[1].Type.Should().Be("VALUE");
        chars[1].MaxDiff.Should().BeNull();
        chars[1].Conversion.Should().BeNull();
    }
}
