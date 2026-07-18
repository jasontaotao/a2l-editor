using System;
using System.Collections.Generic;
using A2lEditor.Core.Model;
using A2lEditor.Core.Serialization;
using A2lEditor.Core.Tests.Diff;
using FluentAssertions;
using Xunit;

namespace A2lEditor.Core.Tests.Serialization;

public class A2lDocumentSerializerTests
{
    private static readonly A2lDocumentSerializer Serializer = new();

    // ====================================================================
    // JSON round-trip
    // ====================================================================

    [Fact]
    public void JsonRoundTrip_EmptyDocument_ProducesValidJson()
    {
        var doc = DiffFixtures.DocWith();

        var json = Serializer.SerializeToJson(doc);
        var back = Serializer.DeserializeFromJson(json);

        back.Version.Should().Be(A2lVersion.V1_31);
        back.ProjectName.Should().Be("TestProject");
        back.ProjectComment.Should().Be("Comment");
        back.ModCommon.Should().BeNull();
        back.Modules.Should().HaveCount(1);
        back.Modules[0].Name.Should().Be("TestModule");
        back.Modules[0].Measurements.Should().BeEmpty();
        back.Modules[0].Characteristics.Should().BeEmpty();
    }

    [Fact]
    public void JsonRoundTrip_MeasurementsAndCharacteristics_PreservesAll()
    {
        var doc = DiffFixtures.DocWith(
            measurements: new[] { DiffFixtures.Meas("V"), DiffFixtures.Meas("I") },
            characteristics: new[] { DiffFixtures.Char("C1"), DiffFixtures.Char("C2") });

        var json = Serializer.SerializeToJson(doc);
        var back = Serializer.DeserializeFromJson(json);

        back.Modules[0].Measurements.Should().HaveCount(2);
        back.Modules[0].Characteristics.Should().HaveCount(2);
        back.Modules[0].Measurements[0].Name.Should().Be("V");
        back.Modules[0].Measurements[1].Name.Should().Be("I");
        back.Modules[0].Characteristics[0].Name.Should().Be("C1");
        back.Modules[0].Characteristics[1].Name.Should().Be("C2");
    }

    [Fact]
    public void JsonRoundTrip_FullDocument_RoundTripsFields()
    {
        var doc = DiffFixtures.DocWith(
            measurements: new[] { DiffFixtures.Meas("Speed", "CM_Linear", 0x1000) },
            characteristics: new[] { DiffFixtures.Char("Gain") },
            compuMethods: new[] { DiffFixtures.Compu("CM_Linear") },
            recordLayouts: new[] { DiffFixtures.Layout("Scalar_UBYTE") },
            groups: new[] { DiffFixtures.Group("MainGroup", refMeasurements: new[] { "Speed" }) },
            modCommon: new A2lModCommon("BMS", A2lByteOrder.MSB_LAST, null, null, null, new LineRange(0, 0)),
            modPar: "MOD_PAR comment",
            axisPts: Array.Empty<A2lAxisPts>(),
            axisPtsX: Array.Empty<A2lAxisPtsX>(),
            axisDescr: Array.Empty<A2lAxisDescr>(),
            userRights: Array.Empty<A2lUserRights>(),
            versionInfo: Array.Empty<A2lVersionInfo>());

        var json = Serializer.SerializeToJson(doc);
        var back = Serializer.DeserializeFromJson(json);

        back.ProjectName.Should().Be("TestProject");
        back.Modules.Should().HaveCount(1);

        var m = back.Modules[0];
        m.Name.Should().Be("TestModule");
        m.Measurements.Should().HaveCount(1);
        m.Measurements[0].Name.Should().Be("Speed");
        m.Measurements[0].CompuMethod.Should().Be("CM_Linear");
        m.Measurements[0].EcuAddress.Should().Be(0x1000);
        m.Characteristics.Should().HaveCount(1);
        m.Characteristics[0].Name.Should().Be("Gain");
        m.CompuMethods.Should().HaveCount(1);
        m.CompuMethods[0].Name.Should().Be("CM_Linear");
        m.CompuMethods[0].CoeffA.Should().Be(1.0);
        m.RecordLayouts.Should().HaveCount(1);
        m.RecordLayouts[0].Name.Should().Be("Scalar_UBYTE");
        m.RecordLayouts[0].Entries.Should().HaveCount(1);
        m.RecordLayouts[0].Entries[0].Keyword.Should().Be("NO_OF_BITS");
        m.Groups.Should().HaveCount(1);
        m.Groups[0].Name.Should().Be("MainGroup");
        m.Groups[0].RefMeasurements.Should().Contain("Speed");
        m.ModPar.Should().Be("MOD_PAR comment");
    }

