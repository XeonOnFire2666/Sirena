using Microsoft.EntityFrameworkCore;
using SirenaDesktop;
using SirenaDesktop.Application.Contracts;
using SirenaDesktop.Core.Models;
using SirenaDesktop.Infrastructure.BackgroundWorkers;
using SirenaDesktop.Infrastructure.Infrastructure;
using SirenaDesktop.Infrastructure.Persistence;
using SirenaDesktop.Services;
using SirenaDesktop.Sync;

public class WorkspaceService(
    WorkspaceStore store,
    SyncEngine engine,
    SyncTrigger trigger,
    JwtTokenContainer jwt,
    IDbContextFactory<SirenaDbContext> factory) : IWorkspaceService
{
    public event EventHandler? Changed;

    public Task<List<Workspace>> GetAll() => store.GetAllAsync();

    public async Task<List<Workspace>> GetByDate(DateOnly date) =>
        (await store.GetAllAsync()).Where(w => !w.IsDeleted && w.Date == date).ToList();

    public async Task<Workspace> Add(string workspaceTypeId, string name, DateOnly date)
    {
        var ws = new Workspace
        {
            Id = Guid.NewGuid(),
            UserId = jwt.GetUserId(),
            WorkspaceTypeId = workspaceTypeId,
            Name = string.IsNullOrWhiteSpace(name)
                ? WorkspaceType.GetDefaultTypes().FirstOrDefault(t => t.Id == workspaceTypeId)?.Title ?? "Workspace"
                : name.Trim(),
            ColorHex = WorkspaceColorPalette.PickRandom(),
            Date = date
        };

        await CommitAsync(ws);
        return ws;
    }

    public Task Delete(Workspace ws)
    {
        ws.IsDeleted = true;
        return Update(ws);
    }

    public Task Update(Workspace ws) => CommitAsync(ws);

    public Task<TodoItem> AddTodo(Workspace ws, string title, DateTime? startTime, DateTime? endTime)
    {
        var todo = new TodoItem
        {
            Id = Guid.NewGuid(),
            Title = title,
            StartTime = startTime,
            EndTime = endTime,
            IsCompleted = false,
            UpdatedAt = DateTime.UtcNow
        };
        ws.Todos.Add(todo);
        return CommitAsync(ws).ContinueWith(_ => todo);
    }

    public Task UpdateTodo(Workspace ws, TodoItem todo)
    {
        todo.UpdatedAt = DateTime.UtcNow;
        return CommitAsync(ws);
    }

    public Task SetTodoCompletion(Workspace ws, TodoItem todo, bool isCompleted)
    {
        todo.IsCompleted = isCompleted;
        todo.CompleteDate = isCompleted ? DateTime.UtcNow : null;
        todo.UpdatedAt = DateTime.UtcNow;
        return CommitAsync(ws);
    }

    public Task DeleteTodo(Workspace ws, TodoItem todo)
    {
        todo.IsDeleted = true;
        todo.UpdatedAt = DateTime.UtcNow;
        return CommitAsync(ws);
    }

    public async Task<WorkspaceSyncResult> Sync()
    {
        var ok = await engine.RunAsync();
        Changed?.Invoke(this, EventArgs.Empty);
        return new WorkspaceSyncResult(await store.GetAllAsync(), !ok);
    }

    private async Task CommitAsync(Workspace ws)
    {
        await store.SaveLocalAsync(ws);
        Changed?.Invoke(this, EventArgs.Empty);
        trigger.Kick();
    }

    public async Task ClearAllWorkspaces()
    {
        await store.ClearAllWorkspacesAsync();
        Changed?.Invoke(this, EventArgs.Empty);
    }
}


