using CommunityToolkit.Mvvm.ComponentModel;

namespace SirenaDesktop.Core.Models;

public partial class WeekDayItem : ObservableObject
{
    public DateOnly Date { get; init; }
    public string DayOfWeekShort { get; init; } = string.Empty;    
    public string DayNumber { get; init; } = string.Empty;  
    public bool IsWeekend { get; init; }       

    [ObservableProperty] public partial bool IsSelected { get; set; }
}