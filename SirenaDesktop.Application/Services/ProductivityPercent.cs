using SirenaDesktop.Application.Contracts.Ai.Procent;
using SirenaDesktop.Core.Models;
using SirenaDesktop.Repositories;

namespace SirenaDesktop.Services;

public class ProductivityPercent(ITaskUnitsCalculator taskUnitsCalculator)
{
    private readonly ITaskUnitsCalculator _taskUnitsCalculator = taskUnitsCalculator;

    public double DailyGoalMinutesFallback { get; set; } = 360;

    public async Task<ProductivityBreakdown> Calculate(
        List<ActionTag> sortedStats,
        DateOnly date,
        double gitUnits,
        double studyUnits,
        double? userDailyGoalMinutes,
        CancellationToken ct = default)
    {
        var breakdown = new ProductivityBreakdown();

        var todosToday = await _taskUnitsCalculator.GetAllTodosForDate(date, ct);
        var completedToday = todosToday.Where(t => t.IsCompleted).ToList();

        breakdown.TaskUnits = TaskUnitsCalculator.CalculateTotal(completedToday);

        breakdown.CompletionRatio = todosToday.Count == 0
            ? null
            : (double)completedToday.Count / todosToday.Count;

        breakdown.DailyGoalMinutes =
            userDailyGoalMinutes
            ?? TaskUnitsCalculator.DailyGoalFromTodos(todosToday)
            ?? DailyGoalMinutesFallback;

        breakdown.GitUnits = gitUnits;
        breakdown.StudyUnits = studyUnits;

        return breakdown;
    }
}

public class ProductivityBreakdown
{
    private const double MaxPercent = 125;
    public double TaskUnits { get; set; }
    public double DailyGoalMinutes { get; set; } = 1;
    public double GitUnits { get; set; }
    public double StudyUnits { get; set; }
    public double? CompletionRatio { get; set; }
    public double GitMultiplier { get; set; } = 2.5;

    private double TaskPercent =>
        DailyGoalMinutes <= 0 ? 0 : TaskUnits / DailyGoalMinutes * 100;

    private double GitPercent => GitUnits * GitMultiplier;

    public int Percent
    {
        get
        {
            var baseWithGit = TaskPercent + GitPercent;
            var withYoutube = baseWithGit + StudyUnits;
            var capped = Math.Clamp(withYoutube, 0, MaxPercent);
            return (int)Math.Round(capped);
        }
    }
}