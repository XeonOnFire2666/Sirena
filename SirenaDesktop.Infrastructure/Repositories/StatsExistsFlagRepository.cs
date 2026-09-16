using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SirenaDesktop.Core.Models;

namespace SirenaDesktop.Repositories;

public class StatsExistsFlagRepository(IDbContextFactory<SirenaDbContext> dbContextFactory) : IStatsExistsFlagRepository
{
    private readonly IDbContextFactory<SirenaDbContext> _contextFactory = dbContextFactory;

    public async Task<PendingChanges?> GetByDate(Guid userId, DateOnly day, CancellationToken ct = default)
    {
        using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.StatsExistsFlag
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId && c.DateOnly == day, ct);
    }

    public async Task Add(DateOnly day, Guid userId, bool flag = false, CancellationToken ct = default)
    {
        using var context = await _contextFactory.CreateDbContextAsync(ct);

        try
        {
            await context.StatsExistsFlag
                .AddAsync(PendingChanges.Create(flag, userId, day), ct);

            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            await context.StatsExistsFlag
                .Where(c => c.UserId == userId && c.DateOnly == day)
                .ExecuteUpdateAsync(c => c.SetProperty(x => x.IsExists, flag), ct);
        }
    }


    public async Task Update(bool flag, DateOnly day, Guid userId, CancellationToken ct = default)
    {
        using var context = await _contextFactory.CreateDbContextAsync(ct);

        await context.StatsExistsFlag
            .Where(c => c.UserId == userId && c.DateOnly == day)
            .ExecuteUpdateAsync(c => c
                .SetProperty(c => c.IsExists, flag));
    }

    public async Task Delete(DateOnly day, Guid userId, CancellationToken ct = default)
    {
        using var context = await _contextFactory.CreateDbContextAsync(ct);

        await context.StatsExistsFlag
            .Where(c => c.UserId == userId && c.DateOnly == day)
            .ExecuteDeleteAsync(ct);
    }
}