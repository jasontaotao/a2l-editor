using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using A2lEditor.App.ViewModels;
using A2lEditor.Core.Parsing;

namespace A2lEditor.App;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _vm;

    public MainWindow() => InitializeComponent();

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        if (DataContext is MainWindowViewModel vm)
        {
            _vm = vm;

            // v0.1.1 perf fix: rebuild tree only on FilePath change (= Open succeeded),
            // NOT on every RawText change (= every keystroke).
            vm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.FilePath))
                    RebuildTree(vm);
            };

            // RelayCommand notifies CanExecuteChanged when NotifyCanExecuteChangedFor is wired.
            // Bind button.IsEnabled so Open/Save buttons reflect command CanExecute state.
            vm.OpenCommand.CanExecuteChanged += (_, _) => OpenButton.IsEnabled = vm.OpenCommand.CanExecute(null);
            vm.SaveCommand.CanExecuteChanged += (_, _) => SaveButton.IsEnabled = vm.SaveCommand.CanExecute(null);
        }
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e) => Close();

    private void RebuildTree(MainWindowViewModel vm)
    {
        ModuleTree.Items.Clear();
        if (string.IsNullOrEmpty(vm.RawText)) return;
        var result = Asap131Parser.ParseText(vm.RawText);
        if (result.Value is null) return;
        foreach (var module in result.Value.Modules)
        {
            var moduleItem = new TreeViewItem
            {
                Header = $"MODULE {module.Name} ({module.Measurements.Count + module.Characteristics.Count} signals)",
                IsExpanded = true
            };
            foreach (var meas in module.Measurements)
                moduleItem.Items.Add(new TreeViewItem { Header = $"  MEASUREMENT {meas.Name}" });
            foreach (var ch in module.Characteristics)
                moduleItem.Items.Add(new TreeViewItem { Header = $"  CHARACTERISTIC {ch.Name}" });
            ModuleTree.Items.Add(moduleItem);
        }
    }
}