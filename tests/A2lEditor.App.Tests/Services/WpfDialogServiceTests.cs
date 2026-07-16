using A2lEditor.App;
using FluentAssertions;
using Xunit;

namespace A2lEditor.App.Tests.Services;

public class WpfDialogServiceTests
{
    [Fact]
    public void WpfDialogService_CanConstruct() =>
        new WpfDialogService().Should().NotBeNull();
}