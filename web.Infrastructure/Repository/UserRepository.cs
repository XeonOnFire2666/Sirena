using Microsoft.EntityFrameworkCore;
using SirenaAPI.Infrastructure;
using SirenaAPI.Interfaces;
using SirenaAPI.Models;

namespace SirenaAPI.Repository;

public class UserRepository( 
    SirenaDbContext dbContext) : IUserRepository<User>
{
    private readonly SirenaDbContext _dbContext = dbContext;
    
    public async Task<User?> GetById(Guid id, CancellationToken ct = default) => await _dbContext.Users
        .AsNoTracking()    
        .FirstOrDefaultAsync(c => c.Id == id, ct);
    
    public async Task<User?> GetByName(string name, CancellationToken ct = default) => await _dbContext.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(c => c.Name == name, ct);

    public async Task<User?> GetByEmail(string email, CancellationToken ct = default)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Email == email)
            .FirstOrDefaultAsync(ct);
    }

    public async Task Add(User user, CancellationToken ct = default)
    {
        await _dbContext.Users.AddAsync(user, ct);
        await _dbContext.SaveChangesAsync(ct);     
    }
    public async Task Delete(Guid id, CancellationToken ct = default)
    {
        await _dbContext.Users
            .Where(c => c.Id == id)
            .ExecuteDeleteAsync(ct);
    }

    public async Task Update(User user, CancellationToken ct = default)
    {
        await _dbContext.Users
            .Where(c => c.Id == user.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Name, user.Name)
                .SetProperty(c => c.Email, user.Email), ct);
    }

    public async Task<User?> GetByGoogleId(string googleSub, CancellationToken ct = default)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Where(c => c.GoogleId == googleSub)
            .FirstOrDefaultAsync(ct);

    }
}