using System.Globalization;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;

namespace SirenaDesktop.Presentation.Converters;

public class BoolToKeyStatusIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hasKey = value is true;
        return hasKey ? PackIconKind.CheckCircle : PackIconKind.AlertCircleOutline;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}