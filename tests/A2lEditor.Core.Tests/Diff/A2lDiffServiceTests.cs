using A2lEditor.Core.Diff;
using A2lEditor.Core.Model;
using FluentAssertions;
using Xunit;

namespace A2lEditor.Core.Tests.Diff;

public class A2lDiffServiceTests
{
    private readonly A2lDiffService _sut = new();

    // ========================================================
    // Test 1: 相同文件 → 全部 Unchanged
    // ========================================================

    [Fact]
    public void DiffDocuments_IdenticalFiles_AllUnchanged()
    {
        var doc = DiffFixtures.DocWith(
            measurements: new[] { DiffFixtures.Meas("V") },
            characteristics: new[] { DiffFixtures.Char("C") });

        var report = _sut.DiffDocuments(doc, doc);

        report.Kind.Should().Be(DiffKind.Unchanged);
        report.HasChanges.Should().BeFalse();
        report.TotalAdded.Should().Be(0);
        report.TotalRemoved.Should().Be(0);
        report.TotalModified.Should().Be(0);
        report.DocumentChanges.Should().BeEmpty();
        report.ModuleDiffs.Should().ContainSingle()
            .Which.Kind.Should().Be(DiffKind.Unchanged);
    }

    // ========================================================
    // Test 2: MEASUREMENT ECU_ADDRESS 变化
    // ========================================================

    [Fact]
    public void DiffDocuments_DifferentMeasurementEcuAddress_DetectsModification()
    {
        var left = DiffFixtures.DocWith(
            measurements: new[] { DiffFixtures.Meas("V", ecuAddress: 0x1000) });
        var right = DiffFixtures.DocWith(
            measurements: new[] { DiffFixtures.Meas("V", ecuAddress: 0x2000) });

        var report = _sut.DiffDocuments(left, right);

        report.Kind.Should().Be(DiffKind.Modified);
        report.TotalModified.Should().Be(1);
        var measDiff = report.ModuleDiffs[0].MeasurementDiffs[0];
        measDiff.Kind.Should().Be(DiffKind.Modified);
        measDiff.BlockName.Should().Be("V");
        measDiff.BlockType.Should().Be("MEASUREMENT");
        measDiff.FieldChanges.Should().Contain(fc =>
            fc.FieldName == "EcuAddress" &&
            fc.OldValue == "0x1000" &&
            fc.NewValue == "0x2000");
    }

    // ========================================================
    // Test 3: 新增 MEASUREMENT
    // ========================================================

    [Fact]
    public void DiffDocuments_AddedMeasurement_DetectsAdded()
    {
        var left = DiffFixtures.DocWith(
            measurements: new[] { DiffFixtures.Meas("V") });
        var right = DiffFixtures.DocWith(
            measurements: new[] { DiffFixtures.Meas("V"), DiffFixtures.Meas("I") });

        var report = _sut.DiffDocuments(left, right);

        report.TotalAdded.Should().Be(1);
        var added = report.ModuleDiffs[0].MeasurementDiffs
            .Single(b => b.Kind == DiffKind.Added);
        added.BlockName.Should().Be("I");
        added.BlockType.Should().Be("MEASUREMENT");
    }

    // ========================================================
    // Test 4: 删除 CHARACTERISTIC
    // ========================================================

    [Fact]
    public void DiffDocuments_RemovedCharacteristic_DetectsRemoved()
    {
        var left = DiffFixtures.DocWith(
            characteristics: new[] { DiffFixtures.Char("C") });
        var right = DiffFixtures.DocWith(
            characteristics: Array.Empty<A2lCharacteristic>());

        var report = _sut.DiffDocuments(left, right);

        report.TotalRemoved.Should().Be(1);
        var removed = report.ModuleDiffs[0].CharacteristicDiffs
            .Single(b => b.Kind == DiffKind.Removed);
        removed.BlockName.Should().Be("C");
    }

    // ========================================================
    // Test 5: 模块增减
    // ========================================================

    [Fact]
    public void DiffDocuments_AddedAndRemovedModule_DetectsModuleChanges()
    {
        var modA = new A2lModule("A", "", Array.Empty<A2lMeasurement>(),
            Array.Empty<A2lCharacteristic>(), Array.Empty<A2lAxisPts>(),
            Array.Empty<A2lCompuMethod>(), Array.Empty<A2lRecordLayout>(),
            Array.Empty<A2lGroup>(), null,
            Array.Empty<A2lAxisDescr>(), Array.Empty<A2lUserRights>(),
            Array.Empty<A2lVersionInfo>(), Array.Empty<A2lAxisPtsX>(),
            new LineRange(0, 0));
        var modB = modA with { Name = "B" };
        var modC = modA with { Name = "C" };

        var left = new A2lDocument(A2lVersion.V1_31, "P", "", "", null,
            new[] { modA, modB }, "", 0);
        var right = new A2lDocument(A2lVersion.V1_31, "P", "", "", null,
            new[] { modA, modC }, "", 0);

        var report = _sut.DiffDocuments(left, right);

        report.ModuleDiffs.Should().HaveCount(3); // A, B, C
        report.ModuleDiffs.Single(m => m.ModuleName == "A").Kind.Should().Be(DiffKind.Unchanged);
        report.ModuleDiffs.Single(m => m.ModuleName == "B").Kind.Should().Be(DiffKind.Removed);
        report.ModuleDiffs.Single(m => m.ModuleName == "C").Kind.Should().Be(DiffKind.Added);

        report.TotalAdded.Should().Be(0); // Module C is added, but it has no blocks inside — total added blocks = 0
        report.TotalRemoved.Should().Be(0);
    }

