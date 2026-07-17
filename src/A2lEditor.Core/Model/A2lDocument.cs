using A2lEditor.Core.Parsing;
using UtfUnknown;

namespace A2lEditor.Core.Model;

public sealed record A2lDocument(
    A2lVersion Version,
    string ProjectName,
    string ProjectComment,
    string HeaderComment,
    A2lModCommon? ModCommon,
    IReadOnlyList<A2lModule> Modules,
    string RawText,
    int SourceLineCount)
{
    public int TotalMeasurementCount =>
        Modules.Sum(m => m.Measurements.Count);

    public int TotalCharacteristicCount =>
        Modules.Sum(m => m.Characteristics.Count);

    // v0.8 NEW: UTF.Unknown BOM-aware file loader. Default to UTF-8 when the file
    // is plain ASCII (ASCII is a strict subset of UTF-8 — safe upgrade). UTF-16
    // variants fall through to their named encoding. Asap131Parser.ParseText
    // already populates RawText + SourceLineCount (verified at
    // Asap131Parser.cs:125-127), so the parser's existing pathway produces the
    // 8-arg ctor fields — no double-population needed.
    public static A2lDocument LoadFromFile(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var detected = CharsetDetector.DetectFromBytes(bytes);
        var encoding = ResolveEncoding(detected);
        var text = encoding.GetString(bytes);
        // UtfUnknown reports HasBOM; Encoding.GetString keeps BOM as U+FEFF.
        // Asap131Lexer treats U+FEFF as a token, which breaks version parsing —
        // strip it before handing the text to the parser (and so RawText is BOM-free
        // when the file was BOM-encoded).
        if (detected.Detected is { HasBOM: true })
        {
            text = StripBom(text);
        }

        var parseResult = Asap131Parser.ParseText(text);
        return parseResult.Value
            ?? throw new InvalidOperationException(
                $"Failed to parse A2L file: {path}");
    }

    private static System.Text.Encoding ResolveEncoding(DetectionResult result)
    {
        var webName = result.Detected?.Encoding?.WebName;
        // ASCII is a strict subset of UTF-8 — upgrade to UTF-8 so the BOM-aware
        // path matches what Asap131Parser would have produced in v0.7.
        if (string.IsNullOrEmpty(webName) ||
            webName.Equals("us-ascii", System.StringComparison.OrdinalIgnoreCase) ||
            webName.Equals("ascii", System.StringComparison.OrdinalIgnoreCase))
        {
            return System.Text.Encoding.UTF8;
        }
        try
        {
            return System.Text.Encoding.GetEncoding(webName);
        }
        catch
        {
            return System.Text.Encoding.UTF8;
        }
    }

    private static string StripBom(string text) =>
        text.Length > 0 && text[0] == '﻿' ? text.Substring(1) : text;
}