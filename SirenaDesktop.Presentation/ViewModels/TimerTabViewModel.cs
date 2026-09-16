using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirenaDesktop.Core.Enums;
using SirenaDesktop.Core.Models;
using SirenaDesktop.Services;

namespace SirenaDesktop.ViewModels;

public enum TimerRunState
{
    Idle,
    Running,
    Paused
}

public partial class TimerTabViewModel : TabViewModelBase
{
    private const int DefaultWorkMinutes = 25;
    private const int DefaultBreakMinutes = 5;
    private const int MinDurationSeconds = 30;
    private readonly TimerHistoryService _historyService;
    private readonly TimerSoundService _soundService;

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

    public TimerTabViewModel() : base("Timer", IconType.TimerOutline)
    {
        _historyService = new TimerHistoryService();
        _soundService = new TimerSoundService();

        _tickTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _tickTimer.Tick += OnTick;

        RemainingSeconds = DefaultWorkMinutes * 60;
        PlannedSeconds = RemainingSeconds;
        CurrentKind = TimerSessionKind.Work;

        IsHistoryCollapsed = true;

        LoadHistory();
        UpdateRemainingText();
    }

    public bool HasHistory => HistoryGroups.Count > 0;
    public ObservableCollection<TimerHistoryGroup> HistoryGroups { get; } = new();

    public bool IsIdle => RunState == TimerRunState.Idle;
    public bool IsRunning => RunState == TimerRunState.Running;
    public bool IsPaused => RunState == TimerRunState.Paused;
    public bool IsTicking => RunState != TimerRunState.Idle;

    public string CurrentKindLabel => CurrentKind == TimerSessionKind.Work ? "Work session" : "Break session";

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

    partial void OnCurrentKindChanged(TimerSessionKind value)
    {
        OnPropertyChanged(nameof(CurrentKindLabel));
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
        PlannedSeconds = DefaultWorkMinutes * 60;
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
        _soundService.PlayTimerEndSound();

        if (_currentSession is not null)
        {
            _currentSession.Result = TimerSessionResult.Completed;
            _currentSession.EndedAt = DateTime.Now;
            _historyService.Upsert(_currentSession);
        }

        SwitchToNextPhaseAndAutoStart();
    }

    private void SwitchToNextPhaseAndAutoStart()
    {
        CurrentKind = CurrentKind == TimerSessionKind.Work
            ? TimerSessionKind.Break
            : TimerSessionKind.Work;

        PlannedSeconds = CurrentKind == TimerSessionKind.Work
            ? DefaultWorkMinutes * 60
            : DefaultBreakMinutes * 60;

        RemainingSeconds = PlannedSeconds;
        UpdateRemainingText();
        UpdateProgress();

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