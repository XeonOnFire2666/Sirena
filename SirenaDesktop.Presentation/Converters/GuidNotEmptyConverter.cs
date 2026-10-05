using System.Globalization;
using Avalonia.Data.Converters;

namespace SirenaDesktop.Presentation.Converters;

public class GuidNotEmptyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Guid g && g != Guid.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}