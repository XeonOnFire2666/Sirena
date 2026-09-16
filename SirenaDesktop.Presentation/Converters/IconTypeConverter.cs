using System.Globalization;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;
using SirenaDesktop.Core.Enums;

namespace SirenaDesktop.Presentation.Converters;

public class IconTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IconType iconType) return ToPackIcon(iconType);
        return PackIconKind.None;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    public static PackIconKind ToPackIcon(IconType iconType)
    {
        return iconType switch
        {
            IconType.HomeOutline => PackIconKind.HomeOutline,
            IconType.TimerOutline => PackIconKind.TimerOutline,
            IconType.Checklist => PackIconKind.CalendarCheckOutline,
            IconType.ChartBar => PackIconKind.ChartBar,
            IconType.CodeBraces => PackIconKind.CodeBraces,
            IconType.SchoolOutline => PackIconKind.SchoolOutline,
            IconType.BriefcaseOutline => PackIconKind.BriefcaseOutline,
            IconType.PaletteOutline => PackIconKind.PaletteOutline,
            IconType.Wheelchair => PackIconKind.Wheelchair,
            IconType.Google => PackIconKind.Google,
            IconType.AccountCircle => PackIconKind.AccountCircle,
            IconType.Cog => PackIconKind.Cog,
            IconType.Close => PackIconKind.Close,
            IconType.Fire => PackIconKind.Fire,
            IconType.Monitor => PackIconKind.Monitor,
            IconType.Smartphone => PackIconKind.Smartphone,
            IconType.Calendar => PackIconKind.Calendar,
            IconType.Filter => PackIconKind.Filter,
            IconType.Plus => PackIconKind.Plus,
            IconType.Edit => PackIconKind.Edit,
            IconType.Delete => PackIconKind.Delete,
            IconType.Check => PackIconKind.Check,
            IconType.Clock => PackIconKind.Clock,
            IconType.ArrowLeft => PackIconKind.ArrowLeft,
            IconType.ArrowRight => PackIconKind.ArrowRight,
            IconType.Settings => PackIconKind.Settings,
            IconType.Logout => PackIconKind.Logout,
            IconType.Git => PackIconKind.Git,
            IconType.CheckCircle => PackIconKind.CheckCircle,
            IconType.AlertCircle => PackIconKind.AlertCircle,
            _ => PackIconKind.None
        };
    }
}