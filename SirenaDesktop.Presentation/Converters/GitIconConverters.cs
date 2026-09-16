using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SirenaDesktop.Presentation.Converters;

public class BoolToGitIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? "CheckCircle" : "SourceBranch";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class BoolToGitBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true
            ? new SolidColorBrush(Color.FromRgb(0x43, 0xA0, 0x47))    
            : new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}