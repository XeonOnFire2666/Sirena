using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using SirenaDesktop.Application.Contracts;
using SirenaDesktop.Application.Services;
using SirenaDesktop.Core.Enums;
using SirenaDesktop.Core.Models;
using SirenaDesktop.Infrastructure.AI;
using SirenaDesktop.Presentation.Converters;

namespace SirenaDesktop.ViewModels;

public partial class TodoTabViewModel : TabViewModelBase
{
    private readonly TodoGitScoringService _gitScoringService;
    private readonly IJwtTokenContainer _tokenContainer;
    private readonly VideoStudyAnalysisService analysisService;
    private readonly IWorkspaceService service;
    private List<Workspace> _allWorkspaces = [];
    private TodoItem? _editingItem;

    private DateOnly _weekStart;

    public TodoTabViewModel(IWorkspaceService service, TodoGitScoringService gitScoringService,
        IJwtTokenContainer tokenContainer, VideoStudyAnalysisService analysisService) : base("Plan Day",
        IconType.Checklist)
    {
        this.service = service;
        _gitScoringService = gitScoringService;
        _tokenContainer = tokenContainer;
        this.analysisService = analysisService;

        CompletedTasks.CollectionChanged += (s, e) => UpdateTasksState();

        _weekStart = GetMonday(SelectedDate);
        RebuildWeekDays();

        _ = SyncAndRefresh();
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

    [ObservableProperty] public partial DateTime? TaskStartTimeInput { get; set; } = DateTime.Now;
    [ObservableProperty] public partial DateTime? TaskEndTimeInput { get; set; } = DateTime.Now.AddHours(1);
    public ObservableCollection<WeekDayItem> WeekDays { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedDayLabel))]
    public partial DateOnly SelectedDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public string SelectedDayLabel => SelectedDate.ToString("d MMMM", CultureInfo.InvariantCulture);
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

    private void RebuildWeekDays()
    {
        WeekDays.Clear();
        var culture = CultureInfo.InvariantCulture;

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
                IsSelected = date == SelectedDate
            });
        }
    }

    private static string CapitalizeFirst(string s)
    {
        return string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..2];
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

        _allWorkspaces.RemoveAll(w => w.Id == ws.Id);

        IsDeleteWorkspaceDialogOpen = false;
        SelectedWorkspace = null;
        Tasks.Clear();
        CompletedTasks.Clear();
        RebuildWorkspaceCards();
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
    }

    private void RebuildWorkspaceCards()
    {
        WorkspaceCards.Clear();

        var todaysWorkspaces = _allWorkspaces
            .Where(w => !w.IsDeleted && w.Date == SelectedDate)
            .OrderBy(w => w.UpdatedAt);

        foreach (var ws in todaysWorkspaces)
            WorkspaceCards.Add(new WorkspaceCardItem(ws));

        OnPropertyChanged(nameof(HasWorkspacesForSelectedDay));
    }

    [RelayCommand]
    private void OpenWorkspace(WorkspaceCardItem card)
    {
        if (card is null) return;

        SelectedWorkspace = card.Workspace;
        ApplyDateFilter();
    }

    [RelayCommand]
    private void CloseWorkspace()
    {
        SelectedWorkspace = null;
        Tasks.Clear();
        CompletedTasks.Clear();
        RebuildWorkspaceCards();
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

        var ws = await service.Add(PendingWorkspaceType.Id, NewWorkspaceNameInput, SelectedDate);
        _allWorkspaces.Add(ws);

        IsWorkspaceDialogOpen = false;
        PendingWorkspaceType = null;
        NewWorkspaceNameInput = string.Empty;

        RebuildWorkspaceCards();
    }

    private bool CanConfirmCreateWorkspace()
    {
        return !string.IsNullOrWhiteSpace(NewWorkspaceNameInput);
    }

    private async Task SyncAndRefresh()
    {
        var result = await service.Sync();
        IsOffline = result.IsOffline;
        _allWorkspaces = result.Workspaces;

        if (SelectedWorkspace is not null && !_allWorkspaces.Any(w => w.Id == SelectedWorkspace.Id && !w.IsDeleted))
            SelectedWorkspace = null;

        RebuildWorkspaceCards();

        if (SelectedWorkspace is not null)
        {
            SelectedWorkspace = _allWorkspaces.FirstOrDefault(w => w.Id == SelectedWorkspace.Id);
            ApplyDateFilter();
        }
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
        await SyncAndRefresh();
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

    [RelayCommand]
    private async Task CompleteTask(TodoItem item)
    {
        if (item == null || SelectedWorkspace is null) return;

        var newState = !item.IsCompleted;


        var todosSnapshotBefore = GetAllTodosForSelectedDate();

        item.IsCompleted = newState;
        item.CompleteDate = newState ? DateTime.UtcNow : null;
        item.UpdatedAt = DateTime.UtcNow;

        await service.SetTodoCompletion(SelectedWorkspace, item, newState);

        if (newState)
        {
            var userId = _tokenContainer.GetUserId();
            await _gitScoringService.ScoreCompletedTodoAsync(item, SelectedWorkspace.WorkspaceTypeId, userId);

            await analysisService.RunIfLastTaskAsync(item, todosSnapshotBefore, userId);
        }

        ApplyDateFilter();
    }

    [RelayCommand]
    private async Task DeleteTask(TodoItem item)
    {
        if (item == null || SelectedWorkspace is null) return;
        if (!SelectedWorkspace.Todos.Contains(item)) return;

        await service.DeleteTodo(SelectedWorkspace, item);
        ApplyDateFilter();
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        if (!HasWorkspaceForSelectedDay) return;

        _editingItem = null;
        DialogTitle = "New Task";
        TaskTitleInput = string.Empty;
        TaskStartTimeInput = DateTime.Now;
        TaskEndTimeInput = DateTime.Now.AddHours(1);
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(TodoItem item)
    {
        _editingItem = item;
        DialogTitle = "Edit Task";
        TaskTitleInput = item.Title;

        TaskStartTimeInput = item.StartTime.HasValue
            ? DateTime.SpecifyKind(item.StartTime.Value, DateTimeKind.Utc).ToLocalTime()
            : null;

        TaskEndTimeInput = item.EndTime.HasValue
            ? DateTime.SpecifyKind(item.EndTime.Value, DateTimeKind.Utc).ToLocalTime()
            : null;

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
            var targetDate = SelectedDate.ToDateTime(TimeOnly.MinValue);

            DateTime? finalStart = TaskStartTimeInput.HasValue
                ? DateTime.SpecifyKind(new DateTime(
                            targetDate.Year, targetDate.Month, targetDate.Day,
                            TaskStartTimeInput.Value.Hour, TaskStartTimeInput.Value.Minute, 0),
                        DateTimeKind.Local)
                    .ToUniversalTime()
                : null;

            DateTime? finalEnd = TaskEndTimeInput.HasValue
                ? DateTime.SpecifyKind(new DateTime(
                            targetDate.Year, targetDate.Month, targetDate.Day,
                            TaskEndTimeInput.Value.Hour, TaskEndTimeInput.Value.Minute, 0),
                        DateTimeKind.Local)
                    .ToUniversalTime()
                : null;

            if (_editingItem == null)
            {
                await service.AddTodo(SelectedWorkspace, TaskTitleInput.Trim(), finalStart, finalEnd);
            }
            else
            {
                _editingItem.Title = TaskTitleInput.Trim();
                _editingItem.StartTime = finalStart;
                _editingItem.EndTime = finalEnd;

                await service.UpdateTodo(SelectedWorkspace, _editingItem);
            }

            IsDialogOpen = false;
            ApplyDateFilter();
            RebuildWorkspaceCards();
        }
        finally
        {
            IsSaving = false;
        }
    }

    private bool CanSaveTask()
    {
        return !string.IsNullOrWhiteSpace(TaskTitleInput);
    }
}

public class WorkspaceCardItem
{
    public WorkspaceCardItem(Workspace workspace)
    {
        Workspace = workspace;
        Background = CreateBrush(workspace.ColorHex);
    }

    public Workspace Workspace { get; }
    public SolidColorBrush Background { get; }

    public string Name => string.IsNullOrWhiteSpace(Workspace.Name)
        ? Workspace.Type?.Title ?? "Untitled"
        : Workspace.Name;

    public PackIconKind Icon => Workspace.Type?.Icon is { } iconType
        ? IconTypeConverter.ToPackIcon(iconType)
        : PackIconKind.ViewGridOutline;

    public int TotalCount => Workspace.Todos.Count(t => !t.IsDeleted);
    public int CompletedCount => Workspace.Todos.Count(t => !t.IsDeleted && t.IsCompleted);
    public string ProgressText => TotalCount == 0 ? "No tasks" : $"{CompletedCount}/{TotalCount} completed";

    private static SolidColorBrush CreateBrush(string hex)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(string.IsNullOrWhiteSpace(hex) ? "#8AB4F8" : hex);
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
        catch
        {
            var fallback = new SolidColorBrush(Colors.Gray);
            fallback.Freeze();
            return fallback;
        }
    }
}