using SirenaDesktop.Core.Models;

namespace SirenaDesktop.Application.Contracts.Ai.Procent;

public interface ITaskUnitsCalculator
{
    Task<double?> CalculateCompletionRatio(DateOnly date, CancellationToken ct = default);

    Task<List<TodoItem>> GetAllTodosForDate(DateOnly date, CancellationToken ct = default);
}