using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using A2lEditor.App;
using A2lEditor.App.Tests.Controls;
using FluentAssertions;
using Xunit;

namespace A2lEditor.App.Tests;

/// <summary>
/// Menu skeleton tests for MainWindow (v0.7 Task 2).
///
/// Verifies the 5 top-level menus (File / Edit / View / Tools / Help) plus key
/// sub-items. All tests run on STA via StaRunner because MainWindow's ctor
/// constructs WPF FrameworkElements which require single-threaded apartment.
/// </summary>
public class MainWindowMenuTests
{
    [Fact]
    public void Menu_HasFiveTopLevelItems()
    {
        StaRunner.Run(() =>
        {
            var window = new MainWindow();
            var named = window.FindName("MainMenu");
            named.Should().NotBeNull("MainWindow must expose a named 'MainMenu' element");
            var menu = (Menu)named;
            menu.Items.Count.Should().Be(5);
            ((MenuItem)menu.Items[0]).Header.ToString().Should().Contain("File");
            ((MenuItem)menu.Items[1]).Header.ToString().Should().Contain("Edit");
            ((MenuItem)menu.Items[2]).Header.ToString().Should().Contain("View");
            ((MenuItem)menu.Items[3]).Header.ToString().Should().Contain("Tools");
            ((MenuItem)menu.Items[4]).Header.ToString().Should().Contain("Help");
        });
    }

    [Fact]
    public void EditMenu_HasUndoRedoCutCopyPasteFind()
    {
        StaRunner.Run(() =>
        {
            var window = new MainWindow();
            var menu = (Menu)window.FindName("MainMenu")!;
            var editMenu = (MenuItem)menu.Items[1];
            editMenu.Header.ToString().Should().Contain("Edit");
            var subItems = editMenu.Items.OfType<MenuItem>().Select(m => m.Header.ToString()).ToList();
            subItems.Should().Contain(h => h.Replace("_", "").Contains("Undo"));
            subItems.Should().Contain(h => h.Replace("_", "").Contains("Redo"));
            subItems.Should().Contain(h => h.Replace("_", "").Contains("Cut"));
            subItems.Should().Contain(h => h.Replace("_", "").Contains("Copy"));
            subItems.Should().Contain(h => h.Replace("_", "").Contains("Paste"));
            subItems.Should().Contain(h => h.Replace("_", "").Contains("Find"));
        });
    }

    [Fact]
    public void ToolsMenu_HasValidateAndFormat()
    {
        StaRunner.Run(() =>
        {
            var window = new MainWindow();
            var menu = (Menu)window.FindName("MainMenu")!;
            var toolsMenu = (MenuItem)menu.Items[3];
            toolsMenu.Header.ToString().Should().Contain("Tools");
            var subItems = toolsMenu.Items.OfType<MenuItem>().Select(m => m.Header.ToString()).ToList();
            subItems.Should().Contain(h => h.Contains("Validate"));
            subItems.Should().Contain(h => h.Contains("Format"));
        });
    }
}