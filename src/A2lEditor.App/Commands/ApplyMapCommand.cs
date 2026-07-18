using System.IO;
using System.Windows;
using System.Windows.Input;
using A2lEditor.App.ViewModels;
using A2lEditor.Core.Parsing;
using A2lEditor.Core.Serialization;
using A2lEditor.Core.Services;
using A2lEditor.Reuse;
using Microsoft.Extensions.DependencyInjection;

namespace A2lEditor.App.Commands;

public static class ApplyMapCommand
{
    public static void OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        var app = Application.Current as App;
        var dialogSvc = app?.Services.GetService(typeof(IDialogService)) as IDialogService;
        var mapSvc = app?.Services.GetService(typeof(IMapAlignmentService)) as IMapAlignmentService;
        var viewModel = app?.Services.GetService(typeof(MainWindowViewModel)) as MainWindowViewModel;

        if (dialogSvc is null || mapSvc is null || viewModel is null || string.IsNullOrEmpty(viewModel.RawText))
        {
            dialogSvc?.ShowError("Apply MAP", "Apply MAP requires an open document.");
            return;
        }

        // Parse the current document from editor text.
        var parseResult = Asap131Parser.ParseText(viewModel.RawText);
        if (parseResult.HasFatalErrors || parseResult.Value is null)
        {
            dialogSvc.ShowError("Apply MAP", "Cannot apply MAP: current document has parse errors.");
            return;
        }
        var doc = parseResult.Value;

        // Inline OpenFileDialog because IDialogService.OpenA2lFile() filters *.a2l only;
        // Apply MAP needs to open .map / .elf files.
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "MAP or ELF files|*.map;*.elf|All files|*.*",
            Title = "Select MAP or ELF file"
        };
        if (dlg.ShowDialog() != true) return;
        var mapPath = dlg.FileName;

        try
        {
            var symbols = mapSvc.LoadMapSymbols(mapPath);
            var report = mapSvc.ValidateCoverage(symbols, doc);

            var message = $"Matched: {report.MatchedInA2l}/{report.TotalMapSymbols}\n" +
                          $"Missing from .a2l: {report.MissingFromA2l}\n" +
                          $"Extra in .a2l: {report.ExtraInA2l.Count}\n\n" +
                          "Apply these updates?";

            var confirm = MessageBox.Show(message, "Apply MAP", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            var result = mapSvc.ApplyAddresses(doc, symbols, new MapApplyOptions(false, true, null));
            if (result.NewDocument is null) return;

            // Serialize back to text: clear RawText so WriteToString generates from model
            // (preserving address updates that were applied to the model).
            var writer = new A2lDocumentWriter();
            using var sw = new StringWriter();
            writer.WriteToString(result.NewDocument with { RawText = "" }, sw);
            viewModel.RawText = sw.ToString();
        }
        catch (FileNotFoundException ex)
        {
            dialogSvc.ShowError("Apply MAP", $"MAP file not found: {ex.FileName}");
        }
        catch (InvalidMapException ex)
        {
            dialogSvc.ShowError("Apply MAP", ex.Message);
        }
    }
}
