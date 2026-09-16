using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirenaDesktop.Application.Contracts.Ai;
using SirenaDesktop.Core.Interfaces;
using SirenaDesktop.Infrastructure.AI;
using SirenaDesktop.Services;
using System.Collections.ObjectModel;

namespace SirenaDesktop.ViewModels;

public partial class MainMenuViewModel : ObservableObject, IDisposable
{
    [ObservableProperty] private int _celebratedStreak;

    private bool _disposed;

    [ObservableProperty] private bool _isProfileDialogOpen;

    [ObservableProperty] private bool _isSettingsDialogOpen;

    [ObservableProperty] private bool _isStreakCelebrationVisible;

    [ObservableProperty] private ITabViewModel _selectedTab;

    public MainMenuViewModel(
        HomeTabViewModel homeTab,
        ActivityTabViewModel activityTab,
        TodoTabViewModel todoTab,
        TimerTabViewModel timerTab,
        GitSettingsViewModel gitSettingsViewModel,
        IGeminiAiProvider geminiCaller,
        IApiKeyStore apiKeyStore)  
    {
        Tabs = [homeTab, activityTab, todoTab, timerTab];
        _selectedTab = Tabs[0];

        GitSettings = gitSettingsViewModel;
        ApiKeySettings = new ApiKeySettingsViewModel(apiKeyStore, geminiCaller);

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

    public ApiKeySettingsViewModel ApiKeySettings { get; }

    public string? UserName => SelectedTabAsHome()?.UserName;
    public string? UserPictureUrl => SelectedTabAsHome()?.UserPictureUrl;

    public void Dispose()
    {
        if (_disposed) return;

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

    private void OnStreakExtended(object? sender, int newStreak)
    {
        CelebratedStreak = newStreak;
        IsStreakCelebrationVisible = true;
    }

    [RelayCommand]
    private void CloseStreakCelebration()
    {
        IsStreakCelebrationVisible = false;
    }

    [RelayCommand]
    private void OpenSettings()
    {
        ThrowIfDisposed();
        IsSettingsDialogOpen = true;
    }

    [RelayCommand]
    private void CloseSettings()
    {
        IsSettingsDialogOpen = false;
    }

    [RelayCommand]
    private void OpenProfile()
    {
        ThrowIfDisposed();
        IsProfileDialogOpen = true;
    }

    [RelayCommand]
    private void CloseProfile()
    {
        IsProfileDialogOpen = false;
    }

    [RelayCommand]
    private void Logout()
    {
        ThrowIfDisposed();
        IsSettingsDialogOpen = false;
        IsProfileDialogOpen = false;
        LogoutRequested?.Invoke(this, EventArgs.Empty);
    }

    private HomeTabViewModel? SelectedTabAsHome()
    {
        return Tabs.OfType<HomeTabViewModel>().FirstOrDefault();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MainMenuViewModel));
    }
}