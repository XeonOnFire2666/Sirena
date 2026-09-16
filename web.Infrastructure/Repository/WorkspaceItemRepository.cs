using Microsoft.EntityFrameworkCore;
using SirenaAPI.Interfaces;
using SirenaAPI.Logic.Entities;

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
}