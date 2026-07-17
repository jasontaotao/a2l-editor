using System.Windows;
using A2lEditor.App;
using A2lEditor.App.Tests.Controls;
using FluentAssertions;
using Xunit;

namespace A2lEditor.App.Tests;

/// <summary>
/// Drag-and-drop behavior tests for MainWindow (v0.7).
///
/// WPF's <see cref="System.Windows.DragEventArgs"/> has no public constructor, so
/// the drop decision logic is factored into testable helpers on MainWindow
/// (<c>ComputeDropEffects</c> / <c>TryGetDroppableA2lFile</c>) that accept an
/// <see cref="IDataObject"/>. WPF's <see cref="DataObject"/> IS publicly
/// constructable and implements <see cref="IDataObject"/>, so we drive the helpers
/// directly with real data objects. All tests run on an STA thread via StaRunner
/// (WPF Window ctor requires STA).
/// </summary>
public class MainWindowDragDropTests
{
    [Fact]
    public void DragEnter_NonFileDrop_EffectsIsNone()
    {
        StaRunner.Run(() =>
        {
            var window = new MainWindow();
            var data = new DataObject(DataFormats.UnicodeText, "hello");

            var effects = window.ComputeDropEffects(data);

            effects.Should().Be(DragDropEffects.None);
        });
    }

    [Fact]
    public void DragEnter_FileDropWithA2l_EffectsIsCopy()
    {
        StaRunner.Run(() =>
        {
            var window = new MainWindow();
            var data = new DataObject(DataFormats.FileDrop, new[] { @"C:\test.a2l" });

            var effects = window.ComputeDropEffects(data);

            effects.Should().Be(DragDropEffects.Copy);
        });
    }

    [Fact]
    public void Drop_A2lFile_ResolvesA2lPath()
    {
        StaRunner.Run(() =>
        {
            var window = new MainWindow();
            var data = new DataObject(
                DataFormats.FileDrop,
                new[] { @"C:\notes.txt", @"C:\model.a2l" });

            var resolved = window.TryGetDroppableA2lFile(data);

            resolved.Should().Be(@"C:\model.a2l");
        });
    }

    [Fact]
    public void Drop_NoA2lFile_ResolvesNull()
    {
        StaRunner.Run(() =>
        {
            var window = new MainWindow();
            var data = new DataObject(DataFormats.FileDrop, new[] { @"C:\notes.txt" });

            var resolved = window.TryGetDroppableA2lFile(data);

            resolved.Should().BeNull();
        });
    }
}
