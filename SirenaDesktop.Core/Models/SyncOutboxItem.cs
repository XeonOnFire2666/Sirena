namespace SirenaDesktop.Core.Models;

public enum SyncEntityKind { Workspace, DayStats }

public class SyncOutboxItem
{
    public SyncEntityKind EntityKind { get; set; }
    public Guid EntityId { get; set; }
    public long Version { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public int FailedAttempts { get; set; }
    public DateTime NextAttemptAtUtc { get; set; } = DateTime.UtcNow;
    public string? LastError { get; set; }
}