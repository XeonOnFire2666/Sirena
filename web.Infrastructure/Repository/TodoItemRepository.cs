using Microsoft.EntityFrameworkCore;
using SirenaAPI.Interfaces;
using SirenaAPI.Logic.Entities;

namespace SirenaAPI.Infrastructure.Repository;

public class TodoItemRepository(SirenaDbContext db) : ITodoItemRepository
{
    private readonly SirenaDbContext _db = db;

    public async Task SyncForWorkspace(Guid workspaceId, List<TodoItem> incomingTodos, CancellationToken ct)
    {
        var existingTodos = await _db.TodoItems
            .Where(t => t.WorkspaceId == workspaceId)
            .ToListAsync(ct);

        var incomingById = incomingTodos.ToDictionary(t => t.Id);
        var existingById = existingTodos.ToDictionary(t => t.Id);

        foreach (var todo in incomingTodos)
        {
            if (existingById.TryGetValue(todo.Id, out var match))
            {
                match.Title = todo.Title;
                match.StartTime = todo.StartTime;
                match.EndTime = todo.EndTime;
                match.CompleteDate = todo.CompleteDate;

                match.IsCompleted = todo.IsCompleted;
                match.IsDeleted = todo.IsDeleted;
                match.UpdatedAt = todo.UpdatedAt;
            }
            else
            {
                // Явно привязываем к воркспейсу, если вдруг забыли
                todo.WorkspaceId = workspaceId;
                _db.TodoItems.Add(todo);
            }
        }

        var toRemove = existingTodos.Where(t => !incomingById.ContainsKey(t.Id)).ToList();
        foreach (var stale in toRemove)
        {
            _db.TodoItems.Remove(stale);
        }

        await _db.SaveChangesAsync(ct);
    }
}