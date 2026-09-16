using SirenaDesktop.Application.Contracts.Ai.Procent;
using SirenaDesktop.Core.Models;

namespace SirenaDesktop.Repositories;

public class TaskUnitsCalculator(IWorkspaceRepository workspaceRepository) : ITaskUnitsCalculator
{
    private const double MinSpeedMultiplier = 0.9;
    private const double MaxSpeedMultiplier = 1.1;
    private const double SpeedSensitivity = 0.1;
    private readonly IWorkspaceRepository _workspaceRepository = workspaceRepository;

    public async Task<double?> CalculateCompletionRatio(DateOnly date, CancellationToken ct = default)
    {
        var todosToday = await GetAllTodosForDate(date, ct);

        if (todosToday.Count == 0) return null;

        var completed = todosToday.Count(t => t.IsCompleted);
        return (double)completed / todosToday.Count;
    }

    public async Task<List<TodoItem>> GetAllTodosForDate(DateOnly date, CancellationToken ct = default)
    {
        var workspaces = await _workspaceRepository.GetByDate(date, ct);

        return workspaces
            .SelectMany(w => w.Todos)
            .Where(t => !t.IsDeleted)
            .ToList();
    }

    public static double? Calculate(TodoItem todo)
    {
        if (!todo.IsCompleted) return null;
        if (todo.StartTime is null || todo.EndTime is null || todo.CompleteDate is null) return null;

        var estimatedMinutes = (todo.EndTime.Value - todo.StartTime.Value).TotalMinutes;
        var actualMinutes = (todo.CompleteDate.Value - todo.StartTime.Value).TotalMinutes;

        if (estimatedMinutes <= 0) return null;
        if (actualMinutes <= 0) actualMinutes = 0.1;

        var speedMultiplier = 1 + SpeedSensitivity * (estimatedMinutes - actualMinutes) / estimatedMinutes;
        speedMultiplier = Math.Clamp(speedMultiplier, MinSpeedMultiplier, MaxSpeedMultiplier);

        return estimatedMinutes * speedMultiplier;
    }

    public static double CalculateTotal(IEnumerable<TodoItem> todosCompletedToday)
    {
        return todosCompletedToday
            .Select(Calculate)
            .Where(u => u.HasValue)
            .Sum(u => u!.Value);
    }

    public static double? DailyGoalFromTodos(IEnumerable<TodoItem> todosToday)
    {
        double total = 0;
        var any = false;

        foreach (var todo in todosToday)
        {
            if (todo.StartTime is null || todo.EndTime is null) continue;

            var estimated = (todo.EndTime.Value - todo.StartTime.Value).TotalMinutes;
            if (estimated <= 0) continue;

            total += estimated;
            any = true; 
        }

        return any ? total : null;
    }
}