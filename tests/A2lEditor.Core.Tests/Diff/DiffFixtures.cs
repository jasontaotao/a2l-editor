using System;
using System.Collections.Generic;
using A2lEditor.Core.Model;

namespace A2lEditor.Core.Tests.Diff;

internal static class DiffFixtures
{
    /// <summary>
    /// 造一个简单的单模块 A2lDocument（含 3 个 MEASUREMENT，2 个 CHARACTERISTIC，
    /// 1 个 COMPU_METHOD，1 个 RECORD_LAYOUT，1 个 GROUP）。
    /// </summary>
    public static A2lDocument DocWith(
        IReadOnlyList<A2lMeasurement>? measurements = null,
        IReadOnlyList<A2lCharacteristic>? characteristics = null,
        IReadOnlyList<A2lCompuMethod>? compuMethods = null,
        IReadOnlyList<A2lRecordLayout>? recordLayouts = null,
        IReadOnlyList<A2lGroup>? groups = null,
        IReadOnlyList<A2lAxisPts>? axisPts = null,
        IReadOnlyList<A2lAxisPtsX>? axisPtsX = null,
        IReadOnlyList<A2lAxisDescr>? axisDescr = null,
        IReadOnlyList<A2lUserRights>? userRights = null,
        IReadOnlyList<A2lVersionInfo>? versionInfo = null,
        string? modPar = null,
        A2lModCommon? modCommon = null,
        string projectName = "TestProject",
        string projectComment = "Comment")
    {
        var mod = new A2lModule(
            "TestModule",
            "Module comment",
            measurements ?? Array.Empty<A2lMeasurement>(),
            characteristics ?? Array.Empty<A2lCharacteristic>(),
            axisPts ?? Array.Empty<A2lAxisPts>(),
            compuMethods ?? Array.Empty<A2lCompuMethod>(),
            recordLayouts ?? Array.Empty<A2lRecordLayout>(),
            groups ?? Array.Empty<A2lGroup>(),
            modPar,
            axisDescr ?? Array.Empty<A2lAxisDescr>(),
            userRights ?? Array.Empty<A2lUserRights>(),
            versionInfo ?? Array.Empty<A2lVersionInfo>(),
            axisPtsX ?? Array.Empty<A2lAxisPtsX>(),
            new LineRange(0, 0));

        return new A2lDocument(
            A2lVersion.V1_31,
            projectName,
            projectComment,
            "Header",
            modCommon,
            new[] { mod },
            "",  // RawText
            0);  // SourceLineCount
    }

    // ============= 常用测量值 =============

    public static A2lMeasurement Meas(string name, string compuMethod = "CM", ulong ecuAddress = 0x1000) =>
        new(name, $"{name} desc", A2lDataType.UBYTE, compuMethod, "1", "1", "0", "100", ecuAddress, new LineRange(0, 0));

    public static A2lCharacteristic Char(string name, ulong ecuAddress = 0x2000) =>
        new(name, $"{name} desc", "RL1", ecuAddress, "0", "100", new LineRange(0, 0));

    public static A2lCompuMethod Compu(string name, string conversionType = "LINEAR") =>
        new(name, $"{name} desc", conversionType, "%.2", "V", 1.0, 0.0, 0.0, 0.0, 0.0, 0.0, new LineRange(0, 0));

    public static A2lRecordLayout Layout(string name) =>
        new(name, new[]
        {
            new RecordLayoutEntry("NO_OF_BITS", 0, "UBYTE", "INDEX_INCR", "DIRECT", null, null),
        }, new LineRange(0, 0));

    public static A2lGroup Group(string name, bool isRoot = false,
        IReadOnlyList<string>? refMeasurements = null) =>
        new(name, $"{name} desc", isRoot,
            refMeasurements ?? Array.Empty<string>(),
            Array.Empty<string>(),
            new LineRange(0, 0));

    public static A2lAxisDescr AxisDescr(string attribute, string inputQuantity = "X") =>
        new(attribute, inputQuantity, "CM", 10, "0", "100", new LineRange(0, 0));

    public static A2lUserRights UserRight(string userId) =>
        new(userId, "READ", "WRITE", "DEFAULT", new LineRange(0, 0));

    public static A2lVersionInfo VersionInfo(string versionNo, string vendor = "Vendor") =>
        new(versionNo, new DateTime(2026, 7, 18), vendor, "desc", new LineRange(0, 0));

    public static A2lAxisPts AxisPt(string name, ulong ecuAddress = 0x3000) =>
        new(name, $"{name} desc", "RL1", ecuAddress, "IQ", "CM", 10, "0", "100", new LineRange(0, 0));

    public static A2lAxisPtsX AxisPtX(string name, ulong ecuAddress = 0x4000) =>
        new(name, $"{name} desc", "RL1", ecuAddress, "IQ", "CM", 20, "0", "100", new LineRange(0, 0));
}
