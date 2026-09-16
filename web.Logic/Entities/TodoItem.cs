using System;

namespace SirenaAPI.Logic.Entities;

public class TodoItem
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public string Title { get; set; } = string.Empty;

    // Заменили DueDate на 3 новых свойства
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public DateTime? CompleteDate { get; set; }

    public bool IsCompleted { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime UpdatedAt { get; set; }
        
    public static TodoItem Create(Guid id, Guid workspaceId, string title, DateTime? startTime, DateTime? endTime, DateTime? completeDate, bool isCompleted, bool isDeleted, DateTime updatedAt)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty");

        return new TodoItem
        {
            Id = id,
            WorkspaceId = workspaceId,
            Title = title,
            StartTime = startTime.HasValue ? DateTime.SpecifyKind(startTime.Value, DateTimeKind.Utc) : null,
            EndTime = endTime.HasValue ? DateTime.SpecifyKind(endTime.Value, DateTimeKind.Utc) : null,
            CompleteDate = completeDate.HasValue ? DateTime.SpecifyKind(completeDate.Value, DateTimeKind.Utc) : null,
            IsCompleted = isCompleted,
            IsDeleted = isDeleted,
            UpdatedAt = updatedAt == default ? DateTime.UtcNow : DateTime.SpecifyKind(updatedAt, DateTimeKind.Utc)
        };
    }
}