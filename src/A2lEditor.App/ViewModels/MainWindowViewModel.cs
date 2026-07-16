using System.IO;
using A2lEditor.Core.Parsing;
using A2lEditor.Core.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace A2lEditor.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IDialogService _dialog;

    [ObservableProperty] private string _filePath = "";
    [ObservableProperty] private string _title = "a2l-editor — (no file)";
    [ObservableProperty] private string _rawText = "";
    [ObservableProperty] private bool _isDirty;
    [ObservableProperty] private string _moduleSummary = "";
    [ObservableProperty] private string _statusMessage = "Ready";

    public MainWindowViewModel(IDialogService dialog) => _dialog = dialog;

    [RelayCommand]
    private void Open()
    {
        var path = _dialog.OpenA2lFile();
        if (path is null) return;
        var result = Asap131Parser.ParseFile(path);
        if (result.HasFatalErrors)
        {
            _dialog.ShowError("Parse failed", string.Join("\n",
                result.Errors.Select(e => $"L{e.Line}: {e.Message}")));
            return;
        }
        if (result.Value is null)
        {
            _dialog.ShowError("Parse failed", "Document is empty or invalid.");
            return;
        }
        FilePath = path;
        RawText = result.Value.RawText;
        Title = $"a2l-editor — {Path.GetFileName(path)}";
        IsDirty = false;
        var total = result.Value.Modules.Sum(m => m.Measurements.Count + m.Characteristics.Count);
        ModuleSummary = $"Modules: {result.Value.Modules.Count}, signals: {total}";
        StatusMessage = $"Parsed {result.Value.SourceLineCount} lines, {result.Errors.Count} warnings";
    }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrEmpty(FilePath)) return;
        var result = Asap131Parser.ParseText(RawText);
        if (result.HasFatalErrors || result.Value is null)
        {
            _dialog.ShowError("Cannot save",
                "Parse failed:\n" + string.Join("\n", result.Errors.Select(e => e.Message)));
            return;
        }
        new A2lDocumentWriter().WriteToFile(result.Value, FilePath);
        IsDirty = false;
        StatusMessage = $"Saved at {DateTime.Now:HH:mm:ss}";
    }
}