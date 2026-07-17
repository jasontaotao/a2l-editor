using System.Collections.Generic;
using System.IO;
using A2lEditor.Core.Model;
using A2lEditor.Core.Serialization;
using FluentAssertions;
using Xunit;

namespace A2lEditor.Core.Tests.RoundTrip;

public class ByteForByteRoundTripTests
{
    [Fact]
    public void BmsModel_LoadFromFileThenWrite_RawTextEqualsInput()
    {
        // BmsModel is 16693 lines, ASCII/UTF-8, no BOM (verified spec-audit 2026-07-17)
        const string path = "samples/BmsModel.a2l";
        File.Exists(path).Should().BeTrue($"BmsModel fixture must exist at {path}");

        var doc = A2lDocument.LoadFromFile(path);
        var sw = new StringWriter();
        new A2lDocumentWriter().WriteToString(doc, sw);
        var output = sw.ToString();

        // 1:1 read→save lock: output must equal original (BmsModel has no comments or
        // transformations; RawText preserve + Writer priority emit = byte-for-byte)
        output.Should().Be(doc.RawText);
    }

    [Fact]
    public void EmptyDocument_NoRawText_FallsBackToSemanticEmit()
    {
        // Manually constructed A2lDocument (no RawText populated).
        // A2lDocument record fields are non-nullable strings, so "" represents
        // "no RawText" — and string.IsNullOrEmpty("") returns true, so the writer
        // must fall through to the semantic emit branch.
        var doc = new A2lDocument(
            Version: A2lVersion.V1_31,
            ProjectName: "P",
            ProjectComment: "",
            HeaderComment: "",
            ModCommon: null,
            Modules: new List<A2lModule>(),
            RawText: "",           // v0.8: must fall through to semantic emit
            SourceLineCount: 0);

        var sw = new StringWriter();
        new A2lDocumentWriter().WriteToString(doc, sw);
        var output = sw.ToString();

        // Should emit semantic (current v0.7 behavior), NOT throw
        output.Should().Contain("ASAP2_VERSION");
    }
}