namespace SirenaDesktop.Infrastructure.Infrastructure;

public static class WorkspaceColorPalette
{
    public static readonly string[] Colors =
    [
        "#8AB4F8", // soft blue
        "#81C995", // mint green
        "#FDD663", // warm yellow
        "#F28B82", // coral
        "#C58AF9", // lilac
        "#78D9EC", // turquoise
        "#FCAD70", // peach
        "#F5A6C9" // pink
    ];

    private static readonly Random _random = new();

    public static string PickRandom()
    {
        return Colors[_random.Next(Colors.Length)];
    }
}