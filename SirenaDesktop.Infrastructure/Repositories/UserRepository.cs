using Microsoft.EntityFrameworkCore;
using SirenaDesktop.Core.Models;

namespace SirenaDesktop.Repositories;

public class UserRepository(
    IDbContextFactory<SirenaDbContext> dbContextFactory) : IUserRepository
{
    private readonly IDbContextFactory<SirenaDbContext> _contextFactory = dbContextFactory;

    public async Task<User?> GetById(Guid id, CancellationToken ct = default)
    {
        using var dbContext = await _contextFactory.CreateDbContextAsync(ct);
        return await dbContext.User
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<User?> Get(CancellationToken ct = default)
    {
        using var dbContext = await _contextFactory.CreateDbContextAsync(ct);
        return await dbContext.User
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);
    }

    public async Task<User?> GetByEmail(string email, CancellationToken ct = default)
    {
        using var dbContext = await _contextFactory.CreateDbContextAsync(ct);
        return await dbContext.User
            .AsNoTracking()
            .Where(u => u.Email == email)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<User?> GetByGoogleId(string googleSub, CancellationToken ct = default)
    {
        using var dbContext = await _contextFactory.CreateDbContextAsync(ct);
        return await dbContext.User
            .AsNoTracking()
            .Where(c => c.GoogleId == googleSub)
            .FirstOrDefaultAsync(ct);
    }

    public async Task Add(User user, CancellationToken ct = default)
    {
        using var dbContext = await _contextFactory.CreateDbContextAsync(ct);
        await dbContext.User.AddAsync(user, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task Delete(CancellationToken ct = default)
    {
        using var dbContext = await _contextFactory.CreateDbContextAsync(ct);
        await dbContext.User.ExecuteDeleteAsync(ct);
    }

    public async Task Update(User user, CancellationToken ct = default)
    {
        using var dbContext = await _contextFactory.CreateDbContextAsync(ct);
        await dbContext.User
            .Where(c => c.Email == user.Email)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Name, user.Name)
                .SetProperty(c => c.Email, user.Email), ct);
    }
}