using A2lEditor.App.Views;
using FluentAssertions;
using Xunit;

namespace A2lEditor.App.Tests;

public class MergeConflictDialogTests
{
    [Fact]
    public void ChangeItem_DefaultAccepted_IsTrue()
    {
        var item = new ChangeItem();
        item.Accepted.Should().BeTrue();
    }

    [Fact]
    public void ChangeItem_AddedKind_IsGreen()
    {
        var item = new ChangeItem { KindLabel = "ADDED" };
        item.KindColor.Should().Be(System.Windows.Media.Brushes.Green);
    }

    [Fact]
    public void ChangeItem_RemovedKind_IsRed()
    {
        var item = new ChangeItem { KindLabel = "REMOVED" };
        item.KindColor.Should().Be(System.Windows.Media.Brushes.Red);
    }

    [Fact]
    public void ChangeItem_ModifiedKind_IsDodgerBlue()
    {
        var item = new ChangeItem { KindLabel = "MODIFIED" };
        item.KindColor.Should().Be(System.Windows.Media.Brushes.DodgerBlue);
    }
}
