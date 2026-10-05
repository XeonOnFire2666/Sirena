using System;
using System.Collections.Generic;
using System.Text;

namespace SirenaDesktop.Application.Contracts;
public interface ITimerSoundService : IDisposable
{
    string DefaultSoundPath { get; set; }
    double Volume { get; set; }
    double FadeInSeconds { get; set; }
    bool IsPlaying { get; }

    void PlaySound(string? customSoundPath);
    void StopSound();
}