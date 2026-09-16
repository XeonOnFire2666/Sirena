namespace SirenaDesktop.Core.Models;

public enum TimerSessionKind
{
    Work,
    Break
}

public enum TimerSessionResult
{
    InProgress,
    Completed,
    Cancelled
}

public class TimerPauseInterval
{
    public DateTime PausedAt { get; set; }
    public DateTime? ResumedAt { get; set; }

    public TimeSpan Duration => (ResumedAt ?? DateTime.Now) - PausedAt;
}

public class TimerSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public TimerSessionKind Kind { get; set; }
    public TimeSpan PlannedDuration { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public TimerSessionResult Result { get; set; } = TimerSessionResult.InProgress;
    public List<TimerPauseInterval> Pauses { get; set; } = new();

    public TimeSpan TotalPausedTime
    {
        get
        {
            var total = TimeSpan.Zero;
            foreach (var p in Pauses)
                total += p.Duration;
            return total;
        }
    }

    public string KindLabel => Kind == TimerSessionKind.Work ? "Работа" : "Отдых";
}