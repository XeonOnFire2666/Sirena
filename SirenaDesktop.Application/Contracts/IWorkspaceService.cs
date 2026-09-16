using SirenaDesktop.Core.Models;

namespace SirenaDesktop.Application.Contracts;

public record WorkspaceSyncResult(List<Workspace> Workspaces, bool IsOffline);

public interface IWorkspaceService
{
    Task<List<Workspace>> GetAll();
    Task<List<Workspace>> GetByDate(DateOnly date);
    Task<WorkspaceSyncResult> Sync();
    Task<Workspace> Add(string workspaceTypeId, string name, DateOnly date);
    Task Delete(Workspace ws);
    Task ClearAllWorkspaces();
    Task<TodoItem> AddTodo(Workspace ws, string title, DateTime? startTime, DateTime? endTime);
    Task UpdateTodo(Workspace ws, TodoItem todo);
    Task SetTodoCompletion(Workspace ws, TodoItem todo, bool isCompleted);
    Task DeleteTodo(Workspace ws, TodoItem todo);
}