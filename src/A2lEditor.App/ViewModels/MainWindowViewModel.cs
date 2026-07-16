using System.Collections.ObjectModel;
using System.IO;
using A2lEditor.Core.Parsing;
using A2lEditor.Core.RecentFiles;
using A2lEditor.Core.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace A2lEditor.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IDialogService _dialog;
    private readonly RecentFilesStore _recentStore;

    [ObservableProperty] private string _filePath = "";
    [ObservableProperty] private string _title = "a2l-editor — (no file)";
    [ObservableProperty] private string _rawText = "";
    [ObservableProperty] private bool _isDirty;
    [ObservableProperty] private string _moduleSummary = "";
    [ObservableProperty] private string _statusMessage = "Ready";

    [ObservableProperty] private ObservableCollection<ParseError> _parseErrors = new();
    [ObservableProperty] private int _errorCount;
    [ObservableProperty] private ObservableCollection<RecentFileEntry> _recentFiles = new();
    [ObservableProperty] private string _recentFilesStatusMessage = "";

    public event Action<int>? NavigateToLineRequested;

    public MainWindowViewModel(IDialogService dialog) : this(dialog, new RecentFilesStore()) { }

    internal MainWindowViewModel(IDialogService dialog, RecentFilesStore recentStore)
    {
        _dialog = dialog;
        _recentStore = recentStore;
        RefreshRecentFiles();
    }

    [RelayCommand]
    private void Open()
    {
        var path = _dialog.OpenA2lFile();
        if (path is null) return;
        LoadFromPath(path);
    }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrEmpty(FilePath)) return;
        var result = Asap131Parser.ParseText(RawText);
        UpdateParseErrors(result);
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

    [RelayCommand]
    private void JumpToLine(int? line)
    {
        if (line is null or < 1) return;
        NavigateToLineRequested?.Invoke(line.Value);
    }

    [RelayCommand]
    private void OpenRecent(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        if (!File.Exists(path))
        {
            _dialog.ShowError("Cannot open recent file", $"File not found: {path}");
            try
            {
                _recentStore.Remove(path);
                RefreshRecentFiles();
            }
            catch (RecentFilesStoreException ex)
            {
                RecentFilesStatusMessage = $"Failed to update recent files: {ex.Message}";
            }
            return;
        }
        LoadFromPath(path);
    }

    [RelayCommand]
    private void ClearRecent()
    {
        try
        {
            _recentStore.Clear();
            RefreshRecentFiles();
        }
        catch (RecentFilesStoreException ex)
        {
            RecentFilesStatusMessage = $"Failed to clear recent files: {ex.Message}";
        }
    }

    private void LoadFromPath(string path)
    {
        var result = Asap131Parser.ParseFile(path);
        UpdateParseErrors(result);
        if (result.HasFatalErrors || result.Value is null)
        {
            _dialog.ShowError("Parse failed", string.Join("\n",
                result.Errors.Select(e => $"L{e.Line}: {e.Message}")));
            return;
        }
        FilePath = path;
        RawText = result.Value.RawText;
        Title = $"a2l-editor — {Path.GetFileName(path)}";
        IsDirty = false;
        var total = result.Value.Modules.Sum(m => m.Measurements.Count + m.Characteristics.Count);
        ModuleSummary = $"Modules: {result.Value.Modules.Count}, signals: {total}";
        StatusMessage = $"Parsed {result.Value.SourceLineCount} lines, {result.Errors.Count} warnings";

        try
        {
            _recentStore.Add(path);
            RefreshRecentFiles();
        }
        catch (RecentFilesStoreException ex)
        {
            RecentFilesStatusMessage = $"Failed to save recent file: {ex.Message}";
        }
    }

    private void UpdateParseErrors<T>(ParseResult<T> result)
    {
        ParseErrors.Clear();
        foreach (var e in result.Errors) ParseErrors.Add(e);
        ErrorCount = ParseErrors.Count;
    }

    private void RefreshRecentFiles()
    {
        RecentFiles.Clear();
        foreach (var e in _recentStore.Entries) RecentFiles.Add(e);
    }
}