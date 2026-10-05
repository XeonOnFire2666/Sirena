

namespace SirenaDesktop.ViewModels;

using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using SirenaDesktop.Application.Contracts;
using SirenaDesktop.Application.Contracts.Connect;
using SirenaDesktop.Application.Services;
using SirenaDesktop.Core.Enums;
using SirenaDesktop.Core.Models;
using SirenaDesktop.Infrastructure.AI;
using SirenaDesktop.Presentation.Converters;
using System.Collections.ObjectModel;
using System.Globalization;
public partial class TodoTabViewModel : TabViewModelBase
{
    private readonly TodoGitScoringService _gitScoringService;
    private readonly IJwtTokenContainer _tokenContainer;
    private readonly VideoStudyAnalysisService analysisService;
    private readonly IWorkspaceService service;
    private readonly Application.Contracts.Connect.IConnectionContainer _connectionContainer;
    private readonly DispatcherTimer _urgentRefreshTimer;

    private readonly SemaphoreSlim _reloadGate = new(1, 1);

    private List<Workspace> _allWorkspaces = [];
    private TodoItem? _editingItem;

    private DateOnly _weekStart;

    public TodoTabViewModel(IWorkspaceService service, TodoGitScoringService gitScoringService,
        IJwtTokenContainer tokenContainer, VideoStudyAnalysisService analysisService,
        Application.Contracts.Connect.IConnectionContainer connectionContainer) : base("Plan Day",
        IconType.Checklist)
    {
        this.service = service;
        _gitScoringService = gitScoringService;
        _tokenContainer = tokenContainer;
        this.analysisService = analysisService;
        _connectionContainer = connectionContainer;

        CompletedTasks.CollectionChanged += (s, e) => UpdateTasksState();

        service.Changed += OnWorkspacesChanged;
        _connectionContainer.StateChanged += OnConnectionStateChanged;

        IsOffline = _connectionContainer.IsOffline;

        _weekStart = GetMonday(SelectedDate);
        RebuildWeekDays();

        _urgentRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _urgentRefreshTimer.Tick += (_, _) => RefreshDaySummary();
        _urgentRefreshTimer.Start();

        _ = Reload();
    }

