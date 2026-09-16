using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using SirenaDesktop.Application.Contracts;
using SirenaDesktop.Core.Enums;
using SirenaDesktop.Core.Models;
using SirenaDesktop.Notifications;
using SirenaDesktop.Services;

namespace SirenaDesktop.ViewModels;

public partial class ActivityTabViewModel : TabViewModelBase
{
    private const int DaysToShow = 30;
    private readonly IJwtTokenContainer _jwtTokenContainer;
    private readonly DayStatsService _statsService;
    private readonly StreakNotificationService _streakNotificationService;

    [ObservableProperty] private int _averageProductivity;

    [ObservableProperty] private int _currentStreak;

    [ObservableProperty] private string _endDateText = string.Empty;

    [ObservableProperty] private bool _isLoading;

    [ObservableProperty] private string _monthRangeText = string.Empty;

    [ObservableProperty] private string _startDateText = string.Empty;

    public ActivityTabViewModel(
        DayStatsService statsService,
        IJwtTokenContainer jwtTokenContainer,
        StreakNotificationService streakNotificationService) : base("Activity", IconType.ChartBar)
    {
        _statsService = statsService;
        _jwtTokenContainer = jwtTokenContainer;
        _streakNotificationService = streakNotificationService;
        _ = UpdateCellsSafe();
    }

    public ObservableCollection<ActivityDayCell> Days { get; } = [];

    public event EventHandler<int>? StreakExtended;

    private async Task UpdateCellsSafe()
    {
        IsLoading = true;
        try
        {
            await UpdateCells();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task UpdateCells()
    {
        var today = DateTime.Today;
        var startDate = today.AddDays(-(DaysToShow - 1));

        SetMonthRangeText(startDate, today);

        var culture = CultureInfo.GetCultureInfo("en-US");
        StartDateText = startDate.ToString("d MMMM yyyy", culture);
        EndDateText = today.ToString("d MMMM yyyy", culture);

        Days.Clear();

        for (var i = DaysToShow - 1; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var dayStat = await _statsService.GetByDate(DateOnly.FromDateTime(date), _jwtTokenContainer.GetUserId());

            var cell = new ActivityDayCell
            {
                Date = DateOnly.FromDateTime(date),
                ProductivityPercent = dayStat?.ProductivityPercent
            };
            Days.Add(cell);
        }

        var validDays = Days.Where(c => c.ProductivityPercent.HasValue).ToList();

        AverageProductivity = validDays.Count > 0
            ? (int)validDays.Average(d => d.ProductivityPercent.Value)
            : 0;

        var streak = 0;
        for (var i = Days.Count - 1; i >= 0; i--)
            if (Days[i].ProductivityPercent.HasValue && Days[i].ProductivityPercent.Value > 60)
            {
                streak++;
            }
            else
            {
                if (i == Days.Count - 1) continue;
                break;
            }

        CurrentStreak = streak;

        if (_streakNotificationService.ShouldCelebrate(CurrentStreak)) StreakExtended?.Invoke(this, CurrentStreak);
    }

    private void SetMonthRangeText(DateTime start, DateTime end)
    {
        var culture = CultureInfo.GetCultureInfo("en-US");
        var startMonth = culture.TextInfo.ToTitleCase(start.ToString("MMMM", culture));
        var endMonth = culture.TextInfo.ToTitleCase(end.ToString("MMMM", culture));

        MonthRangeText = startMonth == endMonth
            ? startMonth
            : $"{startMonth} - {endMonth}";
    }
}