using System.Windows;
using System.Windows.Controls;
using A2lEditor.App.Controls;
using A2lEditor.App.Highlighting;
using A2lEditor.App.ViewModels;
using A2lEditor.Core.Model;
using A2lEditor.Core.Parsing;

namespace A2lEditor.App;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _vm;
    private A2lSyntaxHighlighter? _highlighter;

    public MainWindow() => InitializeComponent();

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        if (DataContext is MainWindowViewModel vm)
        {
            _vm = vm;

            // v0.1.1 perf fix: rebuild tree only on FilePath change.
            vm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.FilePath))
                    RebuildTree(vm);
            };

            // Bind button.IsEnabled to Open/Save CanExecute.
            vm.OpenCommand.CanExecuteChanged += (_, _) => OpenButton.IsEnabled = vm.OpenCommand.CanExecute(null);
            vm.SaveCommand.CanExecuteChanged += (_, _) => SaveButton.IsEnabled = vm.SaveCommand.CanExecute(null);

            // Wire navigate-to-line event from VM to editor.
            vm.NavigateToLineRequested += line =>
            {
                TextEditor.ScrollToLine(line);
                TextEditor.HighlightLine(line);
            };

            // Attach syntax highlighter.
            _highlighter = new A2lSyntaxHighlighter();
            TextEditor.Editor.TextArea.TextView.LineTransformers.Add(_highlighter);
            TextEditor.Editor.TextChanged += (_, _) =>
            {
                if (_vm is not null) _highlighter!.Refresh(_vm.RawText);
            };
            _highlighter.Refresh(vm.RawText);
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
                IsExpanded = true,
                Tag = module
            };
            foreach (var meas in module.Measurements)
            {
                moduleItem.Items.Add(new TreeViewItem
                {
                    Header = $"  MEASUREMENT {meas.Name}",
                    Tag = meas
                });
            }
            foreach (var ch in module.Characteristics)
            {
                moduleItem.Items.Add(new TreeViewItem
                {
                    Header = $"  CHARACTERISTIC {ch.Name}",
                    Tag = ch
                });
            }
            ModuleTree.Items.Add(moduleItem);
        }
    }

    private void ModuleTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is TreeViewItem item && item.Tag is A2lMeasurement meas && meas.SourceLines.Start > 0)
        {
            _vm?.JumpToLineCommand.Execute(meas.SourceLines.Start);
        }
        else if (e.NewValue is TreeViewItem item2 && item2.Tag is A2lCharacteristic ch && ch.SourceLines.Start > 0)
        {
            _vm?.JumpToLineCommand.Execute(ch.SourceLines.Start);
        }
    }
}