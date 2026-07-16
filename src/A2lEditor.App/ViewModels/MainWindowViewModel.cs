using CommunityToolkit.Mvvm.ComponentModel;

namespace A2lEditor.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty] private string _title = "a2l-editor — (no file)";
}
