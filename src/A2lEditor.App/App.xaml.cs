using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace A2lEditor.App;

public partial class App : Application
{
    public IServiceProvider Services { get; }

    public App()
    {
        var sc = new ServiceCollection();
        sc.AddSingleton<IDialogService, WpfDialogService>();
        Services = sc.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var vm = new ViewModels.MainWindowViewModel();
        var window = new MainWindow { DataContext = vm };
        MainWindow = window;
        window.Show();
    }
}
