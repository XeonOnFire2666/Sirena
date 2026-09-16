using Microsoft.EntityFrameworkCore;
using SirenaAPI.Interfaces;
using SirenaAPI.Logic.Entities;
#pragma warning disable CS8603

namespace SirenaAPI.Infrastructure.Repository;

public class DayStatsRepositoriy(SirenaDbContext context) : IDayStatsRepository<DayStats>
{
    private readonly SirenaDbContext _context = context;

    public async Task CreateDayStats(DayStats entity, CancellationToken ct = default)
    {
        await _context.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<DayStats> GetByDateAsNoTracking(DateOnly date, Guid userId, CancellationToken ct = default) =>
        await _context.DayStats
            .AsNoTracking()
            .Include(c => c.Tags)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Date == date, ct);

    public async Task<DayStats> GetByDate(DateOnly date, Guid userId, CancellationToken ct = default) =>
        await _context.DayStats
            .Include(c => c.Tags)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Date == date, ct);

    public async Task UpdateStats(DayStats stats, CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }

    public async Task<List<DayStats>> GetAll(Guid userId, CancellationToken ct = default)
    {
        return await _context.DayStats
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .ToListAsync(ct);
    }

    public async Task<List<DayStats>> GetAllFromDesktop(Guid userId, CancellationToken ct = default)
    {
        return await _context.DayStats
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.Tags.Any(t => t.Platform == "windows"))
            .Include(c => c.Tags.Where(t => t.Platform == "windows"))
            .ToListAsync(ct);
    }

    public async Task<List<DayStats>> GetAllFromMobile(Guid userId, CancellationToken ct = default)
    {
        return await _context.DayStats
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.Tags.Any(t => t.Platform == "android"))
            .Include(c => c.Tags.Where(t => t.Platform == "android"))
            .ToListAsync(ct);
    }

    public async Task<DayStats> GetByDateFromMobile(Guid userId, DateOnly date, CancellationToken ct = default)
    {
        return await _context.DayStats
            .AsNoTracking()
            .Include(c => c.Tags.Where(t => t.Platform == "android"))
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Date == date);
    }

    public async Task<DayStats> GetByDateFromDesktop(Guid userId, DateOnly date, CancellationToken ct = default)
    {
        return await _context.DayStats
            .AsNoTracking()
            .Include(c => c.Tags.Where(t => t.Platform == "windows"))
            .Where(c => c.UserId == userId && c.Date == date)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Date == date);
    }
}
