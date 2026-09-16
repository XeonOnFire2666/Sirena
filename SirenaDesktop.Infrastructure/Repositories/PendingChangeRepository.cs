using Microsoft.EntityFrameworkCore;
using SirenaDesktop.Core.Models;

namespace SirenaDesktop.Repositories;

public class PendingChangeRepository(IDbContextFactory<SirenaDbContext> dbContextFactory) : IPendingChangeRepository
{
    private readonly IDbContextFactory<SirenaDbContext> _contextFactory = dbContextFactory;

    public async Task<List<PendingChange>> GetAll(CancellationToken ct = default)
    {
        using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.PendingChanges.AsNoTracking()
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task Remove(Guid pendingChangeId, CancellationToken ct = default)
    {
        using var db = await _contextFactory.CreateDbContextAsync(ct);
        await db.PendingChanges.Where(p => p.Id == pendingChangeId).ExecuteDeleteAsync(ct);
    }

    public async Task RemoveAll(CancellationToken ct = default)
    {
        using var db = await _contextFactory.CreateDbContextAsync(ct);
        await db.PendingChanges.ExecuteDeleteAsync();
    }
}