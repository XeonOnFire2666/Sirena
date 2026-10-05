using Avalonia.Data.Converters;
using Avalonia.Media;
using SirenaDesktop.Core.Enums;
using SirenaDesktop.Core.Models;
using System;
using System.Globalization;

namespace SirenaDesktop.Views;

public class TimerKindToTintConverter : IMultiValueConverter
{
    private static readonly SolidColorBrush WorkTintBrush = new(Color.Parse("#34E10A00"));
    private static readonly SolidColorBrush BreakTintBrush = new(Color.Parse("#3434C759"));

    private static readonly SolidColorBrush NoTint = new(Colors.Transparent);

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var isRunning = values.ElementAtOrDefault(0) as bool? ?? false;
        var kind = values.ElementAtOrDefault(1) as TimerSessionKind? ?? TimerSessionKind.Work;

        if (!isRunning)
            return Brushes.Transparent;

        return kind == TimerSessionKind.Work ? WorkTintBrush : BreakTintBrush;
    }
}
