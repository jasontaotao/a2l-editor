using A2lEditor.App.Controls;
using FluentAssertions;
using Xunit;

namespace A2lEditor.App.Tests.Controls;

public class A2lTextEditorNavigationTests
{
    [Fact]
    public void ScrollToLine_OutOfRangeLine_DoesNotThrow()
    {
        StaRunner.Run(() =>
        {
            var editor = new A2lTextEditor { Text = "line1\nline2\n" };
            var act = () => editor.ScrollToLine(999);
            act.Should().NotThrow();
        });
    }

    [Fact]
    public void ScrollToLine_EmptyText_DoesNotThrow()
    {
        StaRunner.Run(() =>
        {
            var editor = new A2lTextEditor();
            var act = () => editor.ScrollToLine(1);
            act.Should().NotThrow();
        });
    }

    [Fact]
    public void HighlightLine_OutOfRangeLine_DoesNotThrow()
    {
        StaRunner.Run(() =>
        {
            var editor = new A2lTextEditor { Text = "line1" };
            var act = () => editor.HighlightLine(999);
            act.Should().NotThrow();
        });
    }
}