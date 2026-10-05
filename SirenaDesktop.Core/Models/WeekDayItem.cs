using CommunityToolkit.Mvvm.ComponentModel;

namespace SirenaDesktop.Core.Models;

public partial class WeekDayItem : ObservableObject
{
    public DateOnly Date { get; set; }
    public string DayOfWeekShort { get; set; } = string.Empty;
    public string DayNumber { get; set; } = string.Empty;
    public bool IsWeekend { get; set; }

    [ObservableProperty] public partial bool IsSelected { get; set; }
    [ObservableProperty] public partial bool IsToday { get; set; }
    [ObservableProperty] public partial bool HasTasks { get; set; }
}