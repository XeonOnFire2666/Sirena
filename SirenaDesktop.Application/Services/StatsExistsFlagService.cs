using SirenaDesktop.Core.Models;
using SirenaDesktop.Repositories;

namespace SirenaDesktop.Services;

public class StatsExistsFlagService(IStatsExistsFlagRepository flagRepository)
{
    private readonly IStatsExistsFlagRepository _flagRepository = flagRepository;

    public async Task<PendingChanges?> GetByDate(Guid userId, DateOnly day, CancellationToken ct = default)
    {
        return await _flagRepository.GetByDate(userId, day, ct);
    }

    public async Task Add(DateOnly day, Guid userId, bool flag = false, CancellationToken ct = default)
    {
        await _flagRepository.Add(day, userId, flag, ct);
    }

    public async Task Update(bool flag, DateOnly day, Guid userId, CancellationToken ct = default)
    {
        await _flagRepository.Update(flag, day, userId, ct);
    }

    public async Task Delete(DateOnly day, Guid userId, CancellationToken ct = default)
    {
        await _flagRepository.Delete(day, userId);
    }
}