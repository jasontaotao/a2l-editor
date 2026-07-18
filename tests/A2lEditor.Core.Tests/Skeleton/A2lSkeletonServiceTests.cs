using System.Linq;
using A2lEditor.Core.Skeleton;
using FluentAssertions;
using Xunit;

namespace A2lEditor.Core.Tests.Skeleton;

public class A2lSkeletonServiceTests
{
    private readonly A2lSkeletonService _sut = new();

    [Fact]
    public void Generate_SingleMeasurement_CreatesDoc()
    {
        using var excel = new TempExcelFile(ws =>
        {
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "LongId";
            ws.Cell(1, 3).Value = "BlockType";
            ws.Cell(2, 1).Value = "Battery_Voltage";
            ws.Cell(2, 2).Value = "Battery voltage measurement";
            ws.Cell(2, 4).Value = "UWORD";
            ws.Cell(2, 5).Value = "CM_uint16";
            ws.Cell(2, 6).Value = "0";
            ws.Cell(2, 7).Value = "500";
            ws.Cell(2, 8).Value = "0x1000";
        });

        var doc = _sut.GenerateFromExcel(excel.Path);

        doc.Modules.Should().HaveCount(1);
        doc.TotalMeasurementCount.Should().Be(1);
        var meas = doc.Modules[0].Measurements[0];
        meas.Name.Should().Be("Battery_Voltage");
        meas.LongIdentifier.Should().Be("Battery voltage measurement");
        meas.DataType.Should().Be(A2lEditor.Core.Model.A2lDataType.UWORD);
        meas.CompuMethod.Should().Be("CM_uint16");
        meas.EcuAddress.Should().Be(0x1000UL);
        meas.UpperLimit.Should().Be("500");
    }

    [Fact]
    public void Generate_MultipleRows_CreatesAll()
    {
        using var excel = new TempExcelFile(ws =>
        {
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(2, 1).Value = "V";
            ws.Cell(3, 1).Value = "I";
            ws.Cell(4, 1).Value = "T";
        });

        var doc = _sut.GenerateFromExcel(excel.Path);

        doc.TotalMeasurementCount.Should().Be(3);
        doc.Modules[0].Measurements.Select(m => m.Name)
            .Should().BeEquivalentTo("V", "I", "T");
    }

    [Fact]
    public void Generate_CharacteristicRow_CreatesCharacteristic()
    {
        using var excel = new TempExcelFile(ws =>
        {
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "BlockType";
            ws.Cell(2, 1).Value = "MaxCurrent";
            ws.Cell(2, 3).Value = "CHARACTERISTIC";
            ws.Cell(2, 6).Value = "0";
            ws.Cell(2, 7).Value = "2000";
        });

        var doc = _sut.GenerateFromExcel(excel.Path);

        doc.TotalMeasurementCount.Should().Be(0);
        doc.Modules[0].Characteristics.Should().HaveCount(1);
        doc.Modules[0].Characteristics[0].Name.Should().Be("MaxCurrent");
    }

    [Fact]
    public void Generate_CompuMethodDetected_CreatesCompuMethod()
    {
        using var excel = new TempExcelFile(ws =>
        {
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(2, 1).Value = "V";
            ws.Cell(2, 5).Value = "CM_Voltage";
            ws.Cell(3, 1).Value = "I";
            ws.Cell(3, 5).Value = "CM_Voltage";
            ws.Cell(4, 1).Value = "T";
            ws.Cell(4, 5).Value = "CM_Temp";
        });

        var doc = _sut.GenerateFromExcel(excel.Path);

        doc.Modules[0].CompuMethods.Should().HaveCount(2);
        doc.Modules[0].CompuMethods.Select(c => c.Name)
            .Should().BeEquivalentTo("CM_Voltage", "CM_Temp");
    }

    [Fact]
    public void Generate_SpecificModuleName_UsesGivenName()
    {
        using var excel = new TempExcelFile(ws =>
        {
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(2, 1).Value = "V";
        });

        var doc = _sut.GenerateFromExcel(excel.Path,
            new SkeletonGenerateOptions(ModuleName: "BMS_Module"));

        doc.Modules[0].Name.Should().Be("BMS_Module");
    }

    [Fact]
    public void Generate_SpecificSheet_UsesThatSheet()
    {
        using var excel = new TempExcelFile(ws =>
        {
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(2, 1).Value = "V";
        }, sheetName: "Signals");

        var doc = _sut.GenerateFromExcel(excel.Path,
            new SkeletonGenerateOptions(SheetName: "Signals"));

        doc.TotalMeasurementCount.Should().Be(1);
    }
}
