using A2lEditor.Core.Diff;
using A2lEditor.Core.Merge;
using A2lEditor.Core.Model;
using A2lEditor.Core.Tests.Diff;
using FluentAssertions;
using Xunit;

namespace A2lEditor.Core.Tests.Merge;

public class A2lMergeServiceTests
{
    private readonly A2lMergeService _sut = new(new A2lDiffService());

    // ========================================================
    // Test 1: 相同文件 → merged ≈ baseline
    // ========================================================

    [Fact]
    public void Merge_IdenticalFiles_ReturnsOriginal()
    {
        var doc = DiffFixtures.DocWith(
            measurements: new[] { DiffFixtures.Meas("V") },
            compuMethods: new[] { DiffFixtures.Compu("CM1") });

        var result = _sut.Merge(doc, doc);

        result.MergedDocument.Should().NotBeNull();
        result.MergedDocument!.Modules[0]
            .Should().Be(doc.Modules[0]);
    }

    // ========================================================
    // Test 2: MEASUREMENT 改 ECU_ADDRESS
    // ========================================================

    [Fact]
    public void Merge_ModifiedMeasurement_AppliesChange()
    {
        var baseline = DiffFixtures.DocWith(
            measurements: new[] { DiffFixtures.Meas("V", ecuAddress: 0x1000) });
        var modified = DiffFixtures.DocWith(
            measurements: new[] { DiffFixtures.Meas("V", ecuAddress: 0x2000) });

        var result = _sut.Merge(baseline, modified);

        result.AppliedCount.Should().Be(1);
        result.MergedDocument.Should().NotBeNull();
        result.MergedDocument!.Modules[0].Measurements[0]
            .EcuAddress.Should().Be(0x2000UL);
    }

    // ========================================================
    // Test 3: 新增 MEASUREMENT
    // ========================================================

    [Fact]
    public void Merge_AddedMeasurement_IncludedInResult()
    {
        var baseline = DiffFixtures.DocWith(
            measurements: new[] { DiffFixtures.Meas("V") });
        var modified = DiffFixtures.DocWith(
            measurements: new[] { DiffFixtures.Meas("V"), DiffFixtures.Meas("I") });

        var result = _sut.Merge(baseline, modified);

        result.AppliedCount.Should().Be(1);
        result.MergedDocument!.Modules[0].Measurements
            .Should().HaveCount(2);
        result.MergedDocument!.Modules[0].Measurements[1]
            .Name.Should().Be("I");
    }

    // ========================================================
    // Test 4: 删除的 block → baseline 版保留
    // ========================================================

    [Fact]
    public void Merge_RemovedBlock_KeptInBaseline()
    {
        var baseline = DiffFixtures.DocWith(
            characteristics: new[] { DiffFixtures.Char("C") });
        var modified = DiffFixtures.DocWith(
            characteristics: Array.Empty<A2lCharacteristic>());

        var result = _sut.Merge(baseline, modified);

        // two-way merge 不删除
        result.MergedDocument!.Modules[0].Characteristics
            .Should().Contain(c => c.Name == "C");
    }

    // ========================================================
    // Test 5: COMPU_METHOD 变化
    // ========================================================

    [Fact]
    public void Merge_ModifiedCompuMethod_AppliesChange()
    {
        var leftCm = new A2lCompuMethod("CM1", "desc", "LINEAR", "%.1", "V",
            1.0, 0.0, 0.0, 0.0, 0.0, 0.0, new LineRange(0, 0));
        var rightCm = new A2lCompuMethod("CM1", "desc", "LINEAR", "%.2", "mV",
            1.5, 0.0, 0.0, 0.0, 0.0, 0.0, new LineRange(0, 0));

        var baseline = DiffFixtures.DocWith(compuMethods: new[] { leftCm });
        var modified = DiffFixtures.DocWith(compuMethods: new[] { rightCm });

        var result = _sut.Merge(baseline, modified);

        result.AppliedCount.Should().Be(1);
        var merged = result.MergedDocument!.Modules[0].CompuMethods[0];
        merged.Format.Should().Be("%.2");
        merged.Unit.Should().Be("mV");
        merged.CoeffA.Should().Be(1.5);
    }

    // ========================================================
    // Test 6: 新增模块
    // ========================================================

    [Fact]
    public void Merge_AddedModule_Included()
    {
        var modB = new A2lModule("B", "",
            Array.Empty<A2lMeasurement>(), Array.Empty<A2lCharacteristic>(),
            Array.Empty<A2lAxisPts>(), Array.Empty<A2lCompuMethod>(),
            Array.Empty<A2lRecordLayout>(), Array.Empty<A2lGroup>(), null,
            Array.Empty<A2lAxisDescr>(), Array.Empty<A2lUserRights>(),
            Array.Empty<A2lVersionInfo>(), Array.Empty<A2lAxisPtsX>(),
            new LineRange(0, 0));

        var baseline = DiffFixtures.DocWith(measurements: new[] { DiffFixtures.Meas("V") });
        var modified = new A2lDocument(A2lVersion.V1_31, "P", "", "", null,
            new[] { baseline.Modules[0], modB }, "", 0);

        var result = _sut.Merge(baseline, modified);

        result.AppliedCount.Should().Be(1);
        result.MergedDocument!.Modules.Should().HaveCount(2);
        result.MergedDocument.Modules[1].Name.Should().Be("B");
    }
}
