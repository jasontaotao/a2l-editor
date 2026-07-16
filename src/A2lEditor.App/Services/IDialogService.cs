using System.Windows;

namespace A2lEditor.App;

public interface IDialogService
{
    string? OpenA2lFile();
    string? SaveA2lFile(string defaultName);
    void ShowError(string title, string message);
}

public sealed class WpfDialogService : IDialogService
{
    public string? OpenA2lFile()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "ASAP2 files|*.a2l|All files|*.*"
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? SaveA2lFile(string defaultName)
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            FileName = defaultName,
            Filter = "ASAP2 files|*.a2l"
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public void ShowError(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
}
