using Microsoft.EntityFrameworkCore;
using SirenaDesktop.Core.Models;

namespace SirenaDesktop.Repositories;

public class DayStatsRepository(IDbContextFactory<SirenaDbContext> dbContextFactory) : IDayStatsRepository
{
    private readonly IDbContextFactory<SirenaDbContext> _contextFactory = dbContextFactory;

    public async Task CreateDayStats(DayStats entity, CancellationToken ct = default)
    {
        using var context = await _contextFactory.CreateDbContextAsync(ct);

        var existing = await context.DayStats
            .Include(s => s.Tags)
            .FirstOrDefaultAsync(c => c.UserId == entity.UserId && c.Date == entity.Date, ct);

        if (existing == null)
        {
            await context.AddAsync(entity, ct);
        }
        else
        {
            var existingByTagId = existing.Tags.ToDictionary(t => t.TagId);

            foreach (var tag in entity.Tags)
                if (existingByTagId.TryGetValue(tag.TagId, out var match))
                {
                    match.RecordedMinutes = tag.RecordedMinutes;
                    match.ColorHex = tag.ColorHex;
                    match.AppTitle = tag.AppTitle;
                    match.Domain = tag.Domain;
                    match.DateTime = tag.DateTime;
                }
                else
                {
                    existing.Tags.Add(tag);
                }
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task<DayStats?> GetByDateAsNoTracking(DateOnly date, Guid userId, CancellationToken ct = default)
    {
        using var context = await _contextFactory.CreateDbContextAsync(ct);

        return await context.DayStats
            .AsNoTracking()
            .Include(s => s.Tags)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Date == date, ct);
    }

    public async Task<DayStats?> GetByDateMobile(DateOnly date, Guid userId, CancellationToken ct = default)
    {
        using var context = await _contextFactory.CreateDbContextAsync(ct);

        var dayStat = await context.DayStats
            .Include(s => s.Tags)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Date == date, ct);

        if (dayStat != null)
            dayStat.Tags = dayStat.Tags
                .Where(c => !string.IsNullOrEmpty(c.Platform) &&
                            c.Platform.Equals("android", StringComparison.OrdinalIgnoreCase))
                .ToList();

        return dayStat;
    }

    public async Task<DayStats?> GetByDate(DateOnly date, Guid userId, CancellationToken ct = default)
    {
        using var context = await _contextFactory.CreateDbContextAsync(ct);

        return await context.DayStats
            .Include(s => s.Tags)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Date == date, ct);
    }

    public async Task UpsertTag(Guid userId, DateOnly date, ActionTag tag, CancellationToken ct = default)
    {
        using var context = await _contextFactory.CreateDbContextAsync(ct);

        var stats = await context.DayStats
            .Include(s => s.Tags)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Date == date, ct);

        if (stats == null)
        {
            var newDayStats = DayStats.Create(Guid.NewGuid(), userId, date, new List<ActionTag> { tag });
            await context.AddAsync(newDayStats, ct);
            await context.SaveChangesAsync(ct);
            return;
        }

        var existing = stats.Tags.FirstOrDefault(t => t.TagId == tag.TagId);

        if (existing != null)
        {
            existing.RecordedMinutes = tag.RecordedMinutes;
            existing.ColorHex = tag.ColorHex;
            existing.AppTitle = tag.AppTitle;
            existing.Domain = tag.Domain;
            existing.DateTime = tag.DateTime;
        }
        else
        {
            stats.Tags.Add(tag);
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task SetProductivityPercent(Guid userId, DateOnly date, int percent, CancellationToken ct = default)
    {
        using var context = await _contextFactory.CreateDbContextAsync(ct);

        var stats = await context.DayStats
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Date == date, ct);

        if (stats == null)
        {
            var newDayStats = DayStats.Create(Guid.NewGuid(), userId, date, new List<ActionTag>());
            newDayStats.ProductivityPercent = percent;
            await context.AddAsync(newDayStats, ct);
        }
        else
        {
            stats.ProductivityPercent = percent;
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task IncrementStudyUnits(Guid userId, DateOnly date, double studyUnitsDelta,
        CancellationToken ct = default)
    {
        using var context = await _contextFactory.CreateDbContextAsync(ct);

        var stats = await context.DayStats
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Date == date, ct);

        if (stats == null)
        {
            var newDayStats = DayStats.Create(Guid.NewGuid(), userId, date, new List<ActionTag>());
            newDayStats.StudyUnits = studyUnitsDelta;
            await context.AddAsync(newDayStats, ct);
        }
        else
        {
            stats.StudyUnits += studyUnitsDelta;
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task IncrementGitUnits(Guid userId, DateOnly date, double gitUnitsDelta,
        IEnumerable<string> newlyScoredShas, CancellationToken ct = default)
    {
        using var context = await _contextFactory.CreateDbContextAsync(ct);

        var shas = newlyScoredShas as ICollection<string> ?? newlyScoredShas.ToList();

        var stats = await context.DayStats
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Date == date, ct);

        if (stats == null)
        {
            var newDayStats = DayStats.Create(Guid.NewGuid(), userId, date, new List<ActionTag>());
            newDayStats.GitUnits = gitUnitsDelta;
            newDayStats.ScoredCommitShas = new HashSet<string>(shas);
            await context.AddAsync(newDayStats, ct);
        }
        else
        {
            stats.GitUnits += gitUnitsDelta;
            stats.ScoredCommitShas = new HashSet<string>(stats.ScoredCommitShas.Union(shas));
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task<bool> CheckStats(DateOnly day, Guid userId, CancellationToken ct = default)
    {
        using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.DayStats.AnyAsync(c => c.Date == day && c.UserId == userId);
    }

    public async Task<bool> HasAnyData(Guid userId, CancellationToken ct = default)
    {
        using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.DayStats.AnyAsync(c => c.UserId == userId, ct);
    }
}