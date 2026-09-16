using SirenaAPI.Interfaces;
using SirenaAPI.Logic.Entities;

namespace SirenaAPI.Application.Services;

public record CreateDayStatsDto(
    List<ActionTag> Tags,
    Guid UserId,
    DateOnly Day,
    double StudyUnits = 0,
    double GitUnits = 0,
    string? LastScoredCommitSha = null,
    int? ProductivityPercent = null);

public record UpdateDayStatsDto(
    List<ActionTag> NewTags,
    Guid UserId,
    DateOnly Day,
    double StudyUnits = 0,
    double GitUnits = 0,
    string? LastScoredCommitSha = null,
    int? ProductivityPercent = null);

public class DayStatsService(IDayStatsRepository<DayStats> repository)
{
    private readonly IDayStatsRepository<DayStats> _repository = repository;

    public async Task<DayStats> GetByDate(Guid? userId, DateOnly? date, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(date, nameof(date));
        ArgumentNullException.ThrowIfNull(userId, nameof(date));

        if (userId.Value == Guid.Empty || date.Value.Year < 2025)
            throw new ArgumentException("Real Argument Exception by invalid data");

        return await _repository.GetByDateAsNoTracking(date.Value, userId.Value, ct);
    }

    public async Task CreateDayStats(DayStats stats, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stats, nameof(stats));
        ArgumentNullException.ThrowIfNull(stats.Tags, nameof(stats.Tags));

        if (stats.Tags.Count == 0) throw new ArgumentException("List cannot be with zero items");

        await _repository.CreateDayStats(stats, ct);
    }

    /// <summary>
    /// Upserts tags for a given user/day. Tags are matched by TagId, which is the STABLE
    /// per-app identifier (same value across all days for the same app), not the row's
    /// physical Id. If a tag with that TagId already exists for this day, its RecordedMinutes
    /// and ColorHex are updated in place (the client is expected to send the running total,
    /// not a delta). If it doesn't exist yet for this day, it's added as a new row.
    ///
    /// StudyUnits/GitUnits/LastScoredCommitSha/ProductivityPercent are NOT deltas — the client
    /// always sends its current authoritative totals for these, and the server simply mirrors
    /// them (overwrite, not merge). This differs from Tags on purpose: those metrics are
    /// already incremented client-side by the background workers, so the server doesn't need
    /// (and shouldn't try) to re-derive them.
    /// </summary>
    public async Task UpsertStats(UpdateDayStatsDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentNullException.ThrowIfNull(dto.NewTags);
        if (dto.UserId == Guid.Empty) throw new ArgumentException("Null data");

        var stats = await _repository.GetByDate(dto.Day, dto.UserId, ct);

        if (stats == null)
        {
            foreach (var tag in dto.NewTags)
            {
                if (tag.Id == Guid.Empty) tag.Id = Guid.NewGuid();
            }

            var created = DayStats.Create(dto.UserId, dto.Day, dto.NewTags);
            created.StudyUnits = dto.StudyUnits;
            created.GitUnits = dto.GitUnits;
            created.LastScoredCommitSha = dto.LastScoredCommitSha;
            created.ProductivityPercent = dto.ProductivityPercent;

            await _repository.CreateDayStats(created, ct);
            return;
        }

        // Merge by TagId (app identity), not by row Id: a tag for the same app that's
        // still accumulating minutes today must update the SAME row, not insert a new one.
        var existingByTagId = stats.Tags.ToDictionary(t => t.TagId);
        var changed = false;

        foreach (var incoming in dto.NewTags)
        {
            if (existingByTagId.TryGetValue(incoming.TagId, out var existing))
            {
                if (existing.RecordedMinutes != incoming.RecordedMinutes ||
                    existing.ColorHex != incoming.ColorHex ||
                    existing.AppTitle != incoming.AppTitle ||
                    existing.Domain != incoming.Domain)
                {
                    existing.RecordedMinutes = incoming.RecordedMinutes;
                    existing.ColorHex = incoming.ColorHex;
                    existing.AppTitle = incoming.AppTitle;
                    existing.Domain = incoming.Domain;
                    existing.DateTime = incoming.DateTime;
                    changed = true;
                }
            }
            else
            {
                if (incoming.Id == Guid.Empty) incoming.Id = Guid.NewGuid();
                stats.Tags.Add(incoming);
                changed = true;
            }
        }

        if (stats.StudyUnits != dto.StudyUnits)
        {
            stats.StudyUnits = dto.StudyUnits;
            changed = true;
        }

        if (stats.GitUnits != dto.GitUnits)
        {
            stats.GitUnits = dto.GitUnits;
            changed = true;
        }

        if (!string.IsNullOrEmpty(dto.LastScoredCommitSha) && stats.LastScoredCommitSha != dto.LastScoredCommitSha)
        {
            stats.LastScoredCommitSha = dto.LastScoredCommitSha;
            changed = true;
        }

        if (stats.ProductivityPercent != dto.ProductivityPercent)
        {
            stats.ProductivityPercent = dto.ProductivityPercent;
            changed = true;
        }

        if (changed)
        {
            await _repository.UpdateStats(stats, ct);
        }
    }

    public async Task<List<DayStats>> GetAll(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId is empty");

        var stats = await _repository.GetAll(userId, ct);

        return stats;
    }

    public async Task<List<DayStats>> GetAllFromDesktop(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId is empty");

        var stats = await _repository.GetAllFromDesktop(userId, ct);

        return stats;
    }

    public async Task<List<DayStats>> GetAllFromMobile(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId is empty");

        var stats = await _repository.GetAllFromMobile(userId, ct);

        return stats;
    }

    public async Task<DayStats> GetByDateFromMobile(Guid userId, DateOnly date, CancellationToken ct = default)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId is empty");

        var stat = await _repository.GetByDateFromMobile(userId, date, ct);
        return stat;
    }

    public async Task<DayStats> GetByDateFromDesktop(Guid userId, DateOnly date, CancellationToken ct = default)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId is empty");

        var stat = await _repository.GetByDateFromDesktop(userId, date, ct);
        return stat;
    }
}