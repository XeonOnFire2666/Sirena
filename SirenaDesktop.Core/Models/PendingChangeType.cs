namespace SirenaDesktop.Core.Models;

public enum PendingChangeType
{
    Create,
    Update
}

public enum PendingEntityKind
{
    Todo,
    Workspace
}

public class PendingChange
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EntityId { get; set; }
    public PendingEntityKind EntityKind { get; set; }
    public PendingChangeType Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string PayloadJson { get; set; } = string.Empty;
}