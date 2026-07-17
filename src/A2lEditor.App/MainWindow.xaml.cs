using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

    /// <summary>Current view model resolved from DataContext (null before DI wires it up).</summary>
    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

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

    // --- v0.7 drag-and-drop ---------------------------------------------------
    // 拖放事件从内层 DropTargetBorder 冒泡到 Window；处理器绑定在 Window 上。
    // 判定逻辑抽到 ComputeDropEffects / TryGetDroppableA2lFile 两个纯方法便于单测
    // （WPF 的 DragEventArgs 无公共构造器，无法直接在测试里构造）。

    private void OnDragEnter(object sender, DragEventArgs e)
    {
        e.Effects = ComputeDropEffects(e.Data);
        SetDropTargetVisualFeedback(e.Effects == DragDropEffects.Copy);
        e.Handled = true;
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = ComputeDropEffects(e.Data);
        e.Handled = true;
    }

    private void OnDragLeave(object sender, DragEventArgs e)
    {
        SetDropTargetVisualFeedback(false);
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        SetDropTargetVisualFeedback(false);
        var a2lFile = TryGetDroppableA2lFile(e.Data);
        if (a2lFile is null) return;
        ViewModel?.OpenRecentCommand.Execute(a2lFile);
        e.Handled = true;
    }

    /// <summary>Copy effect only for a FileDrop payload; otherwise None.</summary>
    internal DragDropEffects ComputeDropEffects(IDataObject data)
        => data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;

    /// <summary>First dropped path ending in <c>.a2l</c> (case-insensitive), or null.</summary>
    internal string? TryGetDroppableA2lFile(IDataObject data)
    {
        if (!data.GetDataPresent(DataFormats.FileDrop)) return null;
        if (data.GetData(DataFormats.FileDrop) is not string[] files) return null;
        return files.FirstOrDefault(f =>
            f.EndsWith(".a2l", StringComparison.OrdinalIgnoreCase));
    }

    private void SetDropTargetVisualFeedback(bool active)
        => DropTargetBorder.BorderBrush = active ? Brushes.DodgerBlue : Brushes.Transparent;

    // --- v0.7 menu command handlers -------------------------------------------
    // Thin View-layer handlers: file/validate ops delegate to the ViewModel;
    // editor ops (cut/copy/paste/zoom) target the inner AvalonEdit (TextEditor.Editor);
    // unimplemented v0.8+ features surface a MessageBox placeholder.

    private void OnNew(object sender, ExecutedRoutedEventArgs e) => ViewModel?.NewFile();

    private void OnOpen(object sender, ExecutedRoutedEventArgs e)
        => ViewModel?.OpenCommand.Execute(null);

    private void OnSave(object sender, ExecutedRoutedEventArgs e)
        => ViewModel?.SaveCommand.Execute(null);

    private void OnSave_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        => e.CanExecute = ViewModel?.IsDirty ?? false;

    private void OnSaveAs(object sender, ExecutedRoutedEventArgs e)
    {
        if (ViewModel is null) return;
        var path = ViewModel.OpenSaveAsDialog();
        if (path is not null) ViewModel.SaveAs(path);
    }

    private void OnSaveAs_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        => e.CanExecute = ViewModel?.IsFileOpen ?? false;

    private void OnExit(object sender, ExecutedRoutedEventArgs e) => Close();

    private void OnUndo(object sender, ExecutedRoutedEventArgs e)
        => MessageBox.Show("Undo not implemented in v0.7 (planned for v0.8+).", "Undo");

    private void OnRedo(object sender, ExecutedRoutedEventArgs e)
        => MessageBox.Show("Redo not implemented in v0.7 (planned for v0.8+).", "Redo");

    private void OnCut(object sender, ExecutedRoutedEventArgs e) => TextEditor.Editor.Cut();

    private void OnCopy(object sender, ExecutedRoutedEventArgs e) => TextEditor.Editor.Copy();

    private void OnPaste(object sender, ExecutedRoutedEventArgs e) => TextEditor.Editor.Paste();

    private void OnFind(object sender, ExecutedRoutedEventArgs e)
        => MessageBox.Show("Find dialog not implemented in v0.7 (planned for v0.8+).", "Find");

    private void OnZoomIn(object sender, ExecutedRoutedEventArgs e)
        => TextEditor.Editor.FontSize = Math.Min(TextEditor.Editor.FontSize + 2, 32);

    private void OnZoomOut(object sender, ExecutedRoutedEventArgs e)
        => TextEditor.Editor.FontSize = Math.Max(TextEditor.Editor.FontSize - 2, 8);

    private void OnResetZoom(object sender, ExecutedRoutedEventArgs e)
        => TextEditor.Editor.FontSize = 12;

    private void OnAbout(object sender, ExecutedRoutedEventArgs e)
        => MessageBox.Show("a2l-editor v0.7\n\nASAP2 (.a2l) GUI + CLI editor.", "About");

    private void OnValidate(object sender, ExecutedRoutedEventArgs e) => ViewModel?.Validate();

    private void OnFormat(object sender, ExecutedRoutedEventArgs e)
        => MessageBox.Show("Format not implemented in v0.7 (planned for v0.8+).", "Format");
}