using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SirenaDesktop.Presentation.Converters;
public class OfflineToColorConverter : IValueConverter
{
    public static readonly OfflineToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isOffline)
        {
            return isOffline
                ? new SolidColorBrush(Color.Parse("#B8860B"))
                : new SolidColorBrush(Color.Parse("#2E7D32"));
        }

        return new SolidColorBrush(Colors.Transparent);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class OfflineToTextConverter : IValueConverter
{
    public static readonly OfflineToTextConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isOffline)
        {
            return isOffline ? "Офлайн режим" : "Соединение восстановлено";
        }

        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}