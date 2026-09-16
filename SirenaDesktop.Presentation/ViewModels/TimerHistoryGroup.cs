using SirenaDesktop.Core.Models;

namespace SirenaDesktop.ViewModels;

public class TimerHistoryGroup
{
    public string DateLabel { get; set; } = string.Empty;
    public List<TimerSession> Sessions { get; set; } = new();
}