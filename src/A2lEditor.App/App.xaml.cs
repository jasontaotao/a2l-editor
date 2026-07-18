using System.Windows;
using A2lEditor.App.ViewModels;
using A2lEditor.Core.Services;
using A2lEditor.Reuse;
using Microsoft.Extensions.DependencyInjection;

namespace A2lEditor.App;

public partial class App : Application
{
    public IServiceProvider Services { get; }

    public App()
    {
        var sc = new ServiceCollection();
        sc.AddSingleton<IDialogService, WpfDialogService>();
        sc.AddSingleton<IMapSymbolTableAdapter, MapSymbolTableAdapter>();
        sc.AddSingleton<IMapAlignmentService, MapAlignmentService>();
        sc.AddSingleton<MainWindowViewModel>();
        Services = sc.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var vm = Services.GetRequiredService<MainWindowViewModel>();
        var window = new MainWindow { DataContext = vm };
        MainWindow = window;
        window.Show();
    }
}