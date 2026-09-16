using Microsoft.EntityFrameworkCore;
using SirenaAPI.Infrastructure;
using SirenaAPI.Interfaces;
using SirenaAPI.Logic.Entities;
#pragma warning disable CS8603

namespace SirenaAPI.Adapters.Repository;

public class RefreshTokensRepository(
    SirenaDbContext dbContext) : ITokenRepository<RefreshToken>
{
    private readonly SirenaDbContext _dbContext = dbContext;

    public async Task Add(RefreshToken entity, CancellationToken ct = default)
    {
        await _dbContext.RefreshTokens
            .AddAsync(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task Delete(Guid id, CancellationToken ct = default)
    {
        await _dbContext.RefreshTokens
            .Where(c => c.TokenNahuy == id)
            .ExecuteDeleteAsync();
    }

    public async Task<RefreshToken> GetById(Guid id)
    {
        if (id == Guid.Empty) return null;

        return await _dbContext.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenNahuy == id);
    }

    public async Task<int> Update(Guid oldRefreshToken, Guid newRefreshToken, CancellationToken ct = default)
{
    return await _dbContext.RefreshTokens
        .Where(t => t.TokenNahuy == oldRefreshToken)
        .ExecuteUpdateAsync(c => c.SetProperty(
            t => t.TokenNahuy, newRefreshToken
        ), ct);
}
}
