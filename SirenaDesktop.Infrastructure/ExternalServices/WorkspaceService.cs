using System.Text.Json;
using SirenaDesktop.ApiCallers;
using SirenaDesktop.Application.Contracts;
using SirenaDesktop.Application.Services;
using SirenaDesktop.Core.Models;
using SirenaDesktop.Infrastructure.Infrastructure;
using SirenaDesktop.Repositories;
using SirenaDesktop.Services;

namespace SirenaDesktop.Infrastructure.Services;

public class WorkspaceService(
    IWorkspaceRepository repository,
    WorkspaceApiCaller api,
    IPendingChangeRepository pendingRepo,
    JwtTokenContainer jwt,
    WorkspaceDbService dbService) : IWorkspaceService
{
    private readonly WorkspaceApiCaller _api = api;
    private readonly WorkspaceDbService _dbService = dbService;
    private readonly JwtTokenContainer _jwt = jwt;
    private readonly IPendingChangeRepository _pendingRepo = pendingRepo;
    private readonly IWorkspaceRepository _repository = repository;

    public async Task<List<Workspace>> GetAll()
    {
        return await _dbService.GetAll();
    }

    public async Task<List<Workspace>> GetByDate(DateOnly date)
    {
        return await _dbService.GetByDate(date);
    }

    public async Task<WorkspaceSyncResult> Sync()
    {
        var userId = _jwt.GetUserId();
        if (userId == Guid.Empty)
            return new WorkspaceSyncResult(await _dbService.GetAll(), true);

        var response = await _api.GetAll(userId);

        if (response.IsError || response.IsUnauthorizedError || response.Workspaces is null)
            return new WorkspaceSyncResult(await _dbService.GetAll(), true);

        foreach (var serverWs in response.Workspaces)
        {
            var local = await _repository.GetById(serverWs.Id);
            if (local != null && local.IsDeleted && !serverWs.IsDeleted)
                continue;

            if (local is null || serverWs.UpdatedAt > local.UpdatedAt)
                await _repository.Upsert(serverWs);
        }

        return new WorkspaceSyncResult(await _dbService.GetAll(), false);
    }

    public async Task<Workspace> Add(string workspaceTypeId, string name, DateOnly date)
    {
        var ws = new Workspace
        {
            Id = Guid.NewGuid(),
            UserId = _jwt.GetUserId(),
            WorkspaceTypeId = workspaceTypeId,
            Name = string.IsNullOrWhiteSpace(name)
                ? WorkspaceType.GetDefaultTypes().FirstOrDefault(t => t.Id == workspaceTypeId)?.Title ?? "Пространство"
                : name.Trim(),
            ColorHex = WorkspaceColorPalette.PickRandom(),
            Date = date,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.Add(ws);
        await PushWorkspace(ws, true);

        return ws;
    }

    public async Task Delete(Workspace ws)
    {
        ws.IsDeleted = true;
        await Update(ws);
    }

    public async Task ClearAllWorkspaces()
    {
        await _repository.DeleteWorkspaces();
    }

    public async Task<TodoItem> AddTodo(Workspace ws, string title, DateTime? startTime, DateTime? endTime)
    {
        var todo = new TodoItem
        {
            Id = Guid.NewGuid(),
            Title = title,
            StartTime = startTime,
            EndTime = endTime,
            CompleteDate = null,
            IsCompleted = false,
            UpdatedAt = DateTime.UtcNow
        };

        ws.Todos.Add(todo);
        await Update(ws);
        return todo;
    }

    public async Task UpdateTodo(Workspace ws, TodoItem todo)
    {
        todo.UpdatedAt = DateTime.UtcNow;
        await Update(ws);
    }

    public async Task SetTodoCompletion(Workspace ws, TodoItem todo, bool isCompleted)
    {
        todo.IsCompleted = isCompleted;
        todo.CompleteDate = isCompleted ? DateTime.UtcNow : null;
        todo.UpdatedAt = DateTime.UtcNow;

        await Update(ws);
    }

    public async Task DeleteTodo(Workspace ws, TodoItem todo)
    {
        todo.IsDeleted = true;
        todo.UpdatedAt = DateTime.UtcNow;
        await Update(ws);
    }

    public async Task<bool> PushPendingChanges(List<PendingChange> wsChanges)
    {
        foreach (var change in wsChanges)
        {
            var ws = JsonSerializer.Deserialize<Workspace>(change.PayloadJson);
            if (ws is null)
            {
                await _pendingRepo.Remove(change.Id);
                continue;
            }

            var result = change.Type switch
            {
                PendingChangeType.Create => await _api.Create(new CreateWorkspaceDto(ws.Id, ws.UserId,
                    ws.WorkspaceTypeId, ws.Name, ws.ColorHex, ws.Date, MapTodosOut(ws.Todos), ws.UpdatedAt)),
                _ => await _api.Update(new UpdateWorkspaceDto(ws.Id, ws.UserId, ws.WorkspaceTypeId, ws.Name,
                    ws.ColorHex, ws.Date, MapTodosOut(ws.Todos), ws.IsDeleted, ws.UpdatedAt))
            };

            if (result.IsError) return false;

            await _pendingRepo.Remove(change.Id);
        }

        return true;
    }

    public async Task Update(Workspace ws)
    {
        ws.UpdatedAt = DateTime.UtcNow;
        await _repository.Update(ws);
        await PushWorkspace(ws, false);
    }

    private async Task PushWorkspace(Workspace ws, bool isNew)
    {
        var result = isNew
            ? await _api.Create(new CreateWorkspaceDto(ws.Id, ws.UserId, ws.WorkspaceTypeId, ws.Name, ws.ColorHex,
                ws.Date, MapTodosOut(ws.Todos), ws.UpdatedAt))
            : await _api.Update(new UpdateWorkspaceDto(ws.Id, ws.UserId, ws.WorkspaceTypeId, ws.Name, ws.ColorHex,
                ws.Date, MapTodosOut(ws.Todos), ws.IsDeleted, ws.UpdatedAt));

        if (result.IsError)
            await _repository.QueuePendingChange(ws, isNew ? PendingChangeType.Create : PendingChangeType.Update);
    }

    private static List<TodoDtoOut> MapTodosOut(List<TodoItem> todos)
    {
        return todos.Select(t => new TodoDtoOut(
            t.Id,
            t.Title,
            t.StartTime,
            t.EndTime,
            t.CompleteDate,
            t.IsCompleted,
            t.IsDeleted,
            t.UpdatedAt
        )).ToList();
    }
}