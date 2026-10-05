using System.Text.Json.Serialization;

namespace SirenaDesktop.Core.Models;

public class TimerSettings
{
    public int WorkSeconds { get; set; } = 25 * 60;

    public int BreakSeconds { get; set; } = 5 * 60;

    public double Volume { get; set; } = 1.0;

    public double FadeInSeconds { get; set; } = 0;

    public string? WorkSoundPath { get; set; }

    public string? BreakSoundPath { get; set; }

    public bool AutoStartBreak { get; set; } = false;

    public bool AutoStartWork { get; set; } = false;
}
