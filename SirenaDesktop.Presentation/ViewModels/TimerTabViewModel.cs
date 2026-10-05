using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirenaDesktop.Application.Contracts;
using SirenaDesktop.Core.Enums;
using SirenaDesktop.Core.Models;
using SirenaDesktop.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace SirenaDesktop.ViewModels;

public enum TimerRunState
{
    Idle,
    Running,
    Paused
}

public partial class TimerTabViewModel : TabViewModelBase
{
    private const int MinDurationSeconds = 30;
    private readonly TimerHistoryService _historyService;
    private readonly ITimerSoundService _soundService;
    private readonly TimerSettingsService _settingsService;
    private readonly TimerSettings _settings;
    private readonly DispatcherTimer _tickTimer;
    private TimerSession? _currentSession;


    private DateTime? _pausedAt;

    [ObservableProperty] private TimerSessionKind currentKind;

    [ObservableProperty] private bool isHistoryCollapsed;

    [ObservableProperty] private int plannedSeconds;

    [ObservableProperty] private double progress;

    [ObservableProperty] private int remainingSeconds;

    [ObservableProperty] private string remainingText = "25:00";

    [ObservableProperty] private TimerRunState runState = TimerRunState.Idle;

    [ObservableProperty] private bool isAlarmPlaying;

    public TimerTabViewModel(TimerSettingsService settingsService, TimerHistoryService historyService, ITimerSoundService soundService)
        : base("Timer", IconType.TimerOutline)
    {
        _historyService = historyService;
        _soundService = soundService;
        _settingsService = settingsService;
        _settings = settingsService.Load();

        ApplySoundSettings();

        _tickTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _tickTimer.Tick += OnTick;

        PlannedSeconds = _settings.WorkSeconds;
        RemainingSeconds = PlannedSeconds;
        CurrentKind = TimerSessionKind.Work;

        IsHistoryCollapsed = true;

        LoadHistory();
        UpdateRemainingText();
    }

    public bool HasHistory => HistoryGroups.Count > 0;
    public ObservableCollection<TimerHistoryGroup> HistoryGroups { get; } = new();
    public double HistoryPanelWidth => IsHistoryCollapsed ? 40 : 320;

    public bool IsIdle => RunState == TimerRunState.Idle;
    public bool IsRunning => RunState == TimerRunState.Running;
    public bool IsPaused => RunState == TimerRunState.Paused;
    public bool IsTicking => RunState != TimerRunState.Idle;

    public string CurrentKindLabel => CurrentKind == TimerSessionKind.Work ? "Work session" : "Break session";

    public string StatusSubtitle => CurrentKind == TimerSessionKind.Work ? "Work session" : "Break session";

    public void ApplySoundSettings()
    {
        _soundService.Volume = _settings.Volume;
        _soundService.FadeInSeconds = _settings.FadeInSeconds;
    }

    public void ReloadSettings()
    {
        var fresh = _settingsService.Load();
        _settings.Volume = fresh.Volume;
        _settings.FadeInSeconds = fresh.FadeInSeconds;
        _settings.WorkSoundPath = fresh.WorkSoundPath;
        _settings.BreakSoundPath = fresh.BreakSoundPath;
        _settings.AutoStartBreak = fresh.AutoStartBreak;
        _settings.AutoStartWork = fresh.AutoStartWork;
        ApplySoundSettings();
    }

    partial void OnRunStateChanged(TimerRunState value)
    {
        OnPropertyChanged(nameof(IsIdle));
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(IsTicking));
        StartCommand.NotifyCanExecuteChanged();
        PauseCommand.NotifyCanExecuteChanged();
        ResetCommand.NotifyCanExecuteChanged();
        AddThirtySecCommand.NotifyCanExecuteChanged();
        AddOneMinCommand.NotifyCanExecuteChanged();
        AddFiveMinCommand.NotifyCanExecuteChanged();
        SubtractThirtySecCommand.NotifyCanExecuteChanged();
        SubtractOneMinCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsHistoryCollapsedChanged(bool value)
    {
        OnPropertyChanged(nameof(HistoryPanelWidth));
    }

