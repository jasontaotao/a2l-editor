using A2lEditor.App.Commands;
using A2lEditor.App.Views;
using FluentAssertions;
using Xunit;

namespace A2lEditor.App.Tests;

public class ImportExcelCommandTests
{
    [Fact]
    public void ImportFromExcelCommand_CanExecute_IsTrue()
    {
        AppCommands.ImportFromExcel.Should().NotBeNull();
    }

    [Fact]
    public void FormatPreview_WithData_ShowsCounts()
    {
        var text = ImportExcelDialog.FormatPreview(
            "signals.xlsx", "BMS_Module",
            12, 3, 5);

        text.Should().Contain("signals.xlsx");
        text.Should().Contain("BMS_Module");
        text.Should().Contain("MEASUREMENTS:   12");
        text.Should().Contain("CHARACTERISTICS: 3");
        text.Should().Contain("COMPU_METHODS:  5");
    }

    [Fact]
    public void FormatPreview_EmptyCounts_ShowsZeros()
    {
        var text = ImportExcelDialog.FormatPreview(
            "empty.xlsx", "Module", 0, 0, 0);

        text.Should().Contain("MEASUREMENTS:   0");
        text.Should().Contain("CHARACTERISTICS: 0");
        text.Should().Contain("COMPU_METHODS:  0");
    }
}
