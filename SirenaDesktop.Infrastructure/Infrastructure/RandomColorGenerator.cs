using Avalonia.Media;
using System.Drawing;

namespace SirenaDesktop.Infrastructure.Infrastructure;

public class RandomColorGenerator
{
    private static readonly Random _random = new();

    public static SolidColorBrush Generate()
    {
        var rgb = new byte[3];
        _random.NextBytes(rgb);
        var brush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(rgb[0], rgb[1], rgb[2]));

        return brush;
    }
}