using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SirenaDesktop.Core.Models;

namespace SirenaDesktop.Repositories;

public class WorkspaceRepository(IDbContextFactory<SirenaDbContext> dbContextFactory) : IWorkspaceRepository
{
    private readonly IDbContextFactory<SirenaDbContext> _contextFactory = dbContextFactory;

    public async Task<List<Workspace>> GetAll(CancellationToken ct = default)
    {
        using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.Workspaces
            .AsNoTracking()
            .Where(w => !w.IsDeleted)
            .Include(w => w.Todos)
            .ToListAsync(ct);
    }

    public async Task<List<Workspace>> GetByDate(DateOnly date, CancellationToken ct = default)
    {
        using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.Workspaces.AsNoTracking()
            .Where(w => !w.IsDeleted && w.Date == date)
            .Include(w => w.Todos)
            .ToListAsync(ct);
    }

    public async Task<Workspace?> GetById(Guid id, CancellationToken ct = default)
    {
        using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.Workspaces.AsNoTracking().FirstOrDefaultAsync(w => w.Id == id, ct);
    }

    public async Task Add(Workspace ws, CancellationToken ct = default)
    {
        using var db = await _contextFactory.CreateDbContextAsync(ct);
        await db.Workspaces.AddAsync(ws, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task Update(Workspace ws, CancellationToken ct = default)
    {
        using var db = await _contextFactory.CreateDbContextAsync(ct);
        var existing = await db.Workspaces
            .Include(w => w.Todos)
            .FirstOrDefaultAsync(w => w.Id == ws.Id, ct);

        if (existing is null) return;

        existing.WorkspaceTypeId = ws.WorkspaceTypeId;
        existing.Name = ws.Name;
        existing.ColorHex = ws.ColorHex;
        existing.Date = ws.Date;
        existing.IsDeleted = ws.IsDeleted;
        existing.UpdatedAt = ws.UpdatedAt;

        MergeTodos(db, existing, ws.Todos);

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteWorkspaces(CancellationToken ct = default)
    {
        using var db = await _contextFactory.CreateDbContextAsync(ct);
        await db.Workspaces.ExecuteDeleteAsync(ct);
    }

    public async Task DeleteWorkspace(Guid id, CancellationToken ct = default)
    {
        using var db = await _contextFactory.CreateDbContextAsync(ct);

        await db.Workspaces
            .Where(w => w.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(w => w.IsDeleted, true)
                .SetProperty(w => w.UpdatedAt, DateTime.UtcNow), ct);
    }

    public async Task Upsert(Workspace ws, CancellationToken ct = default)
    {
        using var db = await _contextFactory.CreateDbContextAsync(ct);
        var existing = await db.Workspaces
            .Include(w => w.Todos)
            .FirstOrDefaultAsync(w => w.Id == ws.Id, ct);

        if (existing is null)
        {
            await db.Workspaces.AddAsync(ws, ct);
        }
        else
        {
            existing.WorkspaceTypeId = ws.WorkspaceTypeId;
            existing.Name = ws.Name;
            existing.ColorHex = ws.ColorHex;
            existing.Date = ws.Date;
            existing.IsDeleted = ws.IsDeleted;
            existing.UpdatedAt = ws.UpdatedAt;

            MergeTodos(db, existing, ws.Todos);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task QueuePendingChange(Workspace ws, PendingChangeType type, CancellationToken ct = default)
    {
        using var db = await _contextFactory.CreateDbContextAsync(ct);
        db.PendingChanges.Add(new PendingChange
        {
            EntityId = ws.Id,
            EntityKind = PendingEntityKind.Workspace,
            Type = type,
            PayloadJson = JsonSerializer.Serialize(ws)
        });
        await db.SaveChangesAsync(ct);
    }

    private static void MergeTodos(SirenaDbContext db, Workspace existing, List<TodoItem> incoming)
    {
        var existingById = existing.Todos.ToDictionary(t => t.Id);

        foreach (var todo in incoming)
        {
            if (existingById.TryGetValue(todo.Id, out var match))
            {
                if (todo.UpdatedAt < match.UpdatedAt) continue;

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
                var newTodo = new TodoItem
                {
                    Id = todo.Id,
                    Title = todo.Title,
                    StartTime = todo.StartTime,
                    EndTime = todo.EndTime,
                    CompleteDate = todo.CompleteDate,
                    IsCompleted = todo.IsCompleted,
                    IsDeleted = todo.IsDeleted,
                    UpdatedAt = todo.UpdatedAt,
                    WorkspaceId = existing.Id
                };

                existing.Todos.Add(newTodo);
                db.Entry(newTodo).State = EntityState.Added;
            }
        }
    }
}