    partial void OnCurrentKindChanged(TimerSessionKind value)
    {
        OnPropertyChanged(nameof(CurrentKindLabel));
        OnPropertyChanged(nameof(StatusSubtitle));
    }

    partial void OnPlannedSecondsChanged(int value)
    {
        if (CurrentKind == TimerSessionKind.Work)
            _settings.WorkSeconds = value;
        else
            _settings.BreakSeconds = value;

        _settingsService.Save(_settings);
    }

    private bool CanAdjustDuration()
    {
        return RunState == TimerRunState.Idle;
    }

    [RelayCommand(CanExecute = nameof(CanAdjustDuration))]
    private void AddThirtySec()
    {
        AdjustPlannedDuration(30);
    }

    [RelayCommand(CanExecute = nameof(CanAdjustDuration))]
    private void AddOneMin()
    {
        AdjustPlannedDuration(60);
    }

    [RelayCommand(CanExecute = nameof(CanAdjustDuration))]
    private void AddFiveMin()
    {
        AdjustPlannedDuration(300);
    }

    [RelayCommand(CanExecute = nameof(CanAdjustDuration))]
    private void SubtractThirtySec()
    {
        AdjustPlannedDuration(-30);
    }

    [RelayCommand(CanExecute = nameof(CanAdjustDuration))]
    private void SubtractOneMin()
    {
        AdjustPlannedDuration(-60);
    }

    [RelayCommand]
    private void ToggleHistoryPanel()
    {
        IsHistoryCollapsed = !IsHistoryCollapsed;
    }

    [RelayCommand]
    private void StopAlarm()
    {
        _soundService.StopSound();
        IsAlarmPlaying = false;
    }

