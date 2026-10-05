using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;

namespace SirenaDesktop.Presentation.Converters;

public class ProductivityToBrushConverter : IValueConverter
{
    // Тёмная тема — как было
    private static readonly IBrush NoDataDark = new SolidColorBrush(Color.FromRgb(120, 120, 130));

    // Светлая тема — светлее и песочнее
    private static readonly IBrush NoDataLight = new SolidColorBrush(Color.FromRgb(217, 199, 167));

    private static readonly IBrush Low = new SolidColorBrush(Color.FromRgb(229, 72, 77));
    private static readonly IBrush Mid = new SolidColorBrush(Color.FromRgb(245, 165, 36));
    private static readonly IBrush High = new SolidColorBrush(Color.FromRgb(61, 214, 140));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return GetNoDataBrush();

        var percent = System.Convert.ToInt32(value);

        if (percent >= 71)
            return High;

        if (percent >= 41)
            return Mid;

        return Low;
    }

    private static IBrush GetNoDataBrush()
    {
        var theme = Avalonia.Application.Current?.ActualThemeVariant;

        return theme == ThemeVariant.Light
            ? NoDataLight
            : NoDataDark;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class IsZeroConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int i && i == 0;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class ProductivityToTooltipConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is int percent
            ? $"Productivity: {percent}%"
            : "No data for this day";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class WidthToColumnsConverter : IValueConverter
{
    private const double CellWidth = 30;
    private const double CellMargin = 10;
    private const int MinColumns = 5;
    private const int MaxColumns = 30;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double width || width <= 0)
            return MinColumns;

        var slot = CellWidth + CellMargin;
        var columns = (int)(width / slot);
        return Math.Clamp(columns, MinColumns, MaxColumns);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}