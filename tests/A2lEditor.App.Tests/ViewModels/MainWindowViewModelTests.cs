using A2lEditor.App;
using A2lEditor.App.ViewModels;
using FluentAssertions;
using Moq;
using Xunit;

namespace A2lEditor.App.Tests.ViewModels;

public class MainWindowViewModelTests
{
    private static string SamplesDir() =>
        Path.Combine(AppContext.BaseDirectory, "samples");

    [Fact]
    public void OpenCommand_LoadsFile_UpdatesTitleAndModuleSummary()
    {
        var mockDlg = new Mock<IDialogService>();
        // FIX (Plan v0.1.1): absolute path anchored to test bin directory.
        mockDlg.Setup(d => d.OpenA2lFile())
            .Returns(Path.Combine(SamplesDir(), "BmsModel.a2l"));
        var vm = new MainWindowViewModel(mockDlg.Object);
        vm.OpenCommand.Execute(null);
        vm.Title.Should().Contain("BmsModel.a2l");
        vm.RawText.Should().NotBeEmpty();
        vm.IsDirty.Should().BeFalse();
        vm.ModuleSummary.Should().Contain("signals");
    }

    [Fact]
    public void SaveCommand_WithoutOpenFile_DoesNothing()
    {
        var mockDlg = new Mock<IDialogService>();
        var vm = new MainWindowViewModel(mockDlg.Object);
        vm.SaveCommand.Execute(null);
        mockDlg.Verify(d => d.ShowError(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void OpenCommand_UserCancelsDialog_NoStateChange()
    {
        var mockDlg = new Mock<IDialogService>();
        mockDlg.Setup(d => d.OpenA2lFile()).Returns((string?)null);
        var vm = new MainWindowViewModel(mockDlg.Object);
        vm.OpenCommand.Execute(null);
        vm.Title.Should().Be("a2l-editor — (no file)");
    }
}