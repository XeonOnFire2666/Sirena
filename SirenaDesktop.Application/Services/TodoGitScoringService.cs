using Microsoft.Extensions.Logging;
using SirenaDesktop.Application.Contracts.Git;
using SirenaDesktop.Core.Models;
using SirenaDesktop.Services;

namespace SirenaDesktop.Application.Services;

public class TodoGitScoringService(
    IGitProductivityAnalyzer gitAnalyzer,
    Func<string?> getTrackedRepoPath,
    Func<double, double> pointsToPercent,
    DayStatsService statsService,
    ILogger<TodoGitScoringService>? logger = null)
{
    private readonly Func<string?> _getTrackedRepoPath = getTrackedRepoPath;
    private readonly IGitProductivityAnalyzer _gitAnalyzer = gitAnalyzer;
    private readonly ILogger<TodoGitScoringService>? _logger = logger;
    private readonly Func<double, double> _pointsToPercent = pointsToPercent;
    private readonly DayStatsService _statsService = statsService;

    public async Task ScoreCompletedTodoAsync(
        TodoItem todo,
        string workspaceTypeId,
        Guid userId,
        CancellationToken ct = default)
    {
        if (workspaceTypeId != "software-engineering") return;
        if (!todo.IsCompleted || todo.StartTime is null) return;

        var repoPath = _getTrackedRepoPath();
        if (string.IsNullOrEmpty(repoPath)) return;

        var completedAt = todo.CompleteDate ?? DateTime.UtcNow;
        if (completedAt <= todo.StartTime.Value) return;

        var date = DateOnly.FromDateTime(completedAt);

        try
        {
            var existing = await _statsService.GetByDate(date, userId, ct);
            var alreadyScored = existing?.ScoredCommitShas ?? new HashSet<string>();

            var newScores = _gitAnalyzer.AnalyzeRange(repoPath, todo.StartTime.Value, completedAt, alreadyScored);
            if (newScores.Count == 0) return;

            double rawPoints = newScores.Sum(s => s.TotalPoints);
            var deltaPercent = _pointsToPercent(rawPoints);
            var newShas = newScores.Select(s => s.Sha).ToList();

            await _statsService.IncrementGitUnits(userId, date, deltaPercent, newShas, ct);

            _logger?.LogInformation(
                "Todo {TodoId}: +{DeltaPercent:F2}% git units ({RawPoints} raw) for {Count} commits, interval {From}–{To}",
                todo.Id, deltaPercent, rawPoints, newScores.Count, todo.StartTime.Value, completedAt);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[TodoGitScoringService] scoring error for todo {TodoId}", todo.Id);
        }
    }
}