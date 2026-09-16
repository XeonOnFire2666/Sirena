using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SirenaDesktop.Presentation.Converters;

public class ProductivityToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var percent = value != null ? System.Convert.ToInt32(value) : 0;

        if (percent >= 71)
            return new SolidColorBrush(Color.FromRgb(138, 61, 7));

        if (percent >= 41)
            return new SolidColorBrush(Color.FromRgb(125, 69, 30));

        if (percent >= 21)
            return new SolidColorBrush(Color.FromRgb(69, 50, 37));

        return new SolidColorBrush(Color.FromRgb(51, 45, 41));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}