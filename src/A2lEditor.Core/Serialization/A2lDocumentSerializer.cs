using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using A2lEditor.Core.Model;

namespace A2lEditor.Core.Serialization;

/// <summary>
/// 使用 System.Text.Json（JSON）和 LINQ to XML（XML）序列化/反序列化 A2lDocument。
/// JSON 路由直接委托给 JsonSerializer；XML 路由手动构建 XElement 树。
/// </summary>
public sealed class A2lDocumentSerializer : IA2lDocumentSerializer
{
    // ====================================================================
    // JSON
    // ====================================================================

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true,
    };

    public string SerializeToJson(A2lDocument doc)
        => JsonSerializer.Serialize(doc, JsonOptions);

    public A2lDocument DeserializeFromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<A2lDocument>(json, JsonOptions)
                   ?? throw new InvalidOperationException("JSON deserialization returned null.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"JSON deserialization failed at line {ex.LineNumber}, byte {ex.BytePositionInLine}: {ex.Message}", ex);
        }
    }

    // ====================================================================
    // XML
    // ====================================================================

    public string SerializeToXml(A2lDocument doc)
    {
        var xDoc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), BuildDoc(doc));
        return xDoc.ToString();
    }

    public A2lDocument DeserializeFromXml(string xml)
    {
        try
        {
            var xDoc = XDocument.Parse(xml);
            return ParseDoc(xDoc.Root ?? throw new InvalidOperationException("XML root element missing."));
        }
        catch (System.Xml.XmlException ex)
        {
            throw new InvalidOperationException(
                $"XML deserialization failed at line {ex.LineNumber}, position {ex.LinePosition}: {ex.Message}", ex);
        }
    }

    // ---------------------------------------------------------------
    // XML 构建
    // ---------------------------------------------------------------

    private static XElement BuildDoc(A2lDocument doc) => new("A2lDocument",
        new XElement("Version", doc.Version.ToString()),
        new XElement("ProjectName", doc.ProjectName),
        new XElement("ProjectComment", doc.ProjectComment),
        new XElement("HeaderComment", doc.HeaderComment),
        doc.ModCommon is { } mc ? BuildModCommon(mc) : new XElement("ModCommon"),
        new XElement("Modules", doc.Modules.Select(BuildModule)),
        new XElement("RawText", doc.RawText),
        new XElement("SourceLineCount", doc.SourceLineCount));

    private static XElement BuildModule(A2lModule m) => new("Module",
        new XElement("Name", m.Name),
        new XElement("Comment", m.Comment),
        new XElement("Measurements", m.Measurements.Select(BuildMeasurement)),
        new XElement("Characteristics", m.Characteristics.Select(BuildCharacteristic)),
        new XElement("AxisPts", m.AxisPts.Select(BuildAxisPts)),
        new XElement("CompuMethods", m.CompuMethods.Select(BuildCompuMethod)),
        new XElement("RecordLayouts", m.RecordLayouts.Select(BuildRecordLayout)),
        new XElement("Groups", m.Groups.Select(BuildGroup)),
        m.ModPar is { } mp ? new XElement("ModPar", mp) : new XElement("ModPar"),
        new XElement("AxisDescrEntries", m.AxisDescr.Select(BuildAxisDescr)),
        new XElement("UserRights", m.UserRights.Select(BuildUserRights)),
        new XElement("VersionInfo", m.VersionInfo.Select(BuildVersionInfo)),
        new XElement("AxisPtsX", m.AxisPtsX.Select(BuildAxisPtsX)),
        BuildLineRange(m.SourceLines));

    private static XElement BuildMeasurement(A2lMeasurement v) => new("Measurement",
        new XElement("Name", v.Name),
        new XElement("LongIdentifier", v.LongIdentifier),
        new XElement("DataType", v.DataType.ToString()),
        new XElement("CompuMethod", v.CompuMethod),
        new XElement("Resolution", v.Resolution),
        new XElement("Accuracy", v.Accuracy),
        new XElement("LowerLimit", v.LowerLimit),
        new XElement("UpperLimit", v.UpperLimit),
        new XElement("EcuAddress", $"0x{v.EcuAddress:X}"),
        BuildLineRange(v.SourceLines));

    private static XElement BuildCharacteristic(A2lCharacteristic v) => new("Characteristic",
        new XElement("Name", v.Name),
        new XElement("LongIdentifier", v.LongIdentifier),
        new XElement("Type", v.Type),
        new XElement("RecordLayout", v.RecordLayout),
        new XElement("EcuAddress", $"0x{v.EcuAddress:X}"),
        new XElement("LowerLimit", v.LowerLimit),
        new XElement("UpperLimit", v.UpperLimit),
        v.MaxDiff is not null ? new XElement("MaxDiff", v.MaxDiff) : null,
        v.Conversion is not null ? new XElement("Conversion", v.Conversion) : null,
        BuildLineRange(v.SourceLines));

    private static XElement BuildAxisPts(A2lAxisPts v) => new("AxisPts",
        new XElement("Name", v.Name),
        new XElement("LongIdentifier", v.LongIdentifier),
        new XElement("RecordLayout", v.RecordLayout),
        new XElement("EcuAddress", $"0x{v.EcuAddress:X}"),
        new XElement("InputQuantity", v.InputQuantity),
        new XElement("CompuMethod", v.CompuMethod),
        new XElement("NumberOfAxisPts", v.NumberOfAxisPts),
        new XElement("LowerLimit", v.LowerLimit),
        new XElement("UpperLimit", v.UpperLimit),
        BuildLineRange(v.SourceLines));

    private static XElement BuildAxisPtsX(A2lAxisPtsX v) => new("AxisPtsX",
        new XElement("Name", v.Name),
        new XElement("LongIdentifier", v.LongIdentifier),
        new XElement("RecordLayout", v.RecordLayout),
        new XElement("EcuAddress", $"0x{v.EcuAddress:X}"),
        new XElement("InputQuantity", v.InputQuantity),
        new XElement("CompuMethod", v.CompuMethod),
        new XElement("MaxAxisPoints", v.MaxAxisPoints),
        new XElement("LowerLimit", v.LowerLimit),
        new XElement("UpperLimit", v.UpperLimit),
        BuildLineRange(v.SourceLines));

    private static XElement BuildCompuMethod(A2lCompuMethod v) => new("CompuMethod",
        new XElement("Name", v.Name),
        new XElement("LongIdentifier", v.LongIdentifier),
        new XElement("ConversionType", v.ConversionType),
        new XElement("Format", v.Format),
        new XElement("Unit", v.Unit),
        new XElement("CoeffA", v.CoeffA),
        new XElement("CoeffB", v.CoeffB),
        new XElement("CoeffC", v.CoeffC),
        new XElement("CoeffD", v.CoeffD),
        new XElement("CoeffE", v.CoeffE),
        new XElement("CoeffF", v.CoeffF),
        BuildLineRange(v.SourceLines));

    private static XElement BuildRecordLayout(A2lRecordLayout v) => new("RecordLayout",
        new XElement("Name", v.Name),
        new XElement("Entries", v.Entries.Select(BuildRecordLayoutEntry)),
        BuildLineRange(v.SourceLines));

    private static XElement BuildRecordLayoutEntry(RecordLayoutEntry e) => new("Entry",
        new XElement("Keyword", e.Keyword),
        new XElement("Position", e.Position),
        new XElement("DataType", e.DataType),
        new XElement("IndexMode", e.IndexMode),
        new XElement("AddressingMode", e.AddressingMode),
        e.IndexIncr.HasValue ? new XElement("IndexIncr", e.IndexIncr.Value) : null,
        e.IndexDecr.HasValue ? new XElement("IndexDecr", e.IndexDecr.Value) : null);

    private static XElement BuildGroup(A2lGroup v) => new("Group",
        new XElement("Name", v.Name),
        new XElement("LongIdentifier", v.LongIdentifier),
        new XElement("IsRoot", v.IsRoot),
        new XElement("RefMeasurements", v.RefMeasurements.Select(r => new XElement("Ref", r))),
        new XElement("RefCharacteristics", v.RefCharacteristics.Select(r => new XElement("Ref", r))),
        BuildLineRange(v.SourceLines));

    private static XElement BuildModCommon(A2lModCommon v) => new("ModCommon",
        new XElement("Comment", v.Comment),
        new XElement("ByteOrder", v.ByteOrder.ToString()),
        v.DataSize.HasValue ? new XElement("DataSize", v.DataSize.Value) : null,
        v.AlignmentByteOrder.HasValue ? new XElement("AlignmentByteOrder", v.AlignmentByteOrder.Value.ToString()) : null,
        v.AlignmentOffset.HasValue ? new XElement("AlignmentOffset", v.AlignmentOffset.Value) : null,
        BuildLineRange(v.SourceLines));

    private static XElement BuildAxisDescr(A2lAxisDescr v) => new("AxisDescr",
        new XElement("Attribute", v.Attribute),
        new XElement("InputQuantity", v.InputQuantity),
        new XElement("Conversion", v.Conversion),
        new XElement("MaxNumberOfAxisPoints", v.MaxNumberOfAxisPoints),
        new XElement("LowerLimit", v.LowerLimit),
        new XElement("UpperLimit", v.UpperLimit),
        BuildLineRange(v.SourceLines));

    private static XElement BuildUserRights(A2lUserRights v) => new("UserRights",
        new XElement("UserId", v.UserId),
        new XElement("ReadAccess", v.ReadAccess),
        new XElement("WriteAccess", v.WriteAccess),
        new XElement("AccessMethod", v.AccessMethod),
        BuildLineRange(v.SourceLines));

    private static XElement BuildVersionInfo(A2lVersionInfo v) => new("VersionInfo",
        new XElement("VersionNo", v.VersionNo),
        new XElement("Date", v.Date.ToString("o", CultureInfo.InvariantCulture)),
        new XElement("Vendor", v.Vendor),
        new XElement("Description", v.Description),
        BuildLineRange(v.SourceLines));

    private static XElement BuildLineRange(LineRange lr) =>
        new("SourceLines", new XElement("Start", lr.Start), new XElement("End", lr.End));

    // ---------------------------------------------------------------
    // XML 解析
    // ---------------------------------------------------------------

    private static A2lDocument ParseDoc(XElement root)
    {
        var version = Enum.Parse<A2lVersion>(
            root.Element("Version")?.Value ?? "V1_31", ignoreCase: true);
        var projectName = root.Element("ProjectName")?.Value ?? "";
        var projectComment = root.Element("ProjectComment")?.Value ?? "";
        var headerComment = root.Element("HeaderComment")?.Value ?? "";

        var mcElem = root.Element("ModCommon");
        var modCommon = mcElem?.Elements().Any() == true ? ParseModCommon(mcElem) : null;

        var modules = root.Element("Modules")?.Elements("Module").Select(ParseModule).ToList()
                      ?? new List<A2lModule>();

        var rawText = root.Element("RawText")?.Value ?? "";
        var sourceLineCount = (int?)root.Element("SourceLineCount") ?? 0;

        return new A2lDocument(version, projectName, projectComment, headerComment,
            modCommon, modules, rawText, sourceLineCount);
    }

    private static A2lModule ParseModule(XElement elem)
    {
        var name = elem.Element("Name")?.Value ?? "";
        var comment = elem.Element("Comment")?.Value ?? "";

        var measurements = elem.Element("Measurements")
            ?.Elements("Measurement").Select(ParseMeasurement).ToList() ?? new List<A2lMeasurement>();
        var characteristics = elem.Element("Characteristics")
            ?.Elements("Characteristic").Select(ParseCharacteristic).ToList() ?? new List<A2lCharacteristic>();
        var axisPts = elem.Element("AxisPts")
            ?.Elements("AxisPts").Select(ParseAxisPts).ToList() ?? new List<A2lAxisPts>();
        var compuMethods = elem.Element("CompuMethods")
            ?.Elements("CompuMethod").Select(ParseCompuMethod).ToList() ?? new List<A2lCompuMethod>();
        var recordLayouts = elem.Element("RecordLayouts")
            ?.Elements("RecordLayout").Select(ParseRecordLayout).ToList() ?? new List<A2lRecordLayout>();
        var groups = elem.Element("Groups")
            ?.Elements("Group").Select(ParseGroup).ToList() ?? new List<A2lGroup>();

        var modPar = elem.Element("ModPar")?.Value;
        if (string.IsNullOrEmpty(modPar)) modPar = null;

        var axisDescr = elem.Element("AxisDescrEntries")
            ?.Elements("AxisDescr").Select(ParseAxisDescr).ToList() ?? new List<A2lAxisDescr>();
        var userRights = elem.Element("UserRights")
            ?.Elements("UserRights").Select(ParseUserRights).ToList() ?? new List<A2lUserRights>();
        var versionInfo = elem.Element("VersionInfo")
            ?.Elements("VersionInfo").Select(ParseVersionInfo).ToList() ?? new List<A2lVersionInfo>();
        var axisPtsX = elem.Element("AxisPtsX")
            ?.Elements("AxisPtsX").Select(ParseAxisPtsX).ToList() ?? new List<A2lAxisPtsX>();

        var sourceLines = ParseLineRange(elem.Element("SourceLines"));

        return new A2lModule(name, comment, measurements, characteristics, axisPts,
            compuMethods, recordLayouts, groups, modPar, axisDescr, userRights,
            versionInfo, axisPtsX, sourceLines);
    }

    private static A2lMeasurement ParseMeasurement(XElement e) => new(
        e.Element("Name")?.Value ?? "",
        e.Element("LongIdentifier")?.Value ?? "",
        Enum.Parse<A2lDataType>(e.Element("DataType")?.Value ?? "UBYTE", ignoreCase: true),
        e.Element("CompuMethod")?.Value ?? "",
        e.Element("Resolution")?.Value ?? "",
        e.Element("Accuracy")?.Value ?? "",
        e.Element("LowerLimit")?.Value ?? "",
        e.Element("UpperLimit")?.Value ?? "",
        ParseHex(e.Element("EcuAddress")?.Value ?? "0"),
        ParseLineRange(e.Element("SourceLines")));

    private static A2lCharacteristic ParseCharacteristic(XElement e) => new(
        e.Element("Name")?.Value ?? "",
        e.Element("LongIdentifier")?.Value ?? "",
        e.Element("Type")?.Value ?? "VALUE",
        e.Element("RecordLayout")?.Value ?? "",
        ParseHex(e.Element("EcuAddress")?.Value ?? "0"),
        e.Element("LowerLimit")?.Value ?? "",
        e.Element("UpperLimit")?.Value ?? "",
        e.Element("MaxDiff")?.Value,
        e.Element("Conversion")?.Value,
        ParseLineRange(e.Element("SourceLines")));

    private static A2lAxisPts ParseAxisPts(XElement e) => new(
        e.Element("Name")?.Value ?? "",
        e.Element("LongIdentifier")?.Value ?? "",
        e.Element("RecordLayout")?.Value ?? "",
        ParseHex(e.Element("EcuAddress")?.Value ?? "0"),
        e.Element("InputQuantity")?.Value ?? "",
        e.Element("CompuMethod")?.Value ?? "",
        int.Parse(e.Element("NumberOfAxisPts")?.Value ?? "0", CultureInfo.InvariantCulture),
        e.Element("LowerLimit")?.Value ?? "",
        e.Element("UpperLimit")?.Value ?? "",
        ParseLineRange(e.Element("SourceLines")));

    private static A2lAxisPtsX ParseAxisPtsX(XElement e) => new(
        e.Element("Name")?.Value ?? "",
        e.Element("LongIdentifier")?.Value ?? "",
        e.Element("RecordLayout")?.Value ?? "",
        ParseHex(e.Element("EcuAddress")?.Value ?? "0"),
        e.Element("InputQuantity")?.Value ?? "",
        e.Element("CompuMethod")?.Value ?? "",
        int.Parse(e.Element("MaxAxisPoints")?.Value ?? "0", CultureInfo.InvariantCulture),
        e.Element("LowerLimit")?.Value ?? "",
        e.Element("UpperLimit")?.Value ?? "",
        ParseLineRange(e.Element("SourceLines")));

    private static A2lCompuMethod ParseCompuMethod(XElement e) => new(
        e.Element("Name")?.Value ?? "",
        e.Element("LongIdentifier")?.Value ?? "",
        e.Element("ConversionType")?.Value ?? "",
        e.Element("Format")?.Value ?? "",
        e.Element("Unit")?.Value ?? "",
        double.Parse(e.Element("CoeffA")?.Value ?? "0", CultureInfo.InvariantCulture),
        double.Parse(e.Element("CoeffB")?.Value ?? "0", CultureInfo.InvariantCulture),
        double.Parse(e.Element("CoeffC")?.Value ?? "0", CultureInfo.InvariantCulture),
        double.Parse(e.Element("CoeffD")?.Value ?? "0", CultureInfo.InvariantCulture),
        double.Parse(e.Element("CoeffE")?.Value ?? "0", CultureInfo.InvariantCulture),
        double.Parse(e.Element("CoeffF")?.Value ?? "0", CultureInfo.InvariantCulture),
        ParseLineRange(e.Element("SourceLines")));

    private static A2lRecordLayout ParseRecordLayout(XElement e)
    {
        var name = e.Element("Name")?.Value ?? "";
        var entries = e.Element("Entries")
            ?.Elements("Entry").Select(ParseRecordLayoutEntry).ToList() ?? new List<RecordLayoutEntry>();
        return new A2lRecordLayout(name, entries, ParseLineRange(e.Element("SourceLines")));
    }

    private static RecordLayoutEntry ParseRecordLayoutEntry(XElement e)
    {
        ulong? indexIncr = e.Element("IndexIncr") is { } ii
            ? ulong.Parse(ii.Value, CultureInfo.InvariantCulture) : null;
        ulong? indexDecr = e.Element("IndexDecr") is { } id
            ? ulong.Parse(id.Value, CultureInfo.InvariantCulture) : null;

        return new RecordLayoutEntry(
            e.Element("Keyword")?.Value ?? "",
            int.Parse(e.Element("Position")?.Value ?? "0", CultureInfo.InvariantCulture),
            e.Element("DataType")?.Value ?? "",
            e.Element("IndexMode")?.Value ?? "",
            e.Element("AddressingMode")?.Value ?? "",
            indexIncr, indexDecr);
    }

    private static A2lGroup ParseGroup(XElement e) => new(
        e.Element("Name")?.Value ?? "",
        e.Element("LongIdentifier")?.Value ?? "",
        bool.Parse(e.Element("IsRoot")?.Value ?? "false"),
        (e.Element("RefMeasurements")?.Elements("Ref").Select(x => x.Value) ?? Enumerable.Empty<string>()).ToList(),
        (e.Element("RefCharacteristics")?.Elements("Ref").Select(x => x.Value) ?? Enumerable.Empty<string>()).ToList(),
        ParseLineRange(e.Element("SourceLines")));

    private static A2lModCommon ParseModCommon(XElement e) => new(
        e.Element("Comment")?.Value ?? "",
        Enum.Parse<A2lByteOrder>(e.Element("ByteOrder")?.Value ?? "MSB_LAST", ignoreCase: true),
        e.Element("DataSize") is { } ds ? ulong.Parse(ds.Value, CultureInfo.InvariantCulture) : null,
        e.Element("AlignmentByteOrder") is { } abo ? Enum.Parse<A2lByteOrder>(abo.Value, ignoreCase: true) : null,
        e.Element("AlignmentOffset") is { } ao ? ulong.Parse(ao.Value, CultureInfo.InvariantCulture) : null,
        ParseLineRange(e.Element("SourceLines")));

    private static A2lAxisDescr ParseAxisDescr(XElement e) => new(
        e.Element("Attribute")?.Value ?? "",
        e.Element("InputQuantity")?.Value ?? "",
        e.Element("Conversion")?.Value ?? "",
        ulong.Parse(e.Element("MaxNumberOfAxisPoints")?.Value ?? "0", CultureInfo.InvariantCulture),
        e.Element("LowerLimit")?.Value ?? "",
        e.Element("UpperLimit")?.Value ?? "",
        ParseLineRange(e.Element("SourceLines")));

    private static A2lUserRights ParseUserRights(XElement e) => new(
        e.Element("UserId")?.Value ?? "",
        e.Element("ReadAccess")?.Value ?? "",
        e.Element("WriteAccess")?.Value ?? "",
        e.Element("AccessMethod")?.Value ?? "",
        ParseLineRange(e.Element("SourceLines")));

    private static A2lVersionInfo ParseVersionInfo(XElement e) => new(
        e.Element("VersionNo")?.Value ?? "",
        DateTime.Parse(e.Element("Date")?.Value ?? "2000-01-01", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        e.Element("Vendor")?.Value ?? "",
        e.Element("Description")?.Value ?? "",
        ParseLineRange(e.Element("SourceLines")));

    private static LineRange ParseLineRange(XElement? e)
    {
        if (e is null) return new LineRange(0, 0);
        var start = (int?)e.Element("Start") ?? 0;
        var end = (int?)e.Element("End") ?? 0;
        return new LineRange(start, end);
    }

    // ---------------------------------------------------------------
    // 辅助
    // ---------------------------------------------------------------

    private static ulong ParseHex(string s)
    {
        if (string.IsNullOrEmpty(s))
            throw new ArgumentException("Hex address value is null or empty.", nameof(s));
        var clean = s.Trim().TrimStart('0', 'x', 'X');
        if (string.IsNullOrEmpty(clean))
            return 0; // "0x0" or "0" → valid zero address
        if (!ulong.TryParse(clean, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var addr))
            throw new FormatException($"Invalid hex address: '{s}' — expected format like 0x1000 or DEAD.");
        return addr;
    }
}
