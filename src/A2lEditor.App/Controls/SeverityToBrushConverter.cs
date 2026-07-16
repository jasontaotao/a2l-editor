using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using A2lEditor.Core.Parsing;

namespace A2lEditor.App.Controls;

public sealed class SeverityToBrushConverter : IValueConverter
{
    public static readonly SeverityToBrushConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ErrorSeverity sev)
        {
            return sev switch
            {
                ErrorSeverity.Fatal => new SolidColorBrush(Colors.DarkRed),
                ErrorSeverity.Error => new SolidColorBrush(Colors.Red),
                ErrorSeverity.Warning => new SolidColorBrush(Colors.DarkOrange),
                _ => new SolidColorBrush(Colors.Gray),
            };
        }
        return new SolidColorBrush(Colors.Black);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}