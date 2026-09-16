using SirenaAPI.Logic.Entities;

namespace SirenaAPI.Interfaces;

public interface IWorkspaceRepository
{
    Task<List<Workspace>> GetAll(Guid userId, CancellationToken ct);
    Task Add(Workspace item, CancellationToken ct);
    Task Update(Workspace item, CancellationToken ct);
    Task Delete(Guid id, CancellationToken ct);
}

public interface ITodoItemRepository
{
    Task SyncForWorkspace(Guid workspaceId, List<TodoItem> incomingTodos, CancellationToken ct);
}

public interface IDayStatsRepository<T>
{
    public Task<DayStats> GetByDateAsNoTracking(DateOnly dateTimeUtc, Guid userId, CancellationToken ct = default);
    public Task<DayStats> GetByDate(DateOnly dateTimeUtc, Guid userId, CancellationToken ct = default);
    public Task CreateDayStats(T entity, CancellationToken ct = default);
    public Task UpdateStats(T entity, CancellationToken ct = default);
    public Task<List<DayStats>> GetAll(Guid userId, CancellationToken ct = default);
    public Task<List<DayStats>> GetAllFromDesktop(Guid userId, CancellationToken ct = default);
    public Task<List<DayStats>> GetAllFromMobile(Guid userId, CancellationToken ct = default);
    public Task<DayStats> GetByDateFromMobile(Guid userId, DateOnly date, CancellationToken ct = default);
    public Task<DayStats> GetByDateFromDesktop(Guid userId, DateOnly date, CancellationToken ct = default);
}

public interface IUserRepository<T>
{
    public Task<T?> GetById(Guid id, CancellationToken ct = default);
    public Task<T?> GetByName(string name, CancellationToken ct = default);
    public Task Add(T entity, CancellationToken ct = default);
    public Task Delete(Guid id, CancellationToken ct = default);
    public Task Update(T entity, CancellationToken ct = default);
    public Task<T?> GetByEmail (string email, CancellationToken ct = default);
    public Task<T?> GetByGoogleId(string googleSub, CancellationToken ct = default);
    // public Task<bool> SimpleCheckStatus(string name);
}
public interface ITokenRepository<T>
{
    public Task<RefreshToken> GetById(Guid id);
    public Task Add(T entity, CancellationToken ct = default);
    public Task Delete(Guid id, CancellationToken ct = default);
    public Task<int> Update(Guid oldId, Guid newId, CancellationToken ct = default);
}