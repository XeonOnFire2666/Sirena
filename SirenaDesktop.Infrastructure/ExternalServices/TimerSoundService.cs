using LibVLCSharp.Shared;
using SirenaDesktop.Application.Contracts;

namespace SirenaDesktop.Services;

public class TimerSoundService : ITimerSoundService, IDisposable 
{
    private readonly LibVLC _libVlc;
    private MediaPlayer? _mediaPlayer;
    private Media? _media;
    private CancellationTokenSource? _fadeCts;

    public string DefaultSoundPath { get; set; } = Path.Combine("Assets", "Sounds", "timer_end.wav");

    public double Volume { get; set; } = 1.0;

    public double FadeInSeconds { get; set; } = 0;

    public bool IsPlaying => _mediaPlayer?.IsPlaying ?? false;

    public TimerSoundService()
    {
        LibVLCSharp.Shared.Core.Initialize();
        _libVlc = new LibVLC();
    }

    public void PlaySound(string? customSoundPath)
    {
        StopSound();

        try
        {
            var path = ResolveSoundPath(customSoundPath);

            if (path is null || !File.Exists(path))
            {
                PlayFallbackBeep();
                return;
            }

            _media = new Media(_libVlc, path, FromType.FromPath);
            _mediaPlayer = new MediaPlayer(_media);

            var targetVolume = (int)Math.Clamp(Volume * 100, 0, 100);

            if (FadeInSeconds > 0)
            {
                _mediaPlayer.Volume = 0;
                _mediaPlayer.Play();
                StartFadeIn(targetVolume, FadeInSeconds);
            }
            else
            {
                _mediaPlayer.Volume = targetVolume;
                _mediaPlayer.Play();
            }
        }
        catch
        {
            try
            {
                PlayFallbackBeep();
            }
            catch
            {
            }
        }
    }

    public void StopSound()
    {
        _fadeCts?.Cancel();
        _fadeCts = null;

        try
        {
            _mediaPlayer?.Stop();
        }
        catch
        {
        }

        _mediaPlayer?.Dispose();
        _mediaPlayer = null;

        _media?.Dispose();
        _media = null;
    }

    private void StartFadeIn(int targetVolume, double durationSeconds)
    {
        var cts = new CancellationTokenSource();
        _fadeCts = cts;

        var player = _mediaPlayer;
        if (player is null) return;

        _ = Task.Run(async () =>
        {
            const int steps = 30;
            var stepDelayMs = (int)(durationSeconds * 1000 / steps);

            for (var i = 1; i <= steps; i++)
            {
                if (cts.Token.IsCancellationRequested) return;

                try
                {
                    player.Volume = targetVolume * i / steps;
                }
                catch
                {
                    return;
                }

                try
                {
                    await Task.Delay(stepDelayMs, cts.Token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }
        }, cts.Token);
    }

    private string? ResolveSoundPath(string? customSoundPath)
    {
        var relativeOrAbsolute = string.IsNullOrWhiteSpace(customSoundPath)
            ? DefaultSoundPath
            : customSoundPath;

        return Path.IsPathRooted(relativeOrAbsolute)
            ? relativeOrAbsolute
            : Path.Combine(AppContext.BaseDirectory, relativeOrAbsolute);
    }

    private static void PlayFallbackBeep()
    {
        if (OperatingSystem.IsWindows())
        {
            Console.Beep();
        }
        else
        {
            Console.Write("\a");
        }
    }

    public void Dispose()
    {
        StopSound();
        _libVlc.Dispose();
        GC.SuppressFinalize(this);
    }
}