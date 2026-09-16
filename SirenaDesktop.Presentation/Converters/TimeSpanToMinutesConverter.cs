using System.Globalization;
using System.Windows.Data;

namespace SirenaDesktop.Presentation.Converters;

public class TimeSpanToMinutesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TimeSpan ts) return $"{(int)Math.Round(ts.TotalMinutes)}m";
        return "0m";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}