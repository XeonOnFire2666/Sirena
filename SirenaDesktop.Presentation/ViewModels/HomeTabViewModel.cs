using System.Diagnostics;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.Logging;
using SirenaDesktop.Application.Contracts;
using SirenaDesktop.Core.Enums;
using SirenaDesktop.Core.Models;
using SirenaDesktop.Services;
using SkiaSharp;
using Tmds.DBus.Protocol;

namespace SirenaDesktop.ViewModels;

public partial class HomeTabViewModel : TabViewModelBase, IDisposable
{
    private readonly IDayStatsApiCaller _apiCaller;
    private readonly ILogger<HomeTabViewModel>? _logger;
    private readonly ProductivityPercent _productivityPercentCalc;
    private readonly ITokenRefreshService _refreshService;
    private readonly DayStatsService _statsService;
    private readonly DispatcherTimer _syncTimer;
    private readonly IJwtTokenContainer _tokenContainer;
    private readonly UserService _userService;
    private readonly IWorkspaceService _workspaceService;


    [ObservableProperty]
    private DrawMarginFrame? _chartFrame = new()
    {
        Fill = new SolidColorPaint(new SKColor(0xff, 0xf5, 0xeb))
    };

    [ObservableProperty] private Guid _currentTodo = Guid.Empty;

    [ObservableProperty] private string _currentTodoName = string.Empty;

    [ObservableProperty] private Guid _currentWokspace = Guid.Empty;

    [ObservableProperty] private string _currentWokspaceName = string.Empty;

    private bool _disposed;

    [ObservableProperty] private bool _hasData = true;

    [ObservableProperty] private bool _isFilterPopupOpen;

    [ObservableProperty] private bool _isShowingTodoStep;

    private bool _isUserLoaded;

    [ObservableProperty] private string[] _labels = Array.Empty<string>();

    private DateTime? _lastMobileSyncTime;

    [ObservableProperty] private string _platformButtonText = "Windows";

    [ObservableProperty] private IconType _platformIcon = IconType.Monitor;

    [ObservableProperty] private ISeries[] _pr = Array.Empty<ISeries>();

    [ObservableProperty] private ProductivityBreakdown _productivityBreakdown = new();

    [ObservableProperty] private int _productivityPercent;

