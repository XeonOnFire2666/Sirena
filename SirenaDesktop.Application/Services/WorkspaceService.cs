using SirenaDesktop.Core.Models;
using SirenaDesktop.Repositories;

namespace SirenaDesktop.Application.Services;

public class WorkspaceDbService(IWorkspaceRepository repository)
{
    private readonly IWorkspaceRepository _repository = repository;

    public async Task<List<Workspace>> GetAll()
    {
        return await _repository.GetAll();
    }

    public async Task<List<Workspace>> GetByDate(DateOnly date)
    {
        return await _repository.GetByDate(date);
    }

    public async Task<int> GetAllTimeForTasks(DateOnly date)
    {
        var workspaces = await _repository.GetByDate(date);

        return (int)workspaces
            .SelectMany(w => w.Todos)
            .Where(t => t.StartTime.HasValue && t.EndTime.HasValue)
            .Sum(t => (t.EndTime.Value - t.StartTime.Value).TotalMinutes);
    }
}