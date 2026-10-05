using Material.Icons;
using SirenaDesktop.Core.Enums;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SirenaDesktop.Presentation.Converters;

public class IconTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IconType iconType) return ToMaterialIcon(iconType);
        return MaterialIconKind.HelpCircleOutline;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    public static MaterialIconKind ToMaterialIcon(IconType iconType)
    {
        return iconType switch
        {
            IconType.HomeOutline => MaterialIconKind.HomeVariantOutline,
            IconType.TimerOutline => MaterialIconKind.TimerOutline,
            IconType.Checklist => MaterialIconKind.ClipboardCheckOutline,
            IconType.ChartBar => MaterialIconKind.ChartBoxOutline,
            IconType.CodeBraces => MaterialIconKind.CodeBraces,
            IconType.SchoolOutline => MaterialIconKind.SchoolOutline,
            IconType.BriefcaseOutline => MaterialIconKind.BriefcaseOutline,
            IconType.PaletteOutline => MaterialIconKind.PaletteOutline,
            IconType.Wheelchair => MaterialIconKind.Wheelchair,
            IconType.Google => MaterialIconKind.Google,
            IconType.AccountCircle => MaterialIconKind.AccountCircle,
            IconType.Cog => MaterialIconKind.Cog,
            IconType.Close => MaterialIconKind.Close,
            IconType.Fire => MaterialIconKind.Fire,
            IconType.Monitor => MaterialIconKind.Monitor,
            IconType.Smartphone => MaterialIconKind.CellphoneLink,
            IconType.Calendar => MaterialIconKind.Calendar,
            IconType.Filter => MaterialIconKind.Filter,
            IconType.Plus => MaterialIconKind.Plus,
            IconType.Edit => MaterialIconKind.Pencil,
            IconType.Delete => MaterialIconKind.Delete,
            IconType.Check => MaterialIconKind.Check,
            IconType.Clock => MaterialIconKind.ClockOutline,
            IconType.ArrowLeft => MaterialIconKind.ArrowLeft,
            IconType.ArrowRight => MaterialIconKind.ArrowRight,
            IconType.Settings => MaterialIconKind.Cog,
            IconType.Logout => MaterialIconKind.Logout,
            IconType.Git => MaterialIconKind.Git,
            IconType.CheckCircle => MaterialIconKind.CheckCircle,
            IconType.AlertCircle => MaterialIconKind.AlertCircleOutline,
            _ => MaterialIconKind.ABCOff
        };
    }
}