using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirenaDesktop.Application.Contracts.Ai;
using SirenaDesktop.Core.Interfaces;
using SirenaDesktop.Infrastructure.AI;
using SirenaDesktop.Services;
using SukiUI;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SirenaDesktop.ViewModels;

public partial class MainMenuViewModel : ObservableObject, IDisposable
{
    [ObservableProperty] private int _celebratedStreak;

    private bool _disposed;

    [ObservableProperty] private bool _isProfileDialogOpen;

    [ObservableProperty] private bool _isSettingsDialogOpen;

    [ObservableProperty] private bool _isStreakCelebrationVisible;

    [ObservableProperty] private ITabViewModel _selectedTab;

    [ObservableProperty] private bool _isDarkTheme;

    [ObservableProperty] private bool _isTimerSettingsSectionExpanded;

    public MainMenuViewModel(
        HomeTabViewModel homeTab,
        ActivityTabViewModel activityTab,
        TodoTabViewModel todoTab,
        TimerTabViewModel timerTab,
        GitSettingsViewModel gitSettingsViewModel,
        TimerSettingsViewModel timerSettingsViewModel,
        IGeminiAiProvider geminiCaller,
        IApiKeyStore apiKeyStore)
    {
        Tabs = [homeTab, activityTab, todoTab, timerTab];
        _selectedTab = Tabs[0];

        GitSettings = gitSettingsViewModel;
        TimerSettings = timerSettingsViewModel;
        ApiKeySettings = new ApiKeySettingsViewModel(apiKeyStore, geminiCaller);

        TimerSettings.SettingsChanged += (_, _) => timerTab.ReloadSettings();
        timerTab.SettingsRequested += (_, _) => OpenTimerSettingsCommand.Execute(null);

        _isDarkTheme = Avalonia.Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
        SukiTheme.GetInstance().OnBaseThemeChanged += OnBaseThemeChanged;

        homeTab.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(HomeTabViewModel.UserName))
                OnPropertyChanged(nameof(UserName));
            if (e.PropertyName is nameof(HomeTabViewModel.UserPictureUrl))
                OnPropertyChanged(nameof(UserPictureUrl));
        };

        activityTab.StreakExtended += OnStreakExtended;
    }

    public ObservableCollection<ITabViewModel> Tabs { get; }
    public GitSettingsViewModel GitSettings { get; }
    public TimerSettingsViewModel TimerSettings { get; }
    public ApiKeySettingsViewModel ApiKeySettings { get; }

    public string? UserName => SelectedTabAsHome()?.UserName;
    public string? UserPictureUrl => SelectedTabAsHome()?.UserPictureUrl;

    public string ThemeLabel => IsDarkTheme ? "Enabled" : "Disabled";

    public void Dispose()
    {
        if (_disposed) return;

        SukiTheme.GetInstance().OnBaseThemeChanged -= OnBaseThemeChanged;

        if (Tabs.OfType<ActivityTabViewModel>().FirstOrDefault() is { } activityTab)
            activityTab.StreakExtended -= OnStreakExtended;

        foreach (var tab in Tabs)
            if (tab is IDisposable disposable)
                disposable.Dispose();

        ApiKeySettings.Dispose();

        LogoutRequested = null;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    public event EventHandler? LogoutRequested;

    partial void OnIsDarkThemeChanged(bool value)
    {
        OnPropertyChanged(nameof(ThemeLabel));
    }

    private void OnBaseThemeChanged(ThemeVariant variant)
    {
        Dispatcher.UIThread.Post(() => IsDarkTheme = variant == ThemeVariant.Dark);
    }

    private void OnStreakExtended(object? sender, int newStreak)
    {
        CelebratedStreak = newStreak;
        IsStreakCelebrationVisible = true;
    }

    [RelayCommand]
    private void CloseStreakCelebration() => IsStreakCelebrationVisible = false;

    [RelayCommand]
    private void OpenSettings()
    {
        ThrowIfDisposed();
        IsSettingsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenTimerSettings()
    {
        ThrowIfDisposed();
        IsSettingsDialogOpen = true;
        IsTimerSettingsSectionExpanded = true;
    }

    [RelayCommand]
    private void CloseSettings() => IsSettingsDialogOpen = false;

    [RelayCommand]
    private void OpenProfile()
    {
        ThrowIfDisposed();
        IsProfileDialogOpen = true;
    }

    [RelayCommand]
    private void CloseProfile() => IsProfileDialogOpen = false;

    [RelayCommand]
    private void SelectTab(ITabViewModel? tab)
    {
        Debug.WriteLine($"[Diag] SelectTab -> {tab?.Title}, current={SelectedTab?.Title}");

        if (tab is null) return;
        ThrowIfDisposed();
        SelectedTab = tab;
    }

    [RelayCommand]
    private void Logout()
    {
        ThrowIfDisposed();
        IsSettingsDialogOpen = false;
        IsProfileDialogOpen = false;
        LogoutRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        ThrowIfDisposed();
        var target = IsDarkTheme ? ThemeVariant.Light : ThemeVariant.Dark;
        SukiTheme.GetInstance().ChangeBaseTheme(target);
        if (Avalonia.Application.Current is { } app)
            app.RequestedThemeVariant = target;
    }

    private HomeTabViewModel? SelectedTabAsHome()
        => Tabs.OfType<HomeTabViewModel>().FirstOrDefault();

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MainMenuViewModel));
    }
}
