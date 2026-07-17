using System.Collections.ObjectModel;
using System.IO;
using A2lEditor.Core.Parsing;
using A2lEditor.Core.RecentFiles;
using A2lEditor.Core.Serialization;
using A2lEditor.Core.Validation;
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

    /// <summary>True when a file path is set (used to gate Save As / file-scoped commands).</summary>
    public bool IsFileOpen => !string.IsNullOrEmpty(FilePath);

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
        if (string.IsNullOrEmpty(RawText)) return;
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

    /// <summary>Reset to an empty (no-file) state. v0.7 stub: clears the file path only.</summary>
    public void NewFile()
    {
        FilePath = "";
        // Future: clear editor + reset document state. v0.7 stub.
    }

    /// <summary>Save the current text to <paramref name="path"/> and adopt it as the file path.</summary>
    public void SaveAs(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        var result = Asap131Parser.ParseText(RawText);
        UpdateParseErrors(result);
        if (result.HasFatalErrors || result.Value is null)
        {
            _dialog.ShowError("Cannot save",
                "Parse failed:\n" + string.Join("\n", result.Errors.Select(e => e.Message)));
            return;
        }
        new A2lDocumentWriter().WriteToFile(result.Value, path);
        FilePath = path;
        IsDirty = false;
        StatusMessage = $"Saved as {Path.GetFileName(path)} at {DateTime.Now:HH:mm:ss}";
    }

    /// <summary>Re-run semantic validation over the current text and surface findings in the error list.</summary>
    public void Validate()
    {
        var result = Asap131Parser.ParseText(RawText);
        if (result.HasFatalErrors || result.Value is null)
        {
            UpdateParseErrors(result);
            return;
        }
        var errors = new A2lValidator().Validate(result.Value);
        SetParseErrors(errors);
        StatusMessage = $"Validated: {errors.Count} issue(s)";
    }

    /// <summary>Prompt for a Save-As destination via the dialog service. Returns null if cancelled.</summary>
    public string? OpenSaveAsDialog()
    {
        var defaultName = string.IsNullOrEmpty(FilePath)
            ? "untitled.a2l"
            : Path.GetFileName(FilePath);
        return _dialog.SaveA2lFile(defaultName);
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

    private void SetParseErrors(IReadOnlyList<ParseError> errors)
    {
        ParseErrors.Clear();
        foreach (var e in errors) ParseErrors.Add(e);
        ErrorCount = ParseErrors.Count;
    }

    private void RefreshRecentFiles()
    {
        RecentFiles.Clear();
        foreach (var e in _recentStore.Entries) RecentFiles.Add(e);
    }
}