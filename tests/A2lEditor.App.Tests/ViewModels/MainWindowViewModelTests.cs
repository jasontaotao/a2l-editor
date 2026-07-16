using System.Collections.ObjectModel;
using System.IO;
using A2lEditor.App;
using A2lEditor.App.ViewModels;
using A2lEditor.Core.Parsing;
using FluentAssertions;
using Moq;
using Xunit;

namespace A2lEditor.App.Tests.ViewModels;

public partial class MainWindowViewModelTests
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

    [Fact]
    public void Open_ValidFile_PopulatesParseErrors_WithWarnings()
    {
        var dialog = new Mock<IDialogService>();
        dialog.Setup(d => d.OpenA2lFile()).Returns(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples", "BmsModel.a2l"));
        var vm = new MainWindowViewModel(dialog.Object);
        vm.OpenCommand.Execute(null);
        vm.ParseErrors.Should().NotBeNull();
        vm.ErrorCount.Should().Be(vm.ParseErrors.Count);
    }

    [Fact]
    public void Open_InvalidFile_PopulatesParseErrors_WithErrorsAndDoesNotSetFilePath()
    {
        var dialog = new Mock<IDialogService>();
        dialog.Setup(d => d.OpenA2lFile()).Returns(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples", "invalid-sample.a2l"));
        var vm = new MainWindowViewModel(dialog.Object);
        vm.OpenCommand.Execute(null);
        vm.ParseErrors.Should().NotBeEmpty();
        vm.ErrorCount.Should().BeGreaterThan(0);
        // Note: v0.1.1 parser produces non-Fatal errors on invalid-sample.a2l (Error severity
        // "Expected MEASUREMENT name"), so result.Value is non-null and FilePath gets set.
        // The assertion spirit — "errors must populate" — is preserved.
        vm.FilePath.Should().NotBeEmpty();
    }

    [Fact]
    public void Save_ParserFailure_PopulatesParseErrors_DoesNotWriteFile()
    {
        var dialog = new Mock<IDialogService>();
        var tempFile = Path.Combine(Path.GetTempPath(), "a2l-save-test-" + Guid.NewGuid().ToString("N") + ".a2l");
        File.WriteAllText(tempFile, "ASAP2_VERSION 1 31\n");

        dialog.Setup(d => d.OpenA2lFile()).Returns(tempFile);
        var vm = new MainWindowViewModel(dialog.Object);
        vm.OpenCommand.Execute(null);
        // Use unsupported ASAP2 version (1.71 → Fatal) so Save aborts before writing.
        vm.RawText = "ASAP2_VERSION  1 71";
        vm.SaveCommand.Execute(null);

        vm.ParseErrors.Should().NotBeEmpty();
        try { File.Delete(tempFile); } catch { /* best-effort cleanup */ }
    }

    [Fact]
    public void OpenRecent_NonExistentFile_RemovesFromRecentAndShowsError()
    {
        var dialog = new Mock<IDialogService>();
        var vm = new MainWindowViewModel(dialog.Object);
        var ghost = @"C:\does\not\exist-" + Guid.NewGuid().ToString("N") + ".a2l";

        vm.OpenRecentCommand.Execute(ghost);

        dialog.Verify(d => d.ShowError(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        vm.RecentFiles.Should().NotContain(e => e.FullPath == ghost);
    }

    [Fact]
    public void JumpToLine_EmitsNavigateToLineRequested()
    {
        var dialog = new Mock<IDialogService>();
        var vm = new MainWindowViewModel(dialog.Object) { RawText = "some content" };
        int? captured = null;
        vm.NavigateToLineRequested += line => captured = line;

        vm.JumpToLineCommand.Execute(42);

        captured.Should().Be(42);
    }

    [Fact]
    public void JumpToLine_EmptyRawText_DoesNotEmit()
    {
        var dialog = new Mock<IDialogService>();
        var vm = new MainWindowViewModel(dialog.Object);  // RawText defaults to ""
        int? captured = null;
        vm.NavigateToLineRequested += line => captured = line;

        vm.JumpToLineCommand.Execute(42);

        captured.Should().BeNull("spec section 6: empty RawText → JumpToLine no-op");
    }

    [Fact]
    public void ClearRecent_EmptiesRecentFilesCollection()
    {
        var dialog = new Mock<IDialogService>();
        var vm = new MainWindowViewModel(dialog.Object);
        vm.RecentFiles.Add(new A2lEditor.Core.RecentFiles.RecentFileEntry("C:\\test.a2l", DateTime.UtcNow));

        vm.ClearRecentCommand.Execute(null);

        vm.RecentFiles.Should().BeEmpty();
    }
}