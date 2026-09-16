using Microsoft.EntityFrameworkCore;
using SirenaDesktop.Core.Models;

namespace SirenaDesktop.Repositories;

public class AppTagMapRepository(IDbContextFactory<SirenaDbContext> dbContextFactory)
{
    private readonly IDbContextFactory<SirenaDbContext> _contextFactory = dbContextFactory;

    public async Task<AppTagMap> GetOrCreate(Guid userId, string trackKey, Func<string> colorFactory,
        CancellationToken ct = default)
    {
        using var context = await _contextFactory.CreateDbContextAsync(ct);

        var existing = await context.AppTagMaps
            .FirstOrDefaultAsync(m => m.UserId == userId && m.TrackKey == trackKey, ct);

        if (existing != null) return existing;

        var created = AppTagMap.Create(userId, trackKey, colorFactory());
        await context.AddAsync(created, ct);
        await context.SaveChangesAsync(ct);

        return created;
    }

    public async Task<Dictionary<string, AppTagMap>> GetAllForUser(Guid userId, CancellationToken ct = default)
    {
        using var context = await _contextFactory.CreateDbContextAsync(ct);

        var maps = await context.AppTagMaps
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .ToListAsync(ct);

        return maps.ToDictionary(m => m.TrackKey);
    }
}