    [ObservableProperty] private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty] private string _selectedPlatform = "Windows";

    [ObservableProperty] private SolidColorPaint _tooltipBackgroundPaint;

    [ObservableProperty] private SolidColorPaint _tooltipTextPaint;

    [ObservableProperty] private double _totalRecordedMinutes = 1;

    [ObservableProperty] private List<TodoItem> _totalTodo = [];

    [ObservableProperty] private string _totalTrackedTimeStr = "0h 0m";

    [ObservableProperty] private List<Workspace> _totalWorkspaces = [];

    private int _updateRequestId;

    [ObservableProperty] private string _userName = "Loading...";

    [ObservableProperty] private string _userPictureUrl = string.Empty;

    [ObservableProperty] private Axis[] _xAxes;

    [ObservableProperty] private Axis[] _yAxes;

    public HomeTabViewModel(
        DayStatsService statsService,
        IJwtTokenContainer tokenContainer,
        UserService userService,
        ITokenRefreshService refreshService,
        IDayStatsApiCaller apiCaller,
        IWorkspaceService workspaceService,
        ProductivityPercent productivityPercentCalc,
        ILogger<HomeTabViewModel>? logger = null)
        : base("Home", IconType.HomeOutline)
    {
        _statsService = statsService ?? throw new ArgumentNullException(nameof(statsService));
        _tokenContainer = tokenContainer ?? throw new ArgumentNullException(nameof(tokenContainer));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _refreshService = refreshService;
        _logger = logger;
        _apiCaller = apiCaller;
        _workspaceService = workspaceService;
        _productivityPercentCalc = productivityPercentCalc;

        _xAxes = [];
        _yAxes = [];

        _tooltipBackgroundPaint = new SolidColorPaint(new SKColor(0x2D, 0x28, 0x25));
        _tooltipTextPaint = new SolidColorPaint(new SKColor(0xF5, 0xF0, 0xEB));

        _syncTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10)
        };
        _syncTimer.Tick += OnSyncTimerTick;

        _ = InitializeAsync();
    }

    public string EmptyDataMessage
    {
        get
        {
            var platformStr = SelectedPlatform == "Windows" ? "PC" : "mobile";
            if (SelectedDate.Date == DateTime.Today) return $"{platformStr} statistics for today";
            return $"{platformStr} statistics for {SelectedDate:dd.MM.yyyy} not found";
        }
    }

    public string FilterButtonText
    {
        get
        {
            if (CurrentTodo != Guid.Empty) return CurrentTodoName;
            if (CurrentWokspace != Guid.Empty) return CurrentWokspaceName;
            return "All time";
        }
    }

    public string ChartTitle => CurrentTodo != Guid.Empty
        ? $"App activity in task \"{CurrentTodoName}\""
        : "App activity";


    public int TotalUniqueApps => Pr?.Length ?? 0;

    public void Dispose()
    {
        if (_disposed) return;

        _syncTimer.Stop();
        _syncTimer.Tick -= OnSyncTimerTick;

        _disposed = true;

        GC.SuppressFinalize(this);
    }


    [RelayCommand]
    private void OpenFilterPopup()
    {
        IsShowingTodoStep = CurrentWokspace != Guid.Empty;
        IsFilterPopupOpen = true;
    }

    [RelayCommand]
    private void TogglePlatform()
    {
        SelectedPlatform = SelectedPlatform == "Windows" ? "Android" : "Windows";
    }

    partial void OnSelectedDateChanged(DateTime value)
    {
        OnPropertyChanged(nameof(EmptyDataMessage));

        _ = LoadWorkspacesForDateAsync(value);
        _ = RefreshCurrentAsync();
    }

    partial void OnSelectedPlatformChanged(string value)
    {
        if (value == "Windows")
        {
            PlatformIcon = IconType.Monitor;
            PlatformButtonText = "Windows";
        }
        else
        {
            PlatformIcon = IconType.Smartphone;
            PlatformButtonText = "Android";
        }

        OnPropertyChanged(nameof(EmptyDataMessage));

        _ = RefreshCurrentAsync();
    }

    private Task RefreshCurrentAsync()
    {
        return CurrentTodo != Guid.Empty ? UpdateTodo() : UpdateTimelineFromDatabaseAsync();
    }


    [RelayCommand]
    private void PickWorkspaceInPopup(Workspace? workspace)
    {
        if (workspace == null)
        {
            CurrentWokspace = Guid.Empty;
            IsFilterPopupOpen = false;
            _ = UpdateTimelineFromDatabaseAsync();
            return;
        }

        CurrentWokspace = workspace.Id;
        IsShowingTodoStep = true;
    }

    [RelayCommand]
    private void PickTodoInPopup(TodoItem todo)
    {
        CurrentTodo = todo.Id;
        IsFilterPopupOpen = false;
    }

    [RelayCommand]
    private void BackToWorkspaceStep()
    {
        IsShowingTodoStep = false;
    }

    [RelayCommand]
    private async Task RefreshData()
    {
        if (SelectedPlatform == "Android") _lastMobileSyncTime = null;

        if (CurrentTodo != Guid.Empty)
            await UpdateTodo();
        else
            await UpdateTimelineFromDatabaseAsync();
    }

    public async Task InitializeAsync()
    {
        await LoadWorkspacesForDateAsync(SelectedDate);
        await UpdateTimelineFromDatabaseAsync();
        _syncTimer.Start();
    }

    private async void OnSyncTimerTick(object? sender, EventArgs e)
    {
        if (_disposed) return;

        _logger?.LogInformation("Timer tick - forcing update");

        if (CurrentTodo != Guid.Empty)
            await UpdateTodo();
        else
            await UpdateTimelineFromDatabaseAsync();
    }

    partial void OnCurrentWokspaceChanged(Guid value)
    {
        OnPropertyChanged(nameof(FilterButtonText));

        if (value == Guid.Empty)
        {
            TotalTodo = [];
            CurrentWokspaceName = string.Empty;
            CurrentTodo = Guid.Empty;
            return;
        }

        var workspace = TotalWorkspaces.FirstOrDefault(w => w.Id == value);
        if (workspace == null) return;

        CurrentWokspaceName = workspace.Name;
        TotalTodo = workspace.Todos;
        CurrentTodo = Guid.Empty;
    }

    partial void OnCurrentTodoChanged(Guid value)
    {
        OnPropertyChanged(nameof(FilterButtonText));
        OnPropertyChanged(nameof(ChartTitle));

        if (value == Guid.Empty)
        {
            CurrentTodoName = string.Empty;
            OnPropertyChanged(nameof(FilterButtonText));
            OnPropertyChanged(nameof(ChartTitle));
            _ = UpdateTimelineFromDatabaseAsync();
            return;
        }

        var todo = TotalTodo.FirstOrDefault(t => t.Id == value);
        if (todo == null) return;

        CurrentTodoName = todo.Title;
        OnPropertyChanged(nameof(FilterButtonText));
        OnPropertyChanged(nameof(ChartTitle));

        if (todo.StartTime == null || todo.EndTime == null)
        {
            _logger?.LogWarning("Todo {TodoId} has no StartTime/EndTime", value);
            return;
        }

        _ = UpdateTodo();
    }
    private static DateTime ToLocal(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;
    }

    private async Task UpdateTimelineFromDatabaseAsync()
    {
        if (_disposed) return;

        var currentRequestId = ++_updateRequestId;

        var capturedPlatform = SelectedPlatform;
        var capturedDate = SelectedDate;

        try
        {
            var userId = _tokenContainer.GetUserId();
            if (userId == Guid.Empty) return;

            await LoadUserDataIfNeededAsync(userId);
            if (currentRequestId != _updateRequestId) return;

            if (capturedPlatform == "Android" && capturedDate.Date == DateTime.Today)
            {
                var needsSync = !_lastMobileSyncTime.HasValue ||
                                DateTime.UtcNow - _lastMobileSyncTime.Value >= TimeSpan.FromMinutes(5);

                if (needsSync)
                {
                    _logger?.LogInformation("Starting synchronization...");
                    await SyncWithMobileToday();
                    _lastMobileSyncTime = DateTime.UtcNow;
                }
            }

            if (currentRequestId != _updateRequestId) return;

            await UpdateStatsAsync(userId, capturedDate, capturedPlatform, currentRequestId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating timeline from database");
            Debug.WriteLine($"[HomeTab Error]: {ex}");
        }
    }

    private async Task LoadUserDataIfNeededAsync(Guid userId)
    {
        if (_isUserLoaded) return;

        try
        {
            var user = await _userService.GetById(userId);
            if (user != null)
            {
                UserPictureUrl = user.PictureUrl;
                UserName = user.Name;
                _isUserLoaded = true;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load user data for {UserId}", userId);
        }
    }

    private async Task UpdateStatsAsync(Guid userId, DateTime targetDateTime, string platform, int requestId)
    {
        var targetDate = DateOnly.FromDateTime(targetDateTime);
        var todayStats = await _statsService.GetByDate(targetDate, userId);

        if (requestId != _updateRequestId) return;

        if (todayStats?.Tags == null || todayStats.Tags.Count == 0)
        {
            await Dispatcher.UIThread.InvokeAsync(ClearStatistics);
            return;
        }

        var platformKey = platform.ToLower();
        var platformStats = todayStats.Tags.Where(t => t.Platform?.ToLower() == platformKey).ToList();

        if (platformStats.Count == 0)
        {
            await Dispatcher.UIThread.InvokeAsync(ClearStatistics);
            return;
        }

        var sortedStats = SortStats(platformStats);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (requestId != _updateRequestId) return;

            Pr = [];
            XAxes = [];
            YAxes = [];

            HasData = false;
            _ = UpdateStatistics(sortedStats);
            UpdateChart(sortedStats);

            HasData = true;

            OnPropertyChanged(nameof(TotalUniqueApps));
        });
    }

    private async Task UpdatePeriodStatsAsync(Guid userId, DateTime startTime, DateTime endTime, string platform,
    int requestId)
    {
        var targetDate = DateOnly.FromDateTime(startTime);
        var dayStats = await _statsService.GetByDate(targetDate, userId);

        if (requestId != _updateRequestId) return;

        if (dayStats?.Tags == null || dayStats.Tags.Count == 0)
        {
            await Dispatcher.UIThread.InvokeAsync(ClearStatistics);
            return;
        }

        var platformKey = platform.ToLower();
        var periodStats = dayStats.Tags
            .Where(t => t.Platform?.ToLower() == platformKey)
            .Where(t => ToLocal(t.DateTime) >= startTime && ToLocal(t.DateTime) <= endTime)
            .ToList();

        if (periodStats.Count == 0)
        {
            _logger?.LogInformation("No tags in period {Start} - {End}", startTime, endTime);
            await Dispatcher.UIThread.InvokeAsync(ClearStatistics);
            return;
        }

        var sortedStats = SortStats(periodStats);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (requestId != _updateRequestId) return;

            Pr = [];
            XAxes = [];
            YAxes = [];

            HasData = false;
            _ = UpdateStatistics(sortedStats, false);
            UpdateChart(sortedStats);

            HasData = true;

            OnPropertyChanged(nameof(TotalUniqueApps));
        });
    }


    private void ClearStatistics()
    {
        Pr = [];
        XAxes = [];
        YAxes = [];

        HasData = false;

        TotalRecordedMinutes = 0;
        TotalTrackedTimeStr = "0h 0m";
        ProductivityPercent = 0;
        Labels = [];

        OnPropertyChanged(nameof(TotalUniqueApps));
    }

    private async Task UpdateStatistics(List<ActionTag> sortedStats, bool saveProductivity = true)
    {
        var recordedMinutes = 0;
        foreach (var time in sortedStats) recordedMinutes += time.RecordedMinutes;

        TotalRecordedMinutes = recordedMinutes;

        var totalHours = recordedMinutes / 60;
        var totalMinutes = recordedMinutes % 60;
        TotalTrackedTimeStr = $"{totalHours}h {totalMinutes}m";

        var userId = _tokenContainer.GetUserId();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var dayStats = await _statsService.GetByDate(today, userId);
        var studyUnits = dayStats?.StudyUnits ?? 0;
        var gitUnits = dayStats?.GitUnits ?? 0;

        ProductivityBreakdown = await _productivityPercentCalc.Calculate(
            sortedStats, today, gitUnits, studyUnits, null);

        ProductivityPercent = ProductivityBreakdown.Percent;

        if (saveProductivity)
            await _statsService.SetProductivityPercent(userId, today, ProductivityPercent);

        OnPropertyChanged(nameof(TotalRecordedMinutes));
        OnPropertyChanged(nameof(TotalTrackedTimeStr));
        OnPropertyChanged(nameof(ProductivityPercent));
        OnPropertyChanged(nameof(ProductivityBreakdown));
    }

    private void UpdateChart(List<ActionTag> sortedStats)
    {
        if (sortedStats == null || sortedStats.Count == 0)
        {
            Pr = [];
            XAxes = [];
            YAxes = [];
            return;
        }

        var totalMinutes = sortedStats.Sum(s => s.RecordedMinutes);

        var chartStats = totalMinutes > 0
            ? sortedStats.Where(s => (double)s.RecordedMinutes / totalMinutes >= 0.05).ToList()
            : sortedStats;

        if (chartStats.Count == 0)
        {
            Pr = [];
            XAxes = [];
            YAxes = [];
            return;
        }

        var labelsList = chartStats.Select(s =>
        {
            var raw = string.IsNullOrEmpty(s.Domain) ? s.AppTitle : s.Domain;
            return TruncateLabel(raw, 12);
        }).ToArray();

        Labels = labelsList;

        XAxes =
        [
            new Axis
            {
                Labels = labelsList,
                ShowSeparatorLines = false,
                LabelsPaint = new SolidColorPaint(new SKColor(0xBF, 0xB2, 0xA8))
                {
                    SKTypeface = SKTypeface.FromFamilyName("Infer")
                },
                TextSize = 12,
                MinLimit = -0.5,
                MaxLimit = labelsList.Length - 0.5
            }
        ];

        YAxes =
            [
                new Axis
            {
            MinLimit = 0,
            SeparatorsPaint = new SolidColorPaint(new SKColor(0x4A, 0x40, 0x39)) { StrokeThickness = 0.5f },
            LabelsPaint = new SolidColorPaint(new SKColor(0xBF, 0xB2, 0xA8))
            {
                SKTypeface = SKTypeface.FromFamilyName("Infer")
            },
            TextSize = 12
            }
            ];

        double dynamicWidth = chartStats.Count <= 5 ? 55 : 16;
        var newSeries = new ISeries[chartStats.Count];

        for (var i = 0; i < chartStats.Count; i++)
        {
            var app = chartStats[i];
            var skColor = ToSkColor(app.ColorHex);

            newSeries[i] = new StackedColumnSeries<ObservablePoint>
            {
                Name = string.IsNullOrEmpty(app.Domain) ? app.AppTitle : app.Domain,
                Values = new[] { new ObservablePoint(i, app.RecordedMinutes) },
                Fill = new SolidColorPaint(skColor),
                Stroke = null,
                MaxBarWidth = dynamicWidth,
                StackGroup = 0,
                DataLabelsPaint = null,
                AnimationsSpeed = TimeSpan.FromMilliseconds(300),
                Rx = 4,
                Ry = 4
            };
        }
        
        Pr = newSeries;
    }

    private static string TruncateLabel(string label, int maxLength)
    {
        if (string.IsNullOrEmpty(label) || label.Length <= maxLength)
            return label;

        return label.Substring(0, maxLength - 1) + "...";
    }

    private static SKColor ToSkColor(string colorHex)
    {
        try
        {
            var color = Color.Parse(colorHex);
            return new SKColor(color.R, color.G, color.B, color.A);
        }
        catch
        {
            return new SKColor(0x80, 0x80, 0x80);
        }
    }

    private static List<ActionTag> SortStats(List<ActionTag> tags)
    {
        return tags
            .GroupBy(t => new { t.AppTitle, t.Domain })
            .Select(g => ActionTag.Create(
                Guid.NewGuid(),
                g.First().TagId,
                g.First().DateTime,
                g.Key.AppTitle,
                g.Key.Domain,
                g.Sum(t => t.RecordedMinutes),
                g.First().Platform,
                g.First().ColorHex))
            .ToList();
    }

    public async Task SyncWithMobileToday()
    {
        var todayDate = DateOnly.FromDateTime(DateTime.Today);
        var userId = _tokenContainer.GetUserId();

        var todayStatMobile = await _apiCaller.GetFromMobile(userId, todayDate);

        if (todayStatMobile.IsError) Debug.WriteLine("----- Error ---- " + todayStatMobile.message);
        if (todayStatMobile.IsUnauthorizedError)
        {
            if (await _refreshService.TryRefreshAsync())
            {
                todayStatMobile = await _apiCaller.GetFromMobile(userId, todayDate);
                Debug.WriteLine("----- Refresh response ---- " + todayStatMobile.message);
            }
            else
            {
                Debug.WriteLine("----- Some second Refresh Error ---- ");
                return;
            }
        }

        if (todayStatMobile?.Stats?.Tags == null || todayStatMobile.Stats.Tags.Count == 0)
        {
            Debug.WriteLine("No new data.");
            return;
        }

        var incomingMobileTags = todayStatMobile.Stats.Tags;

        if (incomingMobileTags.Count > 0)
        {
            Debug.WriteLine($"Upserting {incomingMobileTags.Count} mobile tags...");
            await _statsService.UpdateStats(incomingMobileTags, userId, todayDate);
        }
        else
        {
            Debug.WriteLine("Nothing to update.");
        }
    }

    private async Task LoadWorkspacesForDateAsync(DateTime date)
    {
        if (_disposed) return;

        try
        {
            await _workspaceService.Sync();

            var workspaces = await _workspaceService.GetByDate(DateOnly.FromDateTime(date));

            TotalWorkspaces = workspaces ?? [];

            if (CurrentWokspace != Guid.Empty && !TotalWorkspaces.Any(w => w.Id == CurrentWokspace))
                CurrentWokspace = Guid.Empty;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading workspaces from database");
            Debug.WriteLine($"[HomeTab Error]: {ex}");
        }
    }

    private async Task UpdateTodo()
    {
        var userId = _tokenContainer.GetUserId();
        var selectedTodo = TotalTodo.FirstOrDefault(t => t.Id == CurrentTodo);

        if (selectedTodo?.StartTime == null || selectedTodo.EndTime == null) return;

        var currentRequestId = ++_updateRequestId;

        await UpdatePeriodStatsAsync(
            userId,
            ToLocal(selectedTodo.StartTime.Value),
            ToLocal(selectedTodo.EndTime.Value),
            SelectedPlatform,
            currentRequestId);
    }
}