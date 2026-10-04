using Microsoft.EntityFrameworkCore;
using SirenaAPI.Interfaces;
using SirenaAPI.Logic.Entities;
using SirenaAPI.Logic.Exceptions;

namespace SirenaAPI.Infrastructure.Repository;

public class WorkspaceRepository(SirenaDbContext db) : IWorkspaceRepository
{
    private readonly SirenaDbContext _db = db;

    public async Task<List<Workspace>> GetAll(Guid userId, CancellationToken ct)
    {
        return await _db.Workspaces.AsNoTracking()
            .Where(w => w.UserId == userId)
            .OrderBy(w => w.Date)
            .Include(w => w.Todos)
            .ToListAsync(ct);
    }

    public async Task Add(Workspace item, CancellationToken ct)
    {
        var exists = await _db.Workspaces.AnyAsync(w => w.Id == item.Id, ct);
        if (exists) return; 

        _db.Workspaces.Add(item);
        await _db.SaveChangesAsync(ct);
    }

    public async Task Update(Workspace item, CancellationToken ct)
    {
        var existing = await _db.Workspaces.FirstOrDefaultAsync(w => w.Id == item.Id && w.UserId == item.UserId, ct)
            ?? throw new ArgumentException("Workspace не найден");

        if (item.UpdatedAt <= existing.UpdatedAt)
            return;

        existing.WorkspaceTypeId = item.WorkspaceTypeId;
        existing.Name = item.Name;
        existing.ColorHex = item.ColorHex;
        existing.Date = item.Date;
        existing.IsDeleted = item.IsDeleted;
        existing.UpdatedAt = item.UpdatedAt;

        await _db.SaveChangesAsync(ct);
    }

    public async Task Delete(Guid id, CancellationToken ct)
    {
        var existing = await _db.Workspaces.FirstOrDefaultAsync(w => w.Id == id, ct)
            ?? throw new ArgumentException("Workspace не найден");

        await _db.Workspaces.Where(w => w.Id == id).ExecuteDeleteAsync(ct);
    }

    public async Task UpsertAsync(Workspace incoming, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var incomingTodoIds = incoming.Todos.Select(t => t.Id).ToList();

        if (incomingTodoIds.Count > 0)
        {
            var ownedElsewhere = await _db.TodoItems
                .AnyAsync(t => incomingTodoIds.Contains(t.Id) && t.WorkspaceId != incoming.Id, ct);

            if (ownedElsewhere)
                throw new WorkspaceConflictException("Todo id already belongs to another workspace");
        }

        var existing = await _db.Workspaces
            .Include(w => w.Todos)
            .FirstOrDefaultAsync(w => w.Id == incoming.Id, ct);

        if (existing is null)
        {
            await _db.TodoItems
                .Where(t => t.WorkspaceId == incoming.Id)
                .ExecuteDeleteAsync(ct);

            _db.Workspaces.Add(incoming);
        }
        else
        {
            if (existing.UserId != incoming.UserId)
                throw new UnauthorizedAccessException("Workspace belongs to another user");

            Merge(existing, incoming);
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private static void Merge(Workspace existing, Workspace incoming)
    {
        var newest = existing.UpdatedAt;

        if (incoming.UpdatedAt >= existing.UpdatedAt)
        {
            existing.WorkspaceTypeId = incoming.WorkspaceTypeId;
            existing.Name = incoming.Name;
            existing.ColorHex = incoming.ColorHex;
            existing.Date = incoming.Date;
            existing.IsDeleted = incoming.IsDeleted;
            existing.UpdatedAt = incoming.UpdatedAt;
            newest = incoming.UpdatedAt;
        }

        var currentById = existing.Todos.ToDictionary(t => t.Id);

        foreach (var inc in incoming.Todos)
        {
            if (!currentById.TryGetValue(inc.Id, out var cur))
            {
                existing.Todos.Add(TodoItem.Create(
                    inc.Id,
                    existing.Id,
                    inc.Title,
                    inc.StartTime,
                    inc.EndTime,
                    inc.CompleteDate,
                    inc.IsCompleted,
                    inc.IsDeleted,
                    inc.UpdatedAt));

                if (inc.UpdatedAt > newest) newest = inc.UpdatedAt;
                continue;
            }

            if (inc.UpdatedAt < cur.UpdatedAt) continue;

            cur.Title = inc.Title;
            cur.StartTime = inc.StartTime;
            cur.EndTime = inc.EndTime;
            cur.CompleteDate = inc.CompleteDate;
            cur.IsCompleted = inc.IsCompleted;
            cur.IsDeleted = inc.IsDeleted;
            cur.UpdatedAt = inc.UpdatedAt;

            if (inc.UpdatedAt > newest) newest = inc.UpdatedAt;
        }

        existing.UpdatedAt = newest;
    }
}