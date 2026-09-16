using SirenaAPI.Interfaces;
using SirenaAPI.Logic.Entities;

namespace SirenaAPI.Application.Services;

public class TodoItemService(ITodoItemRepository repository)
{
    private readonly ITodoItemRepository _repository = repository;

    public async Task SyncTodos(Guid workspaceId, List<TodoItem> todos, CancellationToken ct = default)
    {
        await _repository.SyncForWorkspace(workspaceId, todos, ct);
    }
}