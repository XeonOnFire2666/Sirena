using System.Media;

namespace SirenaDesktop.Services;

/// <summary>
///     Проигрывает звуковой сигнал при завершении таймера.
///     Путь к файлу настраиваемый — положи свой .wav в папку Assets/Sounds
///     (или укажи другой путь через SoundFilePath) в корне проекта, рядом с exe.
///     ВАЖНО: System.Media.SoundPlayer поддерживает только .wav.
///     Если нужен .mp3 — предусмотрен fallback на системный звук.
/// </summary>
public class TimerSoundService
{
    /// <summary>
    ///     Относительный (от папки с exe) или абсолютный путь к звуковому файлу.
    ///     Измени на свой, когда положишь файл в проект, например:
    ///     "Assets/Sounds/timer_end.wav"
    /// </summary>
    public string SoundFilePath { get; set; } = Path.Combine("Assets", "Sounds", "timer_end.wav");

    public void PlayTimerEndSound()
    {
        try
        {
            var fullPath = Path.IsPathRooted(SoundFilePath)
                ? SoundFilePath
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SoundFilePath);

            if (File.Exists(fullPath))
            {
                using var player = new SoundPlayer(fullPath);
                player.Play(); // асинхронно, не блокирует UI-поток
            }
            else
            {
                // Файл ещё не положен в проект — играем системный звук как заглушку,
                // чтобы функциональность не терялась молча.
                SystemSounds.Exclamation.Play();
            }
        }
        catch
        {
            // Звук не должен ронять таймер, даже если файл повреждён/занят.
            try
            {
                SystemSounds.Exclamation.Play();
            }
            catch
            {
                /* ignore */
            }
        }
    }
}