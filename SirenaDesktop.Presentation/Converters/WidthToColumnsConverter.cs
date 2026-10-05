using System.Globalization;
using Avalonia.Data.Converters;

namespace SirenaDesktop.Presentation.Converters;

//public class WidthToColumnsConverter : IValueConverter
//{
//    private const double CellTotalWidth = 40;      
//    private const double ContainerPadding = 48;     

//    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
//    {
//        if (value is not double width || width <= 0)
//            return 7;

//        var usableWidth = width - ContainerPadding;
//        if (usableWidth <= 0)
//            return 1;

//        var columns = (int)Math.Floor(usableWidth / CellTotalWidth);
//        return columns < 1 ? 1 : columns;
//    }

//    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
//    {
//        throw new NotSupportedException();
//    }
//}