using System.Windows.Input;

namespace A2lEditor.App;

public static class AppCommands
{
    public static readonly RoutedUICommand ValidateFile =
        new("Validate File", "ValidateFile", typeof(AppCommands),
            new InputGestureCollection { new KeyGesture(Key.F5) });

    public static readonly RoutedUICommand FormatFile =
        new("Format File", "FormatFile", typeof(AppCommands),
            new InputGestureCollection { new KeyGesture(Key.F8) });

    public static readonly RoutedUICommand ResetZoom =
        new("Reset Zoom", "ResetZoom", typeof(AppCommands),
            new InputGestureCollection { new KeyGesture(Key.D0, ModifierKeys.Control) });

    public static readonly RoutedUICommand ExitFile =
        new("Exit", "ExitFile", typeof(AppCommands));
    // Note: no default KeyGesture; Alt+F4 is OS default

    public static readonly RoutedUICommand ApplyMap =
        new("Apply MAP...", "ApplyMap", typeof(AppCommands));
}