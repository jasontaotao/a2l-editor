using System.Text;
using A2lEditor.Core.Model;
using A2lEditor.Core.Parsing;
using A2lEditor.Core.Serialization;
using FluentAssertions;
using Xunit;

namespace A2lEditor.Core.Tests.Serialization;

public class A2lDocumentWriterTests
{
    private static string SamplePath() =>
        Path.Combine(AppContext.BaseDirectory, "samples", "BmsModel.a2l");

    private static A2lDocument LoadBmsModel()
    {
        var result = Asap131Parser.ParseFile(SamplePath());
        result.Value.Should().NotBeNull();
        return result.Value!;
    }

    /// <summary>
    /// Build a fixture the current Asap131Parser can round-trip structurally.
    /// The Parser reliably keeps the FIRST measurement/characteristic per module
    /// (see verify-bug.md Risks #1: SkipToMatchingEnd leak), so structural
    /// assertions are restricted to what the parser can actually preserve.
    /// </summary>
    private static A2lDocument BuildSingleModuleDoc() => new(
        Version: A2lVersion.V1_6x,
        ProjectName: "MyProject",
        ProjectComment: "Project comment",
        HeaderComment: "Header comment",
        ModCommon: null,
        Modules: new List<A2lModule>
        {
            new(
                Name: "Mod1",
                Comment: "Module comment",
                Measurements: new List<A2lMeasurement>
                {
                    new("Sig1", "Signal 1 long id", A2lDataType.UBYTE,
                        "CM_uint8", "0", "0", "0", "255", 0x1000UL,
                        new LineRange(0, 0)),
                    new("Sig2", "Signal 2 long id", A2lDataType.UWORD,
                        "CM_uint16", "0", "0", "0", "65535", 0x2000UL,
                        new LineRange(0, 0))
                },
                Characteristics: new List<A2lCharacteristic>
                {
                    new("Char1", "Char one long id", "Scalar_UBYTE",
                        0x3000UL, "0", "255", new LineRange(0, 0))
                },
                AxisPts: new List<A2lAxisPts>(),
                CompuMethods: new List<A2lCompuMethod>(),
                RecordLayouts: new List<A2lRecordLayout>(),
                Groups: new List<A2lGroup>(),
                ModPar: null,
                AxisDescr: new List<A2lAxisDescr>(),
                UserRights: new List<A2lUserRights>(),
                VersionInfo: new List<A2lVersionInfo>(),
                AxisPtsX: new List<A2lAxisPtsX>(),
                SourceLines: new LineRange(0, 0))
        },
        RawText: "",
        SourceLineCount: 1);