    public ObservableCollection<TodoItem> Tasks { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAnyDialogOpen))]
    public partial bool IsDialogOpen { get; set; }

    [ObservableProperty] public partial bool IsTasksEmpty { get; set; } = true;
    [ObservableProperty] public partial bool IsAllTasksCompleted { get; set; }
    [ObservableProperty] public partial bool HasCompletedTasks { get; set; }

    [ObservableProperty] public partial bool IsOffline { get; set; }

    [ObservableProperty] public partial string DialogTitle { get; set; } = "New Task";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAnyDialogOpen))]
    public partial bool IsDeleteWorkspaceDialogOpen { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveTaskCommand))]
    public partial string TaskTitleInput { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveTaskCommand))]
    public partial TimeSpan? TaskStartTimeInput { get; set; } = NowTime();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveTaskCommand))]
    public partial TimeSpan? TaskEndTimeInput { get; set; } = NowTime().Add(TimeSpan.FromHours(1));

    private static TimeSpan NowTime()
    {
        var now = DateTime.Now;
        return new TimeSpan(now.Hour, now.Minute, 0);
    }

    private static TimeSpan? ToLocalTime(DateTime? utc)
    {
        if (utc is null) return null;
        var local = DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc).ToLocalTime();
        return new TimeSpan(local.Hour, local.Minute, 0);
    }

    private static DateTime? ToUtc(DateOnly date, TimeSpan? time)
    {
        if (time is null) return null;
        var local = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue).Add(time.Value), DateTimeKind.Local);
        return local.ToUniversalTime();
    }

    public ObservableCollection<WeekDayItem> WeekDays { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedDayLabel))]
    [NotifyPropertyChangedFor(nameof(SelectedDateTitle))]
    [NotifyPropertyChangedFor(nameof(IsTodaySelected))]
    public partial DateOnly SelectedDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public string SelectedDayLabel => SelectedDate.ToString("d MMMM", CultureInfo.InvariantCulture);
    public string SelectedDateTitle => SelectedDate.ToString("dddd, MMM d", CultureInfo.InvariantCulture);
    public bool IsTodaySelected => SelectedDate == DateOnly.FromDateTime(DateTime.Today);

    [ObservableProperty] public partial string DaySubtitle { get; set; } = "Today";
    [ObservableProperty] public partial int DayDoneCount { get; set; }
    [ObservableProperty] public partial string DayTotalText { get; set; } = "of 0";
    [ObservableProperty] public partial double DayProgress { get; set; }

    [ObservableProperty] public partial bool HasNextTask { get; set; }
    [ObservableProperty] public partial NextTaskInfo? NextTask { get; set; }

    public ObservableCollection<UrgentTaskItem> UrgentTasks { get; } = [];
    [ObservableProperty] public partial bool HasUrgentTasks { get; set; }

    [ObservableProperty]
    public partial string WeekRangeTitle { get; set; } = string.Empty;

    public ObservableCollection<WorkspaceCardItem> WorkspaceCards { get; } = [];

    public bool HasWorkspacesForSelectedDay => WorkspaceCards.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasWorkspaceForSelectedDay))]
    [NotifyPropertyChangedFor(nameof(SelectedWorkspaceType))]
    [NotifyPropertyChangedFor(nameof(IsWorkspaceOpen))]
    [NotifyPropertyChangedFor(nameof(HeaderTitle))]
    [NotifyPropertyChangedFor(nameof(IsAnyDialogOpen))]
    [NotifyPropertyChangedFor(nameof(IsWorkspaceGridVisible))]
    public partial Workspace? SelectedWorkspace { get; set; }

    public WorkspaceType? SelectedWorkspaceType => SelectedWorkspace?.Type;
    public bool IsWorkspaceOpen => SelectedWorkspace is not null;
    public bool HasWorkspaceForSelectedDay => SelectedWorkspace is not null;
    public bool IsWorkspaceGridVisible => SelectedWorkspace is null;
    public bool IsNamingWorkspace => PendingWorkspaceType is not null;
    public string HeaderTitle => SelectedWorkspace?.Name is { Length: > 0 } name ? name : "My Day";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAnyDialogOpen))]
    public partial bool IsWorkspaceDialogOpen { get; set; }

    public bool IsAnyDialogOpen => IsDialogOpen || IsWorkspaceDialogOpen || IsDeleteWorkspaceDialogOpen;
    public List<WorkspaceType> AvailableWorkspaceTypes { get; } = WorkspaceType.GetDefaultTypes();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPickingWorkspaceType))]
    [NotifyPropertyChangedFor(nameof(IsNamingWorkspace))]
    public partial WorkspaceType? PendingWorkspaceType { get; set; }

    public bool IsPickingWorkspaceType => PendingWorkspaceType is null;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCreateWorkspaceCommand))]
    public partial string NewWorkspaceNameInput { get; set; } = string.Empty;

    public ObservableCollection<TodoItem> CompletedTasks { get; } = [];

    [ObservableProperty] public partial bool IsSaving { get; set; }

    private static DateOnly GetMonday(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }

    private void OnWorkspacesChanged(object? sender, EventArgs e) =>
        Dispatcher.UIThread.Post(() => _ = Reload());

    private void OnConnectionStateChanged(bool isError) =>
    Dispatcher.UIThread.Post(() => IsOffline = isError);

    private async Task Reload()
    {
        await _reloadGate.WaitAsync();
        try
        {
            _allWorkspaces = await service.GetAll();

            if (SelectedWorkspace is not null)
            {
                SelectedWorkspace = _allWorkspaces.FirstOrDefault(w => w.Id == SelectedWorkspace.Id && !w.IsDeleted);
                ApplyDateFilter();
            }

            RebuildWorkspaceCards();
            RebuildWeekDays();
            RefreshDaySummary();
        }
        finally
        {
            _reloadGate.Release();
        }
    }

    private void RebuildWeekDays()
    {
        WeekDays.Clear();
        var culture = CultureInfo.InvariantCulture;
        var today = DateOnly.FromDateTime(DateTime.Today);

        for (var i = 0; i < 7; i++)
        {
            var date = _weekStart.AddDays(i);
            var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

            WeekDays.Add(new WeekDayItem
            {
                Date = date,
                DayOfWeekShort = CapitalizeFirst(culture.DateTimeFormat.GetAbbreviatedDayName(date.DayOfWeek)),
                DayNumber = date.Day.ToString(),
                IsWeekend = isWeekend,
                IsSelected = date == SelectedDate,
                IsToday = date == today,
                HasTasks = HasTasksOn(date)
            });
        }

        UpdateWeekRangeTitle();
    }

    private bool HasTasksOn(DateOnly date)
    {
        return _allWorkspaces.Any(w => !w.IsDeleted && w.Date == date &&
                                        w.Todos.Any(t => !t.IsDeleted));
    }

    private static string CapitalizeFirst(string s)
    {
        return string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];
    }

    private void UpdateWeekRangeTitle()
    {
        WeekRangeTitle = BuildWeekRangeTitle(WeekDays);
    }

    private static string BuildWeekRangeTitle(IEnumerable<WeekDayItem> days)
    {
        var list = days.ToList();
        if (list.Count == 0)
            return string.Empty;

        var culture = CultureInfo.GetCultureInfo("en-US");
        var first = list.First().Date;
        var last = list.Last().Date;

        if (first.Year != last.Year)
            return $"{first.ToString("MMM yyyy", culture)} – {last.ToString("MMM yyyy", culture)}";

        if (first.Month != last.Month)
            return $"{first.ToString("MMMM", culture)} – {last.ToString("MMMM yyyy", culture)}";

        return first.ToString("MMMM yyyy", culture);
    }

    [RelayCommand]
    private void PrevWeek()
    {
        _weekStart = _weekStart.AddDays(-7);
        RebuildWeekDays();
    }

    [RelayCommand]
    private void NextWeek()
    {
        _weekStart = _weekStart.AddDays(7);
        RebuildWeekDays();
    }

    [RelayCommand]
    private void GoToToday()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (!WeekDays.Any(d => d.Date == today))
        {
            _weekStart = GetMonday(today);
            RebuildWeekDays();
        }

        var item = WeekDays.FirstOrDefault(d => d.Date == today);
        if (item is not null)
            SelectDay(item);
    }

    [RelayCommand]
    private void OpenDeleteWorkspaceDialog(WorkspaceCardItem card)
    {
        if (card?.Workspace is null) return;

        SelectedWorkspace = card.Workspace;
        IsDeleteWorkspaceDialogOpen = true;
    }

    [RelayCommand]
    private void CancelDeleteWorkspaceDialog()
    {
        IsDeleteWorkspaceDialogOpen = false;
    }

    [RelayCommand]
    private async Task ConfirmDeleteWorkspace()
    {
        if (SelectedWorkspace is null) return;

        var ws = SelectedWorkspace;
        await service.Delete(ws);

        IsDeleteWorkspaceDialogOpen = false;
        SelectedWorkspace = null;
        Tasks.Clear();
        CompletedTasks.Clear();
    }

    [RelayCommand]
    private void SelectDay(WeekDayItem day)
    {
        if (day is null) return;

        SelectedDate = day.Date;

        foreach (var d in WeekDays)
            d.IsSelected = d.Date == day.Date;

        SelectedWorkspace = null;
        RebuildWorkspaceCards();
        RefreshDaySummary();
    }

    private void RebuildWorkspaceCards()
    {
        WorkspaceCards.Clear();

        var todaysWorkspaces = _allWorkspaces
            .Where(w => !w.IsDeleted && w.Date == SelectedDate)
            .OrderBy(w => w.UpdatedAt)
            .ToList();

        for (var i = 0; i < todaysWorkspaces.Count; i++)
        {
            var isWide = i % 3 == 0;
            WorkspaceCards.Add(new WorkspaceCardItem(todaysWorkspaces[i], isWide));
        }

        OnPropertyChanged(nameof(HasWorkspacesForSelectedDay));
    }

    [RelayCommand]
    private void OpenWorkspace(WorkspaceCardItem card)
    {
        if (card is null) return;

        SelectedWorkspace = card.Workspace;
        ApplyDateFilter();
        RefreshDaySummary();
    }

    [RelayCommand]
    private void CloseWorkspace()
    {
        SelectedWorkspace = null;
        Tasks.Clear();
        CompletedTasks.Clear();
        RebuildWorkspaceCards();
        RefreshDaySummary();
    }

    [RelayCommand]
    private void OpenCreateWorkspaceDialog()
    {
        PendingWorkspaceType = null;
        NewWorkspaceNameInput = string.Empty;
        IsWorkspaceDialogOpen = true;
    }

    [RelayCommand]
    private void CancelWorkspaceDialog()
    {
        IsWorkspaceDialogOpen = false;
        PendingWorkspaceType = null;
        NewWorkspaceNameInput = string.Empty;
    }

    [RelayCommand]
    private void PickWorkspaceType(WorkspaceType type)
    {
        if (type is null) return;
        PendingWorkspaceType = type;
        NewWorkspaceNameInput = type.Title;
    }

    [RelayCommand]
    private void BackToTypePicker()
    {
        PendingWorkspaceType = null;
        NewWorkspaceNameInput = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanConfirmCreateWorkspace))]
    private async Task ConfirmCreateWorkspace()
    {
        if (PendingWorkspaceType is null) return;

        await service.Add(PendingWorkspaceType.Id, NewWorkspaceNameInput, SelectedDate);

        IsWorkspaceDialogOpen = false;
        PendingWorkspaceType = null;
        NewWorkspaceNameInput = string.Empty;
    }

    private bool CanConfirmCreateWorkspace()
    {
        return !string.IsNullOrWhiteSpace(NewWorkspaceNameInput);
    }

    private void ApplyDateFilter()
    {
        Tasks.Clear();
        CompletedTasks.Clear();

        if (SelectedWorkspace is not null)
            foreach (var task in SelectedWorkspace.Todos.Where(t => !t.IsDeleted))
                if (task.IsCompleted)
                    CompletedTasks.Add(task);
                else
                    Tasks.Add(task);

        UpdateTasksState();
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await Reload();
    }

    private void UpdateTasksState()
    {
        IsTasksEmpty = IsWorkspaceOpen && !Tasks.Any() && !CompletedTasks.Any();
        IsAllTasksCompleted = IsWorkspaceOpen && Tasks.Count == 0 && CompletedTasks.Any();
        HasCompletedTasks = CompletedTasks.Any();
    }

    private List<TodoItem> GetAllTodosForSelectedDate()
    {
        return _allWorkspaces
            .Where(w => !w.IsDeleted && w.Date == SelectedDate)
            .SelectMany(w => w.Todos)
            .Where(t => !t.IsDeleted)
            .ToList();
    }

    private void RefreshDaySummary()
    {
        var allTasks = GetAllTodosForSelectedDate();

        var total = allTasks.Count;
        var done = allTasks.Count(t => t.IsCompleted);
        DayDoneCount = done;
        DayTotalText = $"of {total}";
        DayProgress = total == 0 ? 0 : (double)done / total * 100.0;

        UpdateDaySubtitle();
        RefreshNextTask(allTasks);
        RefreshUrgentTasks(allTasks);
        RefreshWeekDaysIndicators();
    }

    private void UpdateDaySubtitle()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var diff = SelectedDate.DayNumber - today.DayNumber;
        DaySubtitle = diff switch
        {
            0 => "Today",
            1 => "Tomorrow",
            -1 => "Yesterday",
            _ => SelectedDate.ToString("dddd", CultureInfo.InvariantCulture)
        };
    }

    private void RefreshWeekDaysIndicators()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        foreach (var d in WeekDays)
        {
            d.IsToday = d.Date == today;
            d.HasTasks = HasTasksOn(d.Date);
        }
    }

    private void RefreshNextTask(List<TodoItem> allTasks)
    {
        var nowUtc = DateTime.UtcNow;

        var candidates = allTasks
            .Where(t => !t.IsCompleted && t.StartTime is not null)
            .OrderBy(t => t.StartTime)
            .ToList();

        var next = candidates.FirstOrDefault(t => t.StartTime >= nowUtc)
                   ?? candidates.FirstOrDefault();

        if (next?.StartTime is { } startUtc)
        {
            HasNextTask = true;
            NextTask = new NextTaskInfo
            {
                Title = next.Title,
                StatusText = FormatTaskStatus(startUtc, next.EndTime)
            };
        }
        else
        {
            HasNextTask = false;
            NextTask = null;
        }
    }

    private void RefreshUrgentTasks(List<TodoItem> allTasks)
    {
        UrgentTasks.Clear();

        var nowUtc = DateTime.UtcNow;
        var cutoffUtc = nowUtc.AddHours(2);

        var urgent = allTasks
            .Where(t => !t.IsCompleted && !t.IsDeleted && t.StartTime is not null)
            .Where(t => IsUrgent(t, nowUtc, cutoffUtc))
            .OrderBy(t => t.StartTime)
            .Take(2)
            .ToList();

        foreach (var task in urgent)
        {
            var ws = _allWorkspaces.FirstOrDefault(w => !w.IsDeleted && w.Todos.Contains(task));
            if (ws is null) continue;
            UrgentTasks.Add(new UrgentTaskItem(task, ws));
        }

        HasUrgentTasks = UrgentTasks.Count > 0;
    }

    private static bool IsUrgent(TodoItem task, DateTime nowUtc, DateTime cutoffUtc)
    {
        if (task.StartTime is not { } start) return false;
        if (start > cutoffUtc) return false;

        if (start <= nowUtc)
        {
            var end = task.EndTime ?? start.AddHours(1);
            return end >= nowUtc;
        }

        return true;
    }

    private static string FormatTaskStatus(DateTime startUtc, DateTime? endUtc)
    {
        var start = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc).ToLocalTime();
        var end = endUtc is { } e ? DateTime.SpecifyKind(e, DateTimeKind.Utc).ToLocalTime() : (DateTime?)null;
        var now = DateTime.Now;

        var isActive = start <= now && (end is null || end >= now);
        if (isActive) return "Now";

        var diff = start - now;
        if (diff.TotalMinutes <= 0) return "Overdue";
        if (diff.TotalMinutes < 1) return "in <1m";
        if (diff.TotalMinutes < 60) return $"in {(int)diff.TotalMinutes}m";
        return $"in {(int)diff.TotalHours}h {diff.Minutes}m";
    }

    [RelayCommand]
    private async Task CompleteTask(TodoItem item)
    {
        if (item == null || SelectedWorkspace is null) return;

        var newState = !item.IsCompleted;

        var todosSnapshotBefore = GetAllTodosForSelectedDate();

        await service.SetTodoCompletion(SelectedWorkspace, item, newState);

        if (newState)
        {
            var userId = _tokenContainer.GetUserId();
            await _gitScoringService.ScoreCompletedTodoAsync(item, SelectedWorkspace.WorkspaceTypeId, userId);

            await analysisService.RunIfLastTaskAsync(item, todosSnapshotBefore, userId);
        }

    }

    [RelayCommand]
    private async Task CompleteUrgentTask(UrgentTaskItem urgentItem)
    {
        if (urgentItem?.Task is not { } item) return;

        var workspace = urgentItem.Workspace;
        if (workspace is null) return;

        var newState = !item.IsCompleted;

        var todosSnapshotBefore = GetAllTodosForSelectedDate();

        await service.SetTodoCompletion(workspace, item, newState);

        if (newState)
        {
            var userId = _tokenContainer.GetUserId();
            await _gitScoringService.ScoreCompletedTodoAsync(item, workspace.WorkspaceTypeId, userId);

            await analysisService.RunIfLastTaskAsync(item, todosSnapshotBefore, userId);
        }

    }

    [RelayCommand]
    private async Task DeleteTask(TodoItem item)
    {
        if (item == null || SelectedWorkspace is null) return;
        if (!SelectedWorkspace.Todos.Contains(item)) return;

        await service.DeleteTodo(SelectedWorkspace, item);
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        if (!HasWorkspaceForSelectedDay) return;

        _editingItem = null;
        DialogTitle = "New Task";
        TaskTitleInput = string.Empty;
        TaskStartTimeInput = NowTime();
        TaskEndTimeInput = NowTime().Add(TimeSpan.FromHours(1));
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(TodoItem item)
    {
        _editingItem = item;
        DialogTitle = "Edit Task";
        TaskTitleInput = item.Title;
        TaskStartTimeInput = ToLocalTime(item.StartTime);
        TaskEndTimeInput = ToLocalTime(item.EndTime);
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void CancelDialog()
    {
        IsDialogOpen = false;
    }

    [RelayCommand(CanExecute = nameof(CanSaveTask))]
    private async Task SaveTask()
    {
        if (IsSaving) return;
        if (SelectedWorkspace is null) return;

        IsSaving = true;
        try
        {
            var finalStart = ToUtc(SelectedDate, TaskStartTimeInput);
            var finalEnd = ToUtc(SelectedDate, TaskEndTimeInput);

            if (_editingItem == null)
            {
                await service.AddTodo(SelectedWorkspace, TaskTitleInput.Trim(), finalStart, finalEnd);
            }
            else
            {
                _editingItem.Title = TaskTitleInput.Trim();
                _editingItem.StartTime = finalStart;
                _editingItem.EndTime = finalEnd;
                _editingItem.UpdatedAt = DateTime.UtcNow;

                await service.UpdateTodo(SelectedWorkspace, _editingItem);
            }

            IsDialogOpen = false;
        }
        finally
        {
            IsSaving = false;
        }
    }

    private bool CanSaveTask()
    {
        if (string.IsNullOrWhiteSpace(TaskTitleInput)) return false;
        if (TaskStartTimeInput is { } s && TaskEndTimeInput is { } e && e <= s) return false;
        return true;
    }
}

public class WorkspaceCardItem
{
    public WorkspaceCardItem(Workspace workspace, bool isWide = false)
    {
        Workspace = workspace;
        IsWide = isWide;

        var color = ParseColor(workspace.ColorHex);
        AccentBrush = new SolidColorBrush(color);
        AccentSoftBrush = new SolidColorBrush(color, 0.18);
    }

    public Workspace Workspace { get; }
    public bool IsWide { get; }

    public ISolidColorBrush AccentBrush { get; }
    public ISolidColorBrush AccentSoftBrush { get; }

    public string Name => string.IsNullOrWhiteSpace(Workspace.Name)
        ? Workspace.Type?.Title ?? "Untitled"
        : Workspace.Name;

    public MaterialIconKind Icon => Workspace.Type?.Icon is { } iconType
        ? IconTypeConverter.ToMaterialIcon(iconType)
        : MaterialIconKind.ViewGridOutline;

    public int TotalCount => Workspace.Todos.Count(t => !t.IsDeleted);
    public int CompletedCount => Workspace.Todos.Count(t => !t.IsDeleted && t.IsCompleted);
    public string ProgressText => TotalCount == 0 ? "No tasks" : $"{CompletedCount}/{TotalCount} completed";

    public double Progress => TotalCount == 0 ? 0 : (double)CompletedCount / TotalCount * 100.0;

    public bool IsEmpty => TotalCount == 0;

    public IReadOnlyList<string> PreviewTitles => Workspace.Todos
        .Where(t => !t.IsDeleted && !t.IsCompleted)
        .OrderBy(t => t.StartTime ?? DateTime.MaxValue)
        .Take(4)
        .Select(t => t.Title)
        .ToList();

    private static Color ParseColor(string hex)
    {
        try
        {
            return Color.Parse(string.IsNullOrWhiteSpace(hex) ? "#8AB4F8" : hex);
        }
        catch
        {
            return Colors.Gray;
        }
    }
}


public sealed class UrgentTaskItem
{
    public UrgentTaskItem(TodoItem task, Workspace workspace)
    {
        Task = task;
        Workspace = workspace;

        WorkspaceName = string.IsNullOrWhiteSpace(workspace.Name)
            ? workspace.Type?.Title ?? "Untitled"
            : workspace.Name;

        WorkspaceIcon = workspace.Type?.Icon is { } iconType
            ? IconTypeConverter.ToMaterialIcon(iconType)
            : MaterialIconKind.ViewGridOutline;

        var (isActive, isSoon, text) = ComputeStatus(task);
        IsActive = isActive;
        IsSoon = isSoon;
        StatusText = text;
    }

    public TodoItem Task { get; }

    public Workspace Workspace { get; }

    public string WorkspaceName { get; }
    public MaterialIconKind WorkspaceIcon { get; }

    public string Title => Task.Title;
    public bool IsActive { get; }
    public bool IsSoon { get; }
    public string StatusText { get; }

    private static (bool IsActive, bool IsSoon, string Text) ComputeStatus(TodoItem task)
    {
        if (task.StartTime is not { } startUtc)
            return (false, false, string.Empty);

        var start = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc).ToLocalTime();
        var end = task.EndTime is { } e
            ? DateTime.SpecifyKind(e, DateTimeKind.Utc).ToLocalTime()
            : (DateTime?)null;
        var now = DateTime.Now;

        var isActive = start <= now && (end is null || end >= now);
        if (isActive) return (true, false, "Now");

        var diff = start - now;
        if (diff.TotalMinutes <= 0) return (false, false, "Overdue");

        var isSoon = diff.TotalMinutes <= 30;
        var text = diff.TotalMinutes < 60
            ? $"in {(int)Math.Max(1, diff.TotalMinutes)}m"
            : $"in {(int)diff.TotalHours}h {diff.Minutes}m";

        return (false, isSoon, text);
    }
}


public sealed class NextTaskInfo
{
    public string Title { get; init; } = string.Empty;
    public string StatusText { get; init; } = string.Empty;
}