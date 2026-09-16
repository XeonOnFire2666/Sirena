using SirenaAPI.Logic.Entities;

public class Workspace
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string WorkspaceTypeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public List<TodoItem> Todos { get; set; } = [];
    public bool IsDeleted { get; set; }
    public DateTime UpdatedAt { get; set; }
    private Workspace() { }
    public static Workspace Create(Guid id, Guid userId, string workspaceTypeId, string name, string colorHex, DateOnly date, List<TodoItem> todos, bool isDeleted, DateTime updatedAt)
    {
        if (string.IsNullOrWhiteSpace(workspaceTypeId))
            throw new ArgumentException("WorkspaceTypeId cannot be empty");
        return new Workspace
        {
            Id = id,
            UserId = userId,
            WorkspaceTypeId = workspaceTypeId,
            Name = string.IsNullOrWhiteSpace(name) ? workspaceTypeId : name.Trim(),
            ColorHex = string.IsNullOrWhiteSpace(colorHex) ? "#8AB4F8" : colorHex,
            Date = date,
            Todos = todos ?? [],
            IsDeleted = isDeleted,
            UpdatedAt = updatedAt == default ? DateTime.UtcNow : DateTime.SpecifyKind(updatedAt, DateTimeKind.Utc)
        };
    }
}