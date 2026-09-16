namespace SirenaDesktop.Core.Models;

public class TodoItem
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public DateTime? CompleteDate { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}