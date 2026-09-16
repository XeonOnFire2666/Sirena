using Microsoft.EntityFrameworkCore;
using SirenaDesktop.Core.Models;

namespace SirenaDesktop.Repositories;

public class HydrationFlagRepository(IDbContextFactory<SirenaDbContext> dbContextFactory) : IHydrationFlagRepository
{
    private readonly IDbContextFactory<SirenaDbContext> _contextFactory = dbContextFactory;

    public async Task<bool> IsHydrated(Guid userId, CancellationToken ct = default)
    {
        using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.HydratedUsers.AnyAsync(h => h.UserId == userId, ct);
    }

    public async Task MarkHydrated(Guid userId, CancellationToken ct = default)
    {
        using var context = await _contextFactory.CreateDbContextAsync(ct);

        var already = await context.HydratedUsers.AnyAsync(h => h.UserId == userId, ct);
        if (already) return;

        await context.HydratedUsers.AddAsync(new HydratedUser { UserId = userId }, ct);
        await context.SaveChangesAsync(ct);
    }
}