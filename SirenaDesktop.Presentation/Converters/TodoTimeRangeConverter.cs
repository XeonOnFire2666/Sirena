using System.Globalization;
using Avalonia.Data.Converters;

namespace SirenaDesktop.Presentation.Converters;

public class TodoTimeRangeConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var start = ToLocal(values.ElementAtOrDefault(0) as DateTime?);
        var end = ToLocal(values.ElementAtOrDefault(1) as DateTime?);

        return (start, end) switch
        {
            ({ } s, { } e) => $"{s:HH:mm} – {e:HH:mm}",
            ({ } s, null) => $"с {s:HH:mm}",
            (null, { } e) => $"до {e:HH:mm}",
            _ => string.Empty
        };
    }

    private static DateTime? ToLocal(DateTime? utc)
    {
        return utc.HasValue
            ? DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc).ToLocalTime()
            : null;
    }
}