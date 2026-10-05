using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirenaDesktop.Core.Models;
using SirenaDesktop.Services;


namespace SirenaDesktop.ViewModels;

public partial class TimerSettingsViewModel : ObservableObject
{
    private readonly TimerSettingsService _settingsService;
    private readonly TimerSettings _settings;

    [ObservableProperty] private double volume;

    [ObservableProperty] private double fadeInSeconds;

    [ObservableProperty] private string? workSoundPath;

    [ObservableProperty] private string? breakSoundPath;

    [ObservableProperty] private bool autoStartBreak;

    [ObservableProperty] private bool autoStartWork;

    public TimerSettingsViewModel(TimerSettingsService settingsService)
    {
        _settingsService = settingsService;
        _settings = settingsService.Load();

        volume = _settings.Volume;
        fadeInSeconds = _settings.FadeInSeconds;
        workSoundPath = _settings.WorkSoundPath;
        breakSoundPath = _settings.BreakSoundPath;
        autoStartBreak = _settings.AutoStartBreak;
        autoStartWork = _settings.AutoStartWork;
    }

    public string WorkSoundLabel => string.IsNullOrWhiteSpace(WorkSoundPath)
        ? "Default sound"
        : Path.GetFileName(WorkSoundPath);

    public string BreakSoundLabel => string.IsNullOrWhiteSpace(BreakSoundPath)
        ? "Default sound"
        : Path.GetFileName(BreakSoundPath);

    public event EventHandler? SettingsChanged;

    partial void OnVolumeChanged(double value)
    {
        _settings.Volume = value;
        Persist();
    }

    partial void OnFadeInSecondsChanged(double value)
    {
        _settings.FadeInSeconds = value;
        Persist();
    }

    partial void OnAutoStartBreakChanged(bool value)
    {
        _settings.AutoStartBreak = value;
        Persist();
    }

    partial void OnAutoStartWorkChanged(bool value)
    {
        _settings.AutoStartWork = value;
        Persist();
    }

    partial void OnWorkSoundPathChanged(string? value)
    {
        OnPropertyChanged(nameof(WorkSoundLabel));
    }

    partial void OnBreakSoundPathChanged(string? value)
    {
        OnPropertyChanged(nameof(BreakSoundLabel));
    }

    [RelayCommand]
    private async Task BrowseWorkSoundAsync()
    {
        var picked = await PickSoundFileAsync();
        if (picked is null) return;

        var copied = CopySoundToStorage(picked);
        _settings.WorkSoundPath = copied;
        WorkSoundPath = copied;
        Persist();
    }

    [RelayCommand]
    private async Task BrowseBreakSoundAsync()
    {
        var picked = await PickSoundFileAsync();
        if (picked is null) return;

        var copied = CopySoundToStorage(picked);
        _settings.BreakSoundPath = copied;
        BreakSoundPath = copied;
        Persist();
    }

    [RelayCommand]
    private void ResetWorkSound()
    {
        _settings.WorkSoundPath = null;
        WorkSoundPath = null;
        Persist();
    }

    [RelayCommand]
    private void ResetBreakSound()
    {
        _settings.BreakSoundPath = null;
        BreakSoundPath = null;
        Persist();
    }

    private async Task<string?> PickSoundFileAsync()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime
            is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            return null;

        var topLevel = TopLevel.GetTopLevel(desktop.MainWindow);
        if (topLevel is null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select sound file",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Audio files") { Patterns = new[] { "*.wav", "*.mp3" } }
            }
        });

        var file = files.FirstOrDefault();
        return file?.TryGetLocalPath();
    }

    private string CopySoundToStorage(string sourcePath)
    {
        var destinationFolder = _settingsService.SoundsFolder;
        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(sourcePath)}";
        var destinationPath = Path.Combine(destinationFolder, fileName);

        File.Copy(sourcePath, destinationPath, overwrite: true);
        return destinationPath;
    }

    private void Persist()
    {
        _settingsService.Save(_settings);
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }
}