    [RelayCommand]
    private void OpenSettings()
    {
        SettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? SettingsRequested;

    private void AdjustPlannedDuration(int deltaSeconds)
    {
        var newValue = PlannedSeconds + deltaSeconds;
        PlannedSeconds = Math.Max(MinDurationSeconds, newValue);
        RemainingSeconds = PlannedSeconds;
        UpdateRemainingText();
        UpdateProgress();
    }

    private void LoadHistory()
    {
        var allSessions = _historyService.LoadAll() ?? new List<TimerSession>();

        var grouped = allSessions
            .GroupBy(s => s.StartedAt.Date)
            .OrderByDescending(g => g.Key)
            .Select(g => new TimerHistoryGroup
            {
                DateLabel = FormatDateLabel(g.Key),
                Sessions = g.OrderByDescending(s => s.StartedAt).ToList()
            })
            .ToList();

        HistoryGroups.Clear();
        foreach (var group in grouped) HistoryGroups.Add(group);

        OnPropertyChanged(nameof(HasHistory));
    }

    private string FormatDateLabel(DateTime date)
    {
        if (date == DateTime.Today) return "Today";
        if (date == DateTime.Today.AddDays(-1)) return "Yesterday";

        return date.ToString("d MMMM", CultureInfo.GetCultureInfo("en-US"));
    }

    private bool CanStart()
    {
        return RunState != TimerRunState.Running;
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Start()
    {
        StopAlarmIfPlaying();

        if (RunState == TimerRunState.Idle)
        {
            _currentSession = new TimerSession
            {
                Kind = CurrentKind,
                PlannedDuration = TimeSpan.FromSeconds(PlannedSeconds),
                StartedAt = DateTime.Now,
                Result = TimerSessionResult.InProgress
            };
            _historyService.Upsert(_currentSession);
            LoadHistory();
        }
        else if (RunState == TimerRunState.Paused && _currentSession is not null && _pausedAt is not null)
        {
            var lastPause = _currentSession.Pauses.LastOrDefault();
            if (lastPause is not null && lastPause.ResumedAt is null)
                lastPause.ResumedAt = DateTime.Now;

            _pausedAt = null;
            _historyService.Upsert(_currentSession);
            LoadHistory();
        }

        RunState = TimerRunState.Running;
        _tickTimer.Start();
    }

    private bool CanPause()
    {
        return RunState == TimerRunState.Running;
    }

    [RelayCommand(CanExecute = nameof(CanPause))]
    private void Pause()
    {
        _tickTimer.Stop();
        RunState = TimerRunState.Paused;
        _pausedAt = DateTime.Now;

        _currentSession?.Pauses.Add(new TimerPauseInterval { PausedAt = _pausedAt.Value });
        if (_currentSession is not null)
        {
            _historyService.Upsert(_currentSession);
            LoadHistory();
        }
    }

    private bool CanReset()
    {
        return RunState != TimerRunState.Idle;
    }

    [RelayCommand(CanExecute = nameof(CanReset))]
    private void Reset()
    {
        _tickTimer.Stop();
        StopAlarmIfPlaying();

        if (_currentSession is not null)
        {
            var lastPause = _currentSession.Pauses.LastOrDefault();
            if (lastPause is not null && lastPause.ResumedAt is null)
                lastPause.ResumedAt = DateTime.Now;

            _currentSession.Result = TimerSessionResult.Cancelled;
            _currentSession.EndedAt = DateTime.Now;
            _historyService.Upsert(_currentSession);
        }

        _currentSession = null;
        _pausedAt = null;

        CurrentKind = TimerSessionKind.Work;
        PlannedSeconds = _settings.WorkSeconds;
        RemainingSeconds = PlannedSeconds;
        RunState = TimerRunState.Idle;

        UpdateRemainingText();
        UpdateProgress();
        LoadHistory();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (RemainingSeconds > 0)
        {
            RemainingSeconds--;
            UpdateRemainingText();
            UpdateProgress();
            return;
        }

        _tickTimer.Stop();

        var soundPath = CurrentKind == TimerSessionKind.Work ? _settings.WorkSoundPath : _settings.BreakSoundPath;
        _soundService.PlaySound(soundPath);
        IsAlarmPlaying = true;

        if (_currentSession is not null)
        {
            _currentSession.Result = TimerSessionResult.Completed;
            _currentSession.EndedAt = DateTime.Now;
            _historyService.Upsert(_currentSession);
        }

        var shouldAutoStart = CurrentKind == TimerSessionKind.Work
            ? _settings.AutoStartBreak
            : _settings.AutoStartWork;

        SwitchToNextPhase(autoStart: shouldAutoStart);
    }

    private void SwitchToNextPhase(bool autoStart)
    {
        CurrentKind = CurrentKind == TimerSessionKind.Work
            ? TimerSessionKind.Break
            : TimerSessionKind.Work;

        PlannedSeconds = CurrentKind == TimerSessionKind.Work
            ? _settings.WorkSeconds
            : _settings.BreakSeconds;

        RemainingSeconds = PlannedSeconds;
        UpdateRemainingText();
        UpdateProgress();

        if (!autoStart)
        {
            RunState = TimerRunState.Idle;
            LoadHistory();
            return;
        }

        _currentSession = new TimerSession
        {
            Kind = CurrentKind,
            PlannedDuration = TimeSpan.FromSeconds(PlannedSeconds),
            StartedAt = DateTime.Now,
            Result = TimerSessionResult.InProgress
        };
        _historyService.Upsert(_currentSession);
        LoadHistory();

        RunState = TimerRunState.Running;
        _tickTimer.Start();
    }

    private void StopAlarmIfPlaying()
    {
        if (!IsAlarmPlaying) return;

        _soundService.StopSound();
        IsAlarmPlaying = false;
    }

    private void UpdateRemainingText()
    {
        var span = TimeSpan.FromSeconds(Math.Max(0, RemainingSeconds));
        RemainingText = span.TotalHours >= 1
            ? span.ToString(@"h\:mm\:ss")
            : span.ToString(@"m\:ss");
    }

    private void UpdateProgress()
    {
        Progress = PlannedSeconds <= 0
            ? 0
            : 1.0 - (double)RemainingSeconds / PlannedSeconds;
    }
}
