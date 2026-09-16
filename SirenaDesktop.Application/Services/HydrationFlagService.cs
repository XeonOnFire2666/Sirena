using SirenaDesktop.Repositories;

namespace SirenaDesktop.Services;

public class HydrationFlagService(IHydrationFlagRepository repository)
{
    private readonly IHydrationFlagRepository _repository = repository;

    public async Task<bool> IsHydrated(Guid userId, CancellationToken ct = default)
    {
        return await _repository.IsHydrated(userId, ct);
    }

    public async Task MarkHydrated(Guid userId, CancellationToken ct = default)
    {
        await _repository.MarkHydrated(userId, ct);
    }
}