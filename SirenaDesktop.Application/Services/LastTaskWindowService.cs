using SirenaDesktop.Core.Models;

namespace SirenaDesktop.Services;

public static class LastTaskWindowService
{
    public static TodoItem? GetCurrentLastTask(IEnumerable<TodoItem> todosToday)
    {
        return todosToday
            .Where(t => !t.IsDeleted && t.StartTime is not null)
            .OrderByDescending(t => t.EndTime ?? t.StartTime)
            .ThenByDescending(t => t.StartTime)
            .FirstOrDefault();
    }

    public static bool ShouldTriggerAnalysis(TodoItem completedTask, IEnumerable<TodoItem> allTodosBeforeCompletion)
    {
        if (completedTask.StartTime is null) return false;
        if (DateTime.Now < completedTask.StartTime.Value) return false;

        var lastTask = GetCurrentLastTask(allTodosBeforeCompletion);
        return lastTask?.Id == completedTask.Id;
    }
}