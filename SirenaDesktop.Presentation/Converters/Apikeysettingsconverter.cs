using System.Globalization;
using Avalonia.Data.Converters;
using Material.Icons;

namespace SirenaDesktop.Presentation.Converters;

public class BoolToKeyStatusIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hasKey = value is true;
        return hasKey ? MaterialIconKind.CheckCircle : MaterialIconKind.AlertCircleOutline;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}