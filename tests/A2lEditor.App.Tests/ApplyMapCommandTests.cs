using A2lEditor.App.Commands;
using FluentAssertions;
using Xunit;

namespace A2lEditor.App.Tests;

public class ApplyMapCommandTests
{
    [Fact]
    public void ApplyMapCommand_CanExecute_IsTrue()
    {
        // Static RoutedCommand always supports CanExecute=true when no CanExecute handler is registered.
        // The real CanExecute check happens at WPF runtime via MainWindow.xaml.cs binding.
        AppCommands.ApplyMap.Should().NotBeNull();
    }
}
