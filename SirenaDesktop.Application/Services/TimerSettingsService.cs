using System;
using System.IO;
using System.Text.Json;
using SirenaDesktop.Core.Models;

namespace SirenaDesktop.Services;

public class TimerSettingsService
{
    private readonly string _settingsFilePath;

    public TimerSettingsService()
    {
        var appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SirenaDesktop");

        Directory.CreateDirectory(appDataFolder);

        _settingsFilePath = Path.Combine(appDataFolder, "timer_settings.json");
    }

    public string SoundsFolder
    {
        get
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SirenaDesktop",
                "Sounds");

            Directory.CreateDirectory(folder);
            return folder;
        }
    }

    public TimerSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
                return new TimerSettings();

            var json = File.ReadAllText(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<TimerSettings>(json);
            return settings ?? new TimerSettings();
        }
        catch
        {
            return new TimerSettings();
        }
    }

    public void Save(TimerSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);
        }
        catch
        {
        }
    }
}
