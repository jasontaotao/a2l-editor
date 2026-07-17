using A2lEditor.Reuse;
using FluentAssertions;
using Xunit;

namespace A2lEditor.Core.Tests.Map;

public class MapSymbolTableAdapterTests
{
    private readonly MapSymbolTableAdapter _sut = new();

    [Fact]
    public void LoadSymbols_IarMap_ReturnsExpectedSymbols()
    {
        var symbols = _sut.LoadSymbols("samples/MiniMapFixture.iar.map");
        symbols.Should().HaveCount(3);
        symbols.Select(s => s.Name).Should().Contain(new[] { "Battery_Voltage", "Cell_Temperature", "Pack_Current" });
        symbols.First(s => s.Name == "Battery_Voltage").Address.Should().Be(0x08000000UL);
    }

    [Fact]
    public void LoadSymbols_HightechMap_ReturnsExpectedSymbols()
    {
        var symbols = _sut.LoadSymbols("samples/MiniMapFixture.hightech.map");
        symbols.Should().HaveCount(3);
        symbols.Should().Contain(s => s.Name == "Battery_Voltage");
    }

    [Fact]
    public void LoadSymbols_GccMap_ReturnsExpectedSymbols()
    {
        var symbols = _sut.LoadSymbols("samples/MiniMapFixture.gcc.map");
        symbols.Should().HaveCount(3);
        symbols.Should().Contain(s => s.Name == "Battery_Voltage");
    }

    [Fact]
    public void LoadSymbols_ElfFile_ReturnsExpectedSymbols()
    {
        var symbols = _sut.LoadSymbols("samples/MiniMapFixture.elf");
        symbols.Should().HaveCount(3);
        symbols.Should().Contain(s => s.Name == "Battery_Voltage");
    }
}
