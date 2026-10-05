using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Net.Http;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Data.Converters;
using Material.Icons;
using SirenaDesktop.Core.Enums;

namespace SirenaDesktop.Presentation.Converters;

public class SafeImageBitmapConverter : IValueConverter
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private static readonly ConcurrentDictionary<string, Bitmap?> Cache = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string url || string.IsNullOrWhiteSpace(url))
            return null;

        if (Cache.TryGetValue(url, out var cached))
            return cached;

        Bitmap? bitmap = null;
        try
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                {
                    using var stream = Http.GetStreamAsync(uri).GetAwaiter().GetResult();
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    ms.Position = 0;
                    bitmap = new Bitmap(ms);
                }
                else if (uri.Scheme == "avares" || uri.Scheme == "resm" || uri.Scheme == "res")
                {
                    using var stream = AssetLoader.Open(uri);
                    bitmap = new Bitmap(stream);
                }
                else if (uri.IsFile && File.Exists(uri.LocalPath))
                {
                    bitmap = new Bitmap(uri.LocalPath);
                }
            }
            else if (File.Exists(url))
            {
                bitmap = new Bitmap(url);
            }
        }
        catch
        {
            bitmap = null;
        }

        Cache[url] = bitmap;
        return bitmap;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class NullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null && !(value is string s && string.IsNullOrEmpty(s));

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class IconKindConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        switch (value)
        {
            case MaterialIconKind kind:
                return kind;

            case IconType iconType:
                return IconTypeConverter.ToMaterialIcon(iconType);

            case string kindName when !string.IsNullOrWhiteSpace(kindName):
                if (Enum.TryParse<MaterialIconKind>(kindName, true, out var parsed))
                    return parsed;

                if (Enum.TryParse<IconType>(kindName, true, out var iconTypeFromString))
                    return IconTypeConverter.ToMaterialIcon(iconTypeFromString);

                break;
        }

        return MaterialIconKind.HelpCircleOutline;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class KeyStatusTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "Key saved" : "Key Not Configured";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}