using SirenaDesktop.Core.Models;
using SirenaDesktop.Repositories;

namespace SirenaDesktop.Services;

public class DayStatsService(IDayStatsRepository repository)
{
    private readonly IDayStatsRepository _repository = repository;

    public async Task<DayStats> GetByDateMobile(DateOnly? date, Guid? userId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(date);
        ArgumentNullException.ThrowIfNull(userId);

        if (userId.Value == Guid.Empty || date.Value.Year < 2025)
            throw new ArgumentException("Real Argument Exception by invalid data");


        return await _repository.GetByDateMobile(date.Value, userId.Value, ct);
    }

    public async Task<DayStats> GetByDate(DateOnly? date, Guid? userId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(date);
        ArgumentNullException.ThrowIfNull(userId);

        if (userId.Value == Guid.Empty || date.Value.Year < 2025)
            throw new ArgumentException("Real Argument Exception by invalid data");


        return await _repository.GetByDateAsNoTracking(date.Value, userId.Value, ct);
    }

    public async Task CreateDayStats(DayStats stats, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stats);
        ArgumentNullException.ThrowIfNull(stats.Tags, nameof(stats.Tags));

        if (stats.Tags.Count == 0) throw new ArgumentException("List cannot be with zero items");

        await _repository.CreateDayStats(stats, ct);
    }

    public async Task UpsertTag(Guid userId, DateOnly day, ActionTag tag, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(tag);
        if (userId == Guid.Empty) throw new ArgumentException("Guid.Empty data");

        await _repository.UpsertTag(userId, day, tag, ct);
    }

    public async Task UpdateStats(List<ActionTag> tags, Guid userId, DateOnly day, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(tags);
        if (userId == Guid.Empty) throw new ArgumentException("Guid.Empty data");
        if (tags.Count == 0) return;

        foreach (var tag in tags) await _repository.UpsertTag(userId, day, tag, ct);
    }

    public async Task<bool> IsExist(DateOnly day, Guid userId)
    {
        return await _repository.CheckStats(day, userId);
    }

    public async Task<bool> HasAnyData(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty) return false;
        return await _repository.HasAnyData(userId, ct);
    }

    public async Task AddDownloadData(List<DayStats> dtos, CancellationToken ct = default)
    {
        if (dtos == null || dtos.Count == 0) return;

        foreach (var stat in dtos) await _repository.CreateDayStats(stat, ct);
    }

    public async Task IncrementStudyUnits(Guid userId, DateOnly day, double studyUnitsDelta,
        CancellationToken ct = default)
    {
        if (userId == Guid.Empty) throw new ArgumentException("Guid.Empty data");
        if (studyUnitsDelta == 0) return;

        await _repository.IncrementStudyUnits(userId, day, studyUnitsDelta, ct);
    }

    public async Task IncrementGitUnits(Guid userId, DateOnly day, double gitUnitsDelta,
        IEnumerable<string> newlyScoredShas, CancellationToken ct = default)
    {
        if (userId == Guid.Empty) throw new ArgumentException("Guid.Empty data");

        var shas = newlyScoredShas?.ToList() ?? [];
        if (gitUnitsDelta == 0 && shas.Count == 0) return;

        await _repository.IncrementGitUnits(userId, day, gitUnitsDelta, shas, ct);
    }

    public async Task SetProductivityPercent(Guid userId, DateOnly day, int percent, CancellationToken ct = default)
    {
        if (userId == Guid.Empty) throw new ArgumentException("Guid.Empty data");

        await _repository.SetProductivityPercent(userId, day, percent, ct);
    }
}