    // ========================================================
    // Test 6: 多字段同时变化
    // ========================================================

    [Fact]
    public void DiffDocuments_MultipleFieldChanges_AllCaptured()
    {
        var left = DiffFixtures.DocWith(
            measurements: new[] { new A2lMeasurement("V", "Voltage", A2lDataType.UBYTE, "CM1", "0.1", "1.0", "0", "100", 0x1000, new LineRange(0, 0)) });
        var right = DiffFixtures.DocWith(
            measurements: new[] { new A2lMeasurement("V", "Battery voltage", A2lDataType.SWORD, "CM2", "0.01", "0.5", "-50", "150", 0x2000, new LineRange(0, 0)) });

        var report = _sut.DiffDocuments(left, right);

        var changes = report.ModuleDiffs[0].MeasurementDiffs[0].FieldChanges;
        changes.Should().HaveCountGreaterThan(2);
        changes.Should().Contain(fc => fc.FieldName == "LongIdentifier");
        changes.Should().Contain(fc => fc.FieldName == "DataType");
        changes.Should().Contain(fc => fc.FieldName == "EcuAddress");
        changes.Should().Contain(fc => fc.FieldName == "LowerLimit");
    }

    // ========================================================
    // Test 7: COMPU_METHOD 系数变化
    // ========================================================

    [Fact]
    public void DiffDocuments_CompuMethodCoeffsChanged_DetectsModification()
    {
        var left = DiffFixtures.DocWith(
            compuMethods: new[] { DiffFixtures.Compu("VoltTable") });
        var right = DiffFixtures.DocWith(
            compuMethods: new[] { new A2lCompuMethod("VoltTable", "Voltage table", "LINEAR", "%.2", "V", 2.0, 1.0, 0.0, 0.0, 0.0, 0.0, new LineRange(0, 0)) });

        var report = _sut.DiffDocuments(left, right);

        report.TotalModified.Should().Be(1);
        var diff = report.ModuleDiffs[0].CompuMethodDiffs[0];
        diff.Kind.Should().Be(DiffKind.Modified);
        diff.FieldChanges.Should().Contain(fc => fc.FieldName == "CoeffA");
        diff.FieldChanges.Should().Contain(fc => fc.FieldName == "CoeffB");
    }

    // ========================================================
    // Test 8: RECORD_LAYOUT entry 变化
    // ========================================================

    [Fact]
    public void DiffDocuments_RecordLayoutEntryChanged_DetectsModification()
    {
        var left = DiffFixtures.DocWith(
            recordLayouts: new[]
            {
                new A2lRecordLayout("RL1", new[]
                {
                    new RecordLayoutEntry("NO_OF_BITS", 0, "UBYTE", "INDEX_INCR", "DIRECT", null, null),
                }, new LineRange(0, 0))
            });
        var right = DiffFixtures.DocWith(
            recordLayouts: new[]
            {
                new A2lRecordLayout("RL1", new[]
                {
                    new RecordLayoutEntry("NO_OF_BITS", 0, "SWORD", "INDEX_INCR", "DIRECT", null, null),
                }, new LineRange(0, 0))
            });

        var report = _sut.DiffDocuments(left, right);

        report.TotalModified.Should().Be(1);
        report.ModuleDiffs[0].RecordLayoutDiffs[0].Kind.Should().Be(DiffKind.Modified);
    }

    // ========================================================
    // Test 9: MOD_PAR 变化
    // ========================================================

    [Fact]
    public void DiffDocuments_ModParChanged_DetectsChange()
    {
        var left = DiffFixtures.DocWith(modPar: "original config");
        var right = DiffFixtures.DocWith(modPar: "updated config");

        var report = _sut.DiffDocuments(left, right);

        report.TotalModified.Should().Be(0); // ModPar is a module-level field, not counted in blocks
        report.ModuleDiffs[0].ModParChange.Should().NotBeNull();
        report.ModuleDiffs[0].ModParChange!.FieldName.Should().Be("ModPar");
        report.ModuleDiffs[0].ModParChange!.OldValue.Should().Contain("original");
        report.ModuleDiffs[0].ModParChange!.NewValue.Should().Contain("updated");
    }

    // ========================================================
    // Test 10: GROUP REF_MEASUREMENT 变化
    // ========================================================

    [Fact]
    public void DiffDocuments_GroupRefsChanged_DetectsChange()
    {
        var left = DiffFixtures.DocWith(
            groups: new[] { DiffFixtures.Group("G1", refMeasurements: new[] { "V", "I" }) });
        var right = DiffFixtures.DocWith(
            groups: new[] { DiffFixtures.Group("G1", refMeasurements: new[] { "V", "I", "T" }) });

        var report = _sut.DiffDocuments(left, right);

        report.TotalModified.Should().Be(1);
        var diff = report.ModuleDiffs[0].GroupDiffs[0];
        diff.Kind.Should().Be(DiffKind.Modified);
        diff.FieldChanges.Should().Contain(fc => fc.FieldName == "RefMeasurements");
    }
}
