using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System.Globalization;
using System.Net.Http;

namespace SirenaDesktop.Presentation.Converters;

public class SafeImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string url || string.IsNullOrWhiteSpace(url))
            return null;    

        try
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https"))
            {
                using var httpClient = new HttpClient();
                using var stream = httpClient.GetStreamAsync(uri).GetAwaiter().GetResult();
                return new Bitmap(stream);
            }

            return new Bitmap(url);
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}