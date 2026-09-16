using SirenaAPI.Interfaces;
using SirenaAPI.Logic.Entities;

namespace SirenaAPI.Application.Services;

public class WorkspaceService(IWorkspaceRepository repository, TodoItemService todoService)
{
    private readonly IWorkspaceRepository _repository = repository;
    private readonly TodoItemService _todoService = todoService;

    public async Task<List<Workspace>> GetAll(Guid? userId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId, nameof(userId));
        if (userId.Value == Guid.Empty) throw new ArgumentException("UserId is empty");

        return await _repository.GetAll(userId.Value, ct);
    }

    public async Task Add(Workspace entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));
        await _repository.Add(entity, ct);
    }

    public async Task Update(Workspace entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));

        await _repository.Update(entity, ct);
        await _todoService.SyncTodos(entity.Id, entity.Todos, ct);
    }

    public async Task Delete(Guid id, CancellationToken ct = default)
    {
        if(id == Guid.Empty) throw new ArgumentException("Id is empty");

        await _repository.Delete(id, ct);
    }
}