using SirenaDesktop.Core.Models;

namespace SirenaDesktop.Repositories;

public interface IDayStatsRepository
{
    Task CreateDayStats(DayStats entity, CancellationToken ct = default);
    Task<DayStats?> GetByDateAsNoTracking(DateOnly date, Guid userId, CancellationToken ct = default);
    Task<DayStats?> GetByDateMobile(DateOnly date, Guid userId, CancellationToken ct = default);
    Task<DayStats?> GetByDate(DateOnly date, Guid userId, CancellationToken ct = default);
    Task UpsertTag(Guid userId, DateOnly date, ActionTag tag, CancellationToken ct = default);
    Task SetProductivityPercent(Guid userId, DateOnly date, int percent, CancellationToken ct = default);
    Task IncrementStudyUnits(Guid userId, DateOnly date, double studyUnitsDelta, CancellationToken ct = default);

    Task IncrementGitUnits(Guid userId, DateOnly date, double gitUnitsDelta, IEnumerable<string> newlyScoredShas,
        CancellationToken ct = default);

    Task<bool> CheckStats(DateOnly day, Guid userId, CancellationToken ct = default);
    Task<bool> HasAnyData(Guid userId, CancellationToken ct = default);
}

public interface IWorkspaceRepository
{
    Task<List<Workspace>> GetAll(CancellationToken ct = default);
    Task<List<Workspace>> GetByDate(DateOnly date, CancellationToken ct = default);
    Task<Workspace?> GetById(Guid id, CancellationToken ct = default);
    Task Add(Workspace ws, CancellationToken ct = default);
    Task Update(Workspace ws, CancellationToken ct = default);
    Task DeleteWorkspaces(CancellationToken ct = default);
    Task DeleteWorkspace(Guid id, CancellationToken ct = default);
    Task Upsert(Workspace ws, CancellationToken ct = default);
    Task QueuePendingChange(Workspace ws, PendingChangeType type, CancellationToken ct = default);
}

public interface IPendingChangeRepository
{
    Task<List<PendingChange>> GetAll(CancellationToken ct = default);
    Task Remove(Guid pendingChangeId, CancellationToken ct = default);
    Task RemoveAll(CancellationToken ct = default);
}

public interface IStatsExistsFlagRepository
{
    Task<PendingChanges?> GetByDate(Guid userId, DateOnly day, CancellationToken ct = default);
    Task Add(DateOnly day, Guid userId, bool flag = false, CancellationToken ct = default);
    Task Update(bool flag, DateOnly day, Guid userId, CancellationToken ct = default);
    Task Delete(DateOnly day, Guid userId, CancellationToken ct = default);
}

public interface IUserRepository
{
    Task<User?> GetById(Guid id, CancellationToken ct = default);
    Task<User?> Get(CancellationToken ct = default);
    Task<User?> GetByEmail(string email, CancellationToken ct = default);
    Task<User?> GetByGoogleId(string googleSub, CancellationToken ct = default);
    Task Add(User user, CancellationToken ct = default);
    Task Delete(CancellationToken ct = default);
    Task Update(User user, CancellationToken ct = default);
}

public interface IHydrationFlagRepository
{
    Task<bool> IsHydrated(Guid userId, CancellationToken ct = default);
    Task MarkHydrated(Guid userId, CancellationToken ct = default);
}

public interface ITodoItemRepository
{
    Task HardDeleteAll(CancellationToken ct = default);
}