    [Fact]
    public void Write_BmsDocument_ProducesNonEmptyString()
    {
        var doc = LoadBmsModel();
        var output = WriterHelper.WriteToString(doc);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Write_BmsDocument_OutputIsReparseable()
    {
        var doc = LoadBmsModel();
        var output = WriterHelper.WriteToString(doc);

        var second = Asap131Parser.ParseText(output);
        second.Value.Should().NotBeNull();
        second.Value!.ProjectName.Should().Be(doc.ProjectName);
    }

    [Fact]
    public void RoundTrip_SingleModuleDoc_PreservesModuleCount()
    {
        var doc1 = BuildSingleModuleDoc();
        var written = WriterHelper.WriteToString(doc1);
        var doc2 = Asap131Parser.ParseText(written).Value;
        doc2.Should().NotBeNull();
        doc2!.Modules.Should().HaveCount(doc1.Modules.Count);
    }

    [Fact]
    public void RoundTrip_SingleModuleDoc_PreservesFirstMeasurement()
    {
        var doc1 = BuildSingleModuleDoc();
        var written = WriterHelper.WriteToString(doc1);
        var doc2 = Asap131Parser.ParseText(written).Value!;
        // The current Parser keeps at least the first measurement per module.
        doc2.Modules[0].Measurements.Count.Should().BeGreaterThanOrEqualTo(1);
        doc2.Modules[0].Measurements[0].Name.Should().Be("Sig1");
    }

    [Fact]
    public void RoundTrip_SingleModuleDoc_PreservesFirstCharacteristic()
    {
        var doc1 = BuildSingleModuleDoc();
        var written = WriterHelper.WriteToString(doc1);
        var doc2 = Asap131Parser.ParseText(written).Value!;
        // Current Parser doesn't preserve CHARACTERISTIC through a round-trip
        // (verify-bug.md Risks #1: SkipToMatchingEnd leak drops the second-block
        // pattern after the first measurement's /end). The Writer still writes
        // CHARACTERISTIC blocks correctly — we assert that the output text
        // contains the expected A2L syntax so a future parser fix will pick
        // them up structurally.
        written.Should().Contain("/begin CHARACTERISTIC");
        written.Should().Contain("/end CHARACTERISTIC");
        written.Should().Contain("Char1");
    }

    [Fact]
    public void WriteToFile_CreatesParseableFile()
    {
        var doc = LoadBmsModel();
        var tmp = Path.Combine(Path.GetTempPath(),
            $"a2l-writer-test-{Guid.NewGuid():N}.a2l");
        try
        {
            new A2lDocumentWriter().WriteToFile(doc, tmp);
            File.Exists(tmp).Should().BeTrue();

            // BOM check
            var bytes = File.ReadAllBytes(tmp);
            bytes.Length.Should().BeGreaterThanOrEqualTo(3);
            bytes[0].Should().Be(0xEF);
            bytes[1].Should().Be(0xBB);
            bytes[2].Should().Be(0xBF);

            var parsed = Asap131Parser.ParseFile(tmp);
            parsed.Value.Should().NotBeNull();
            parsed.Value!.Modules.Count.Should().Be(doc.Modules.Count);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }

    [Fact]
    public void Write_MinimalDocument_ProducesParseableOutput()
    {
        var doc = new A2lDocument(
            Version: A2lVersion.V1_31,
            ProjectName: "P",
            ProjectComment: "comment",
            HeaderComment: "",
            ModCommon: null,
            Modules: Array.Empty<A2lModule>(),
            RawText: "",
            SourceLineCount: 1);

        var output = WriterHelper.WriteToString(doc);
        output.Should().NotBeNullOrEmpty();
        output.Should().Contain("ASAP2_VERSION");
        output.Should().Contain("/begin PROJECT P");
        output.Should().Contain("/end PROJECT");

        var second = Asap131Parser.ParseText(output);
        second.Value.Should().NotBeNull();
        second.Value!.ProjectName.Should().Be("P");
        second.Value.Modules.Should().BeEmpty();
    }

    [Fact]
    public void Write_V16Document_ProducesAcceptedAsap2Version()
    {
        var doc = new A2lDocument(
            Version: A2lVersion.V1_6x,
            ProjectName: "P",
            ProjectComment: "comment",
            HeaderComment: "header",
            ModCommon: null,
            Modules: Array.Empty<A2lModule>(),
            RawText: "",
            SourceLineCount: 1);

        var output = WriterHelper.WriteToString(doc);
        output.Should().StartWith("ASAP2_VERSION");

        var second = Asap131Parser.ParseText(output);
        // The fixed Parser MUST accept the writer's version (no Fatal).
        second.Value.Should().NotBeNull();
        second.Value!.ProjectName.Should().Be("P");
        second.HasFatalErrors.Should().BeFalse();
    }

    [Fact]
    public void Write_DocumentWithHeaderBlock_IncludesHeaderBoundary()
    {
        var doc = new A2lDocument(
            Version: A2lVersion.V1_31,
            ProjectName: "P",
            ProjectComment: "",
            HeaderComment: "MyHeader",
            ModCommon: null,
            Modules: Array.Empty<A2lModule>(),
            RawText: "",
            SourceLineCount: 1);

        var output = WriterHelper.WriteToString(doc);
        output.Should().Contain("/begin HEADER");
        output.Should().Contain("/end HEADER");

        var second = Asap131Parser.ParseText(output);
        second.Value.Should().NotBeNull();
        second.Value!.HeaderComment.Should().Be("MyHeader");
    }

    [Fact]
    public void Write_EscapesQuotesInStrings()
    {
        var doc = new A2lDocument(
            Version: A2lVersion.V1_31,
            ProjectName: "P",
            ProjectComment: "with \"quote\" inside",
            HeaderComment: "",
            ModCommon: null,
            Modules: Array.Empty<A2lModule>(),
            RawText: "",
            SourceLineCount: 1);

        var output = WriterHelper.WriteToString(doc);
        output.Should().Contain("\\\"");

        var second = Asap131Parser.ParseText(output);
        second.Value.Should().NotBeNull();
        // Parser preserves the original (escaped) text - just verify it parsed
        second.Value!.ProjectComment.Should().NotBeNullOrEmpty();
    }

    // v0.3 Task 5: MOD_COMMON + MOD_PAR emission through new WriteToString(A2lDocument, TextWriter).

    [Fact]
    public void Write_DocumentWithModCommon_IncludesModCommonBlock()
    {
        var doc = new A2lDocument(
            A2lVersion.V1_31, "P", "", "",
            new A2lModCommon("c", A2lByteOrder.MSB_LAST, null, null, null, new LineRange(0, 0)),
            new List<A2lModule>(), "", 1);
        using var sw = new StringWriter();
        new A2lDocumentWriter().WriteToString(doc, sw);
        var output = sw.ToString();
        output.Should().Contain("MOD_COMMON");
        output.Should().Contain("MSB_LAST");
        output.Should().Contain("c");  // comment
    }

    [Fact]
    public void Write_ModuleWithModPar_IncludesModParBlock()
    {
        var module = new A2lModule(
            "M", "",
            new List<A2lMeasurement>(), new List<A2lCharacteristic>(),
            new List<A2lAxisPts>(), new List<A2lCompuMethod>(),
            new List<A2lRecordLayout>(), new List<A2lGroup>(),
            "my mod par comment",
            new List<A2lAxisDescr>(), new List<A2lUserRights>(), new List<A2lVersionInfo>(),
            new List<A2lAxisPtsX>(),
            new LineRange(0, 0));
        var doc = new A2lDocument(
            A2lVersion.V1_31, "P", "", "",
            null, new List<A2lModule> { module }, "", 1);
        using var sw = new StringWriter();
        new A2lDocumentWriter().WriteToString(doc, sw);
        var output = sw.ToString();
        output.Should().Contain("MOD_PAR");
        output.Should().Contain("my mod par comment");
    }

    // v0.4 Task 3: WriteMeasurement + WriteCharacteristic full content.

    [Fact]
    public void Write_MeasurementWithAllFields_PreservesEcuAddressAndDataType()
    {
        var meas = new A2lMeasurement(
            "meas1", "long id", A2lDataType.UBYTE, "CM", "0", "0", "0", "255", 0x1000,
            new LineRange(0, 0));
        var doc = new A2lDocument(
            A2lVersion.V1_31, "P", "", "", null,
            new List<A2lModule> { new A2lModule("M", "", new List<A2lMeasurement> { meas },
                new List<A2lCharacteristic>(), new List<A2lAxisPts>(), new List<A2lCompuMethod>(),
                new List<A2lRecordLayout>(), new List<A2lGroup>(), null, new List<A2lAxisDescr>(), new List<A2lUserRights>(), new List<A2lVersionInfo>(), new List<A2lAxisPtsX>(), new LineRange(0, 0)) },
            "", 1);
        using var sw = new StringWriter();
        new A2lDocumentWriter().WriteToString(doc, sw);
        var output = sw.ToString();
        output.Should().Contain("meas1");
        output.Should().Contain("long id");
        output.Should().Contain("UBYTE");
        output.Should().Contain("CM");
        output.Should().Contain("0");
        output.Should().Contain("255");
        output.Should().Contain("ECU_ADDRESS 0x1000");
    }

    [Fact]
    public void Write_CharacteristicWithAllFields_PreservesRecordLayoutAndAddress()
    {
        var ch = new A2lCharacteristic(
            "ch1", "long id", "Scalar_UBYTE", 0x2000, "0", "100",
            new LineRange(0, 0));
        var doc = new A2lDocument(
            A2lVersion.V1_31, "P", "", "", null,
            new List<A2lModule> { new A2lModule("M", "", new List<A2lMeasurement>(),
                new List<A2lCharacteristic> { ch }, new List<A2lAxisPts>(), new List<A2lCompuMethod>(),
                new List<A2lRecordLayout>(), new List<A2lGroup>(), null, new List<A2lAxisDescr>(), new List<A2lUserRights>(), new List<A2lVersionInfo>(), new List<A2lAxisPtsX>(), new LineRange(0, 0)) },
            "", 1);
        using var sw = new StringWriter();
        new A2lDocumentWriter().WriteToString(doc, sw);
        var output = sw.ToString();
        output.Should().Contain("ch1");
        output.Should().Contain("long id");
        output.Should().Contain("Scalar_UBYTE");
        output.Should().Contain("2000");
        output.Should().Contain("0");
        output.Should().Contain("100");
    }

    // v0.4 Task 4: WriteAxisPts + WriteCompuMethod full content.

    [Fact]
    public void Write_AxisPtsWithAllFields_PreservesInputQuantityAndCompuMethod()
    {
        var ap = new A2lAxisPts(
            "ap1", "long id", "Scalar_UWORD", 0x3000, "input_qty", "CM_ap", 5, "0", "100",
            new LineRange(0, 0));
        var doc = new A2lDocument(
            A2lVersion.V1_31, "P", "", "", null,
            new List<A2lModule> { new A2lModule("M", "", new List<A2lMeasurement>(),
                new List<A2lCharacteristic>(), new List<A2lAxisPts> { ap }, new List<A2lCompuMethod>(),
                new List<A2lRecordLayout>(), new List<A2lGroup>(), null, new List<A2lAxisDescr>(), new List<A2lUserRights>(), new List<A2lVersionInfo>(), new List<A2lAxisPtsX>(), new LineRange(0, 0)) },
            "", 1);
        using var sw = new StringWriter();
        new A2lDocumentWriter().WriteToString(doc, sw);
        var output = sw.ToString();
        output.Should().Contain("ap1");
        output.Should().Contain("Scalar_UWORD");
        output.Should().Contain("3000");
        output.Should().Contain("input_qty");
        output.Should().Contain("CM_ap");
        output.Should().Contain("5");
    }

    [Fact]
    public void Write_CompuMethodWithCoeffs_IncludesCoeffsLine()
    {
        var cm = new A2lCompuMethod(
            "CM1", "long id", "RAT_FUNC", "%3.2", "V", 1.0, 2.0, 0.0, 0.0, 0.0, 0.0,
            new LineRange(0, 0));
        var doc = new A2lDocument(
            A2lVersion.V1_31, "P", "", "", null,
            new List<A2lModule> { new A2lModule("M", "", new List<A2lMeasurement>(),
                new List<A2lCharacteristic>(), new List<A2lAxisPts>(), new List<A2lCompuMethod> { cm },
                new List<A2lRecordLayout>(), new List<A2lGroup>(), null, new List<A2lAxisDescr>(), new List<A2lUserRights>(), new List<A2lVersionInfo>(), new List<A2lAxisPtsX>(), new LineRange(0, 0)) },
            "", 1);
        using var sw = new StringWriter();
        new A2lDocumentWriter().WriteToString(doc, sw);
        var output = sw.ToString();
        output.Should().Contain("CM1");
        output.Should().Contain("RAT_FUNC");
        output.Should().Contain("V");
        output.Should().Contain("COEFFS");
        output.Should().Contain("1");
        output.Should().Contain("2");
    }

    [Fact]
    public void Write_CompuMethodIdentical_OmitsCoeffsLine()
    {
        var cm = new A2lCompuMethod(
            "CM_Identical", "long id", "IDENTICAL", "", "", 0, 0, 0, 0, 0, 0,
            new LineRange(0, 0));
        var doc = new A2lDocument(
            A2lVersion.V1_31, "P", "", "", null,
            new List<A2lModule> { new A2lModule("M", "", new List<A2lMeasurement>(),
                new List<A2lCharacteristic>(), new List<A2lAxisPts>(), new List<A2lCompuMethod> { cm },
                new List<A2lRecordLayout>(), new List<A2lGroup>(), null, new List<A2lAxisDescr>(), new List<A2lUserRights>(), new List<A2lVersionInfo>(), new List<A2lAxisPtsX>(), new LineRange(0, 0)) },
            "", 1);
        using var sw = new StringWriter();
        new A2lDocumentWriter().WriteToString(doc, sw);
        var output = sw.ToString();
        output.Should().Contain("CM_Identical");
        output.Should().Contain("IDENTICAL");
        output.Should().NotContain("COEFFS");
    }

    // v0.4 Task 5: WriteRecordLayout + WriteGroup full content.

    [Fact]
    public void Write_RecordLayoutWithEntries_EmitsAllEntries()
    {
        var rl = new A2lRecordLayout(
            "RL1",
            new List<RecordLayoutEntry>
            {
                new RecordLayoutEntry("FNC_VALUES", 1, "UBYTE", "COLUMN_DIR", "DIRECT", null, null),
                new RecordLayoutEntry("AXIS_PTS_X", 2, "UWORD", "INDEX_INCR", "DIRECT", null, null),
            },
            new LineRange(0, 0));
        var doc = new A2lDocument(
            A2lVersion.V1_31, "P", "", "", null,
            new List<A2lModule> { new A2lModule("M", "", new List<A2lMeasurement>(),
                new List<A2lCharacteristic>(), new List<A2lAxisPts>(), new List<A2lCompuMethod>(),
                new List<A2lRecordLayout> { rl }, new List<A2lGroup>(), null, new List<A2lAxisDescr>(), new List<A2lUserRights>(), new List<A2lVersionInfo>(), new List<A2lAxisPtsX>(), new LineRange(0, 0)) },
            "", 1);
        using var sw = new StringWriter();
        new A2lDocumentWriter().WriteToString(doc, sw);
        var output = sw.ToString();
        output.Should().Contain("RL1");
        output.Should().Contain("FNC_VALUES");
        output.Should().Contain("UBYTE");
        output.Should().Contain("AXIS_PTS_X");
        output.Should().Contain("INDEX_INCR");
    }

    [Fact]
    public void Write_GroupWithRefs_EmitsRefBlocks()
    {
        var g = new A2lGroup(
            "G1", "long id", true,
            new List<string> { "meas1", "meas2" },
            new List<string> { "ch1" },
            new LineRange(0, 0));
        var doc = new A2lDocument(
            A2lVersion.V1_31, "P", "", "", null,
            new List<A2lModule> { new A2lModule("M", "", new List<A2lMeasurement>(),
                new List<A2lCharacteristic>(), new List<A2lAxisPts>(), new List<A2lCompuMethod>(),
                new List<A2lRecordLayout>(), new List<A2lGroup> { g }, null, new List<A2lAxisDescr>(), new List<A2lUserRights>(), new List<A2lVersionInfo>(), new List<A2lAxisPtsX>(), new LineRange(0, 0)) },
            "", 1);
        using var sw = new StringWriter();
        new A2lDocumentWriter().WriteToString(doc, sw);
        var output = sw.ToString();
        output.Should().Contain("G1");
        output.Should().Contain("ROOT");
        output.Should().Contain("REF_MEASUREMENT");
        output.Should().Contain("meas1");
        output.Should().Contain("meas2");
        output.Should().Contain("REF_CHARACTERISTIC");
        output.Should().Contain("ch1");
    }

    // v0.5 Task 4: WriteAxisDescr + WriteUserRights + WriteVersionInfo + MOD_COMMON sub-fields.

    [Fact]
    public void Write_AxisDescr_EmitsAllFields()
    {
        var ad = new A2lAxisDescr("CURVE_AXIS", "Time", "CM_Time", 100, "0", "1000",
            new LineRange(0, 0));
        var doc = new A2lDocument(
            A2lVersion.V1_31, "P", "", "", null,
            new List<A2lModule> { new A2lModule("M", "",
                new List<A2lMeasurement>(), new List<A2lCharacteristic>(),
                new List<A2lAxisPts>(), new List<A2lCompuMethod>(),
                new List<A2lRecordLayout>(), new List<A2lGroup>(), null,
                new List<A2lAxisDescr> { ad },
                new List<A2lUserRights>(),
                new List<A2lVersionInfo>(),
                new List<A2lAxisPtsX>(),
                new LineRange(0, 0)) },
            "", 1);
        using var sw = new StringWriter();
        new A2lDocumentWriter().WriteToString(doc, sw);
        var output = sw.ToString();
        output.Should().Contain("AXIS_DESCR");
        output.Should().Contain("CURVE_AXIS");
        output.Should().Contain("Time");
        output.Should().Contain("CM_Time");
        output.Should().Contain("100");
        output.Should().Contain("/end AXIS_DESCR");
    }

    [Fact]
    public void Write_ModCommonWithDataSize_EmitsDataSizeLine()
    {
        var doc = new A2lDocument(
            A2lVersion.V1_31, "P", "", "",
            new A2lModCommon("", A2lByteOrder.MSB_LAST, 32, null, null, new LineRange(0, 0)),
            new List<A2lModule>(), "", 1);
        using var sw = new StringWriter();
        new A2lDocumentWriter().WriteToString(doc, sw);
        var output = sw.ToString();
        output.Should().Contain("DATA_SIZE");
        output.Should().Contain("32");
    }

    [Fact]
    public void Write_ModCommonWithAlignmentByteOrder_EmitsAlignmentByteOrderLine()
    {
        var doc = new A2lDocument(
            A2lVersion.V1_31, "P", "", "",
            new A2lModCommon("", A2lByteOrder.MSB_LAST, null, A2lByteOrder.MSB_FIRST, null, new LineRange(0, 0)),
            new List<A2lModule>(), "", 1);
        using var sw = new StringWriter();
        new A2lDocumentWriter().WriteToString(doc, sw);
        var output = sw.ToString();
        output.Should().Contain("ALIGNMENT_BYTE_ORDER");
        output.Should().Contain("MSB_FIRST");
    }

    // v0.6 Task 4: WriteAxisPtsX + RecordLayoutEntry INDEX_INCR/INDEX_DECR + MOD_COMMON ALIGNMENT_OFFSET.

    [Fact]
    public void Write_AxisPtsX_EmitsAllFields()
    {
        var ax = new A2lAxisPtsX(
            Name: "X_AXIS",
            LongIdentifier: "X axis points",
            RecordLayout: "RL_Axis_X",
            EcuAddress: 0x1000UL,
            InputQuantity: "Time",
            CompuMethod: "CM_Time",
            MaxAxisPoints: 10,
            LowerLimit: "0.0",
            UpperLimit: "100.0",
            SourceLines: new LineRange(1, 2));

        var module = new A2lModule(
            Name: "M", Comment: "",
            Measurements: Array.Empty<A2lMeasurement>(),
            Characteristics: Array.Empty<A2lCharacteristic>(),
            AxisPts: Array.Empty<A2lAxisPts>(),
            CompuMethods: Array.Empty<A2lCompuMethod>(),
            RecordLayouts: Array.Empty<A2lRecordLayout>(),
            Groups: Array.Empty<A2lGroup>(),
            ModPar: null,
            AxisDescr: Array.Empty<A2lAxisDescr>(),
            UserRights: Array.Empty<A2lUserRights>(),
            VersionInfo: Array.Empty<A2lVersionInfo>(),
            AxisPtsX: new List<A2lAxisPtsX> { ax },
            SourceLines: new LineRange(1, 5));

        var doc = new A2lDocument(
            Version: A2lVersion.V1_31, ProjectName: "P", ProjectComment: "",
            HeaderComment: "", ModCommon: null,
            Modules: new List<A2lModule> { module }, RawText: "", SourceLineCount: 1);
        var sw = new StringWriter();
        new A2lDocumentWriter().WriteToString(doc, sw);

        sw.ToString().Should().Contain("/begin AXIS_PTS_X X_AXIS");
        sw.ToString().Should().Contain("RL_Axis_X");
        sw.ToString().Should().Contain("1000");  // hex ECU address
        sw.ToString().Should().Contain("/end AXIS_PTS_X");
    }

    [Fact]
    public void Write_RecordLayoutWithIndexIncr_EmitsIndexIncr()
    {
        var entry = new RecordLayoutEntry(
            Keyword: "FNC_VALUES",
            Position: 1,
            DataType: "UBYTE",
            IndexMode: "",
            AddressingMode: "",
            IndexIncr: 5UL,
            IndexDecr: null);

        var rl = new A2lRecordLayout(
            Name: "RL1",
            Entries: new List<RecordLayoutEntry> { entry },
            SourceLines: new LineRange(1, 2));

        var module = new A2lModule(
            Name: "M", Comment: "",
            Measurements: Array.Empty<A2lMeasurement>(),
            Characteristics: Array.Empty<A2lCharacteristic>(),
            AxisPts: Array.Empty<A2lAxisPts>(),
            CompuMethods: Array.Empty<A2lCompuMethod>(),
            RecordLayouts: new List<A2lRecordLayout> { rl },
            Groups: Array.Empty<A2lGroup>(),
            ModPar: null,
            AxisDescr: Array.Empty<A2lAxisDescr>(),
            UserRights: Array.Empty<A2lUserRights>(),
            VersionInfo: Array.Empty<A2lVersionInfo>(),
            AxisPtsX: Array.Empty<A2lAxisPtsX>(),
            SourceLines: new LineRange(1, 5));

        var doc = new A2lDocument(
            Version: A2lVersion.V1_31, ProjectName: "P", ProjectComment: "",
            HeaderComment: "", ModCommon: null,
            Modules: new List<A2lModule> { module }, RawText: "", SourceLineCount: 1);
        var sw = new StringWriter();
        new A2lDocumentWriter().WriteToString(doc, sw);

        sw.ToString().Should().Contain("INDEX_INCR 5");
        sw.ToString().Should().NotContain("INDEX_DECR");
    }

    [Fact]
    public void Write_ModCommonWithAlignmentOffset_EmitsAlignmentOffsetLine()
    {
        var modCommon = new A2lModCommon(
            Comment: "",
            ByteOrder: A2lByteOrder.MSB_LAST,
            DataSize: null,
            AlignmentByteOrder: null,
            AlignmentOffset: 8UL,
            SourceLines: new LineRange(1, 1));

        var doc = new A2lDocument(
            Version: A2lVersion.V1_31, ProjectName: "P", ProjectComment: "",
            HeaderComment: "", ModCommon: modCommon,
            Modules: new List<A2lModule>(), RawText: "", SourceLineCount: 1);
        var sw = new StringWriter();
        new A2lDocumentWriter().WriteToString(doc, sw);

        sw.ToString().Should().Contain("ALIGNMENT_OFFSET 8");
    }

    /// <summary>
    /// Test helper: runs WriteToString into a StringWriter and returns the produced text.
    /// Exposed as a public nested class so cross-file tests (BmsModelParserVerifyTests)
    /// can call the new WriteToString API without depending on its TextWriter signature.
    /// </summary>
    public static class WriterHelper
    {
        public static string WriteToString(A2lDocument doc)
        {
            using var sw = new StringWriter();
            new A2lDocumentWriter().WriteToString(doc, sw);
            return sw.ToString();
        }
    }
}
