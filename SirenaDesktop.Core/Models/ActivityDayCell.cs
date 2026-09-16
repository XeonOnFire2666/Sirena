using CommunityToolkit.Mvvm.ComponentModel;

namespace SirenaDesktop.Core.Models;

public partial class ActivityDayCell : ObservableObject
{
    [ObservableProperty] private int? _productivityPercent;

    public DateOnly Date { get; set; }
}