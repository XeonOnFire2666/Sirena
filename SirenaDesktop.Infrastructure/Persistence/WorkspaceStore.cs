using Microsoft.EntityFrameworkCore;
using SirenaDesktop.Core.Models;
using SirenaDesktop.Infrastructure.ExternalServices;
using SirenaDesktop.Infrastructure.Services;
using SirenaDesktop.Repositories;
using System.Text.Json;

namespace SirenaDesktop.Infrastructure.Persistence;

public sealed class WorkspaceStore(IDbContextFactory<SirenaDbContext> factory)
{
    public async Task SaveLocalAsync(Workspace ws, CancellationToken ct = default)
    {
        ws.UpdatedAt = DateTime.UtcNow;

        await using var db = await factory.CreateDbContextAsync(ct);
        await UpsertGraphAsync(db, ws, ct);

        var payload = JsonSerializer.Serialize(WorkspaceSyncMapper.ToUpsert(ws), JsonSerializerOptions.Web);
        var outbox = await db.SyncOutbox.FindAsync(new object[] { SyncEntityKind.Workspace, ws.Id }, ct);

        if (outbox is null)
        {
            outbox = new SyncOutboxItem { EntityKind = SyncEntityKind.Workspace, EntityId = ws.Id };
            db.SyncOutbox.Add(outbox);
        }

        outbox.Version++;
        outbox.PayloadJson = payload;
        outbox.FailedAttempts = 0;
        outbox.NextAttemptAtUtc = DateTime.UtcNow;
        outbox.LastError = null;

        await db.SaveChangesAsync(ct);
    }

    public async Task SaveServerAsync(Workspace server, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await UpsertGraphAsync(db, server, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<Workspace>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Workspaces.AsNoTracking().Include(w => w.Todos).ToListAsync(ct);
    }

    public async Task<HashSet<Guid>> GetPendingIdsAsync(SyncEntityKind kind, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var ids = await db.SyncOutbox
            .Where(o => o.EntityKind == kind)
            .Select(o => o.EntityId)
            .ToListAsync(ct);
        return ids.ToHashSet();
    }

    public async Task<List<SyncOutboxItem>> GetDueAsync(DateTime nowUtc, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.SyncOutbox.AsNoTracking()
            .Where(o => o.NextAttemptAtUtc <= nowUtc)
            .OrderBy(o => o.NextAttemptAtUtc)
            .ToListAsync(ct);
    }

    public async Task AckAsync(SyncEntityKind kind, Guid id, long version, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await db.SyncOutbox
            .Where(o => o.EntityKind == kind && o.EntityId == id && o.Version == version)
            .ExecuteDeleteAsync(ct);
    }

    public async Task MarkFailedAsync(SyncOutboxItem item, string error, CancellationToken ct = default)
    {
        var delay = TimeSpan.FromSeconds(Math.Min(300, Math.Pow(2, item.FailedAttempts + 1)));

        await using var db = await factory.CreateDbContextAsync(ct);
        await db.SyncOutbox
            .Where(o => o.EntityKind == item.EntityKind && o.EntityId == item.EntityId && o.Version == item.Version)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.FailedAttempts, o => o.FailedAttempts + 1)
                .SetProperty(o => o.NextAttemptAtUtc, DateTime.UtcNow.Add(delay))
                .SetProperty(o => o.LastError, error), ct);
    }

    public async Task ClearAllWorkspacesAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await db.SyncOutbox
            .Where(o => o.EntityKind == SyncEntityKind.Workspace)
            .ExecuteDeleteAsync(ct);
        await db.Set<TodoItem>().ExecuteDeleteAsync(ct);
        await db.Workspaces.ExecuteDeleteAsync(ct);
    }

    private static async Task UpsertGraphAsync(SirenaDbContext db, Workspace ws, CancellationToken ct)
    {
        var incomingIds = ws.Todos.Select(t => t.Id).ToList();

        var knownTodos = await db.Set<TodoItem>()
            .Where(t => incomingIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, ct);

        var workspace = await db.Workspaces
            .Include(w => w.Todos)
            .FirstOrDefaultAsync(w => w.Id == ws.Id, ct);

        if (workspace is null)
        {
            workspace = new Workspace { Id = ws.Id };
            db.Workspaces.Add(workspace);
        }

        db.Entry(workspace).CurrentValues.SetValues(ws);

        foreach (var incoming in ws.Todos)
        {
            if (knownTodos.TryGetValue(incoming.Id, out var row))
            {
                db.Entry(row).CurrentValues.SetValues(incoming);
                row.WorkspaceId = workspace.Id;
            }
            else
            {
                db.Set<TodoItem>().Add(new TodoItem
                {
                    Id = incoming.Id,
                    WorkspaceId = workspace.Id,
                    Title = incoming.Title,
                    StartTime = incoming.StartTime,
                    EndTime = incoming.EndTime,
                    CompleteDate = incoming.CompleteDate,
                    IsCompleted = incoming.IsCompleted,
                    IsDeleted = incoming.IsDeleted,
                    UpdatedAt = incoming.UpdatedAt
                });
            }
        }

        var incomingSet = incomingIds.ToHashSet();
        var removed = workspace.Todos.Where(t => !incomingSet.Contains(t.Id)).ToList();
        db.Set<TodoItem>().RemoveRange(removed);
    }
}