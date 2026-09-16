namespace SirenaDesktop.Core.Models;

public class Workspace
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string WorkspaceTypeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public List<TodoItem> Todos { get; set; } = [];
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }

    public WorkspaceType? Type =>
        WorkspaceType.GetDefaultTypes().FirstOrDefault(t => t.Id == WorkspaceTypeId);
}