    [Fact]
    public void JsonRoundTrip_ModCommon_WithOptionalFields_PreservesAll()
    {
        var modCommon = new A2lModCommon(
            "BMS settings",
            A2lByteOrder.MSB_FIRST,
            4,  // DataSize
            A2lByteOrder.MSB_LAST,  // AlignmentByteOrder
            8,  // AlignmentOffset
            new LineRange(0, 0));

        var doc = DiffFixtures.DocWith(modCommon: modCommon);

        var json = Serializer.SerializeToJson(doc);
        var back = Serializer.DeserializeFromJson(json);

        var mc = back.ModCommon;
        mc.Should().NotBeNull();
        mc!.ByteOrder.Should().Be(A2lByteOrder.MSB_FIRST);
        mc!.DataSize.Should().Be(4);
        mc!.AlignmentByteOrder.Should().Be(A2lByteOrder.MSB_LAST);
        mc!.AlignmentOffset.Should().Be(8);
    }

    [Fact]
    public void JsonRoundTrip_VersionInfo_PreservesDate()
    {
        var date = new DateTime(2026, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        var doc = DiffFixtures.DocWith(
            versionInfo: new[]
            {
                new A2lVersionInfo("1.0", date, "VendorA", "Initial release", new LineRange(0, 0))
            });

        var json = Serializer.SerializeToJson(doc);
        var back = Serializer.DeserializeFromJson(json);

        back.Modules[0].VersionInfo.Should().HaveCount(1);
        back.Modules[0].VersionInfo[0].VersionNo.Should().Be("1.0");
        back.Modules[0].VersionInfo[0].Date.Should().Be(date);
        back.Modules[0].VersionInfo[0].Vendor.Should().Be("VendorA");
    }

    // ====================================================================
    // XML round-trip
    // ====================================================================

    [Fact]
    public void XmlRoundTrip_EmptyDocument_PreservesStructure()
    {
        var doc = DiffFixtures.DocWith();

        var xml = Serializer.SerializeToXml(doc);
        var back = Serializer.DeserializeFromXml(xml);

        back.ProjectName.Should().Be("TestProject");
        back.HeaderComment.Should().Be("Header");
        back.ModCommon.Should().BeNull();
        back.Modules.Should().HaveCount(1);
        back.Modules[0].Name.Should().Be("TestModule");
        back.Modules[0].Measurements.Should().BeEmpty();
        back.Modules[0].Characteristics.Should().BeEmpty();
    }

    [Fact]
    public void XmlRoundTrip_FullDocument_RoundTripsFields()
    {
        var doc = DiffFixtures.DocWith(
            measurements: new[] { DiffFixtures.Meas("V") },
            characteristics: new[] { DiffFixtures.Char("C1") },
            compuMethods: new[] { DiffFixtures.Compu("CM1") },
            recordLayouts: new[] { DiffFixtures.Layout("Scalar_UBYTE") },
            modCommon: new A2lModCommon("BMS", A2lByteOrder.MSB_LAST, null, null, null, new LineRange(0, 0)),
            modPar: "ModPar value");

        var xml = Serializer.SerializeToXml(doc);
        var back = Serializer.DeserializeFromXml(xml);

        back.ProjectName.Should().Be("TestProject");
        back.Modules.Should().HaveCount(1);
        back.Modules[0].Name.Should().Be("TestModule");
        back.Modules[0].Measurements.Should().HaveCount(1);
        back.Modules[0].Measurements[0].Name.Should().Be("V");
        back.Modules[0].Characteristics.Should().HaveCount(1);
        back.Modules[0].Characteristics[0].Name.Should().Be("C1");
        back.Modules[0].CompuMethods.Should().HaveCount(1);
        back.Modules[0].CompuMethods[0].Name.Should().Be("CM1");
        back.Modules[0].RecordLayouts.Should().HaveCount(1);
        back.Modules[0].RecordLayouts[0].Name.Should().Be("Scalar_UBYTE");
        back.Modules[0].ModPar.Should().Be("ModPar value");
    }

    [Fact]
    public void XmlRoundTrip_ModCommonNullableFields_AreOptional()
    {
        var doc = DiffFixtures.DocWith(
            modCommon: new A2lModCommon("test", A2lByteOrder.MSB_LAST, null, null, null, new LineRange(0, 0)));

        var xml = Serializer.SerializeToXml(doc);
        var back = Serializer.DeserializeFromXml(xml);

        var mc = back.ModCommon;
        mc.Should().NotBeNull();
        mc!.DataSize.Should().BeNull();
        mc!.AlignmentByteOrder.Should().BeNull();
        mc!.AlignmentOffset.Should().BeNull();
    }
}
