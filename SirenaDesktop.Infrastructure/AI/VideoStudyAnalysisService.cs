using System.Diagnostics;
using SirenaDesktop.Application.Contracts.Ai;
using SirenaDesktop.Core.Models;
using SirenaDesktop.Services;

namespace SirenaDesktop.Infrastructure.AI;

public class VideoStudyAnalysisService(
    IVideoWatchAccumulator accumulator,
    IGeminiAiProvider geminiCaller,
    DayStatsService statsService,
    IApiKeyStore apiKeyStore)
{
    public async Task RunIfLastTaskAsync(
        TodoItem completedTask,
        IEnumerable<TodoItem> allTodosTodayBeforeCompletion,
        Guid userId,
        CancellationToken ct = default)
    {
        if (userId == Guid.Empty) return;

        if (!apiKeyStore.HasKey)
        {
            Debug.WriteLine(
                "[VideoStudyAnalysis] Gemini API key is not set — analysis skipped, data saved.");
            return;
        }

        if (!LastTaskWindowService.ShouldTriggerAnalysis(completedTask, allTodosTodayBeforeCompletion))
            return;

        var snapshot = accumulator.Drain();
        if (snapshot.Count == 0) return;

        Console.WriteLine(
            $"[VideoStudyAnalysis] Triggered by completing the last task '{completedTask.Title}'. Videos: {snapshot.Count}");

        VideoAnalysisResult result;
        try
        {
            result = await geminiCaller.AnalyzeAsync(snapshot, ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VideoStudyAnalysis] Unhandled exception: {ex.Message}");
            accumulator.Merge(snapshot);
            return;
        }

        if (!result.IsSuccess)
        {
            Console.WriteLine(
                "[VideoStudyAnalysis] Analysis failed (network/API). Data returned to accumulator.");
            accumulator.Merge(snapshot);
            return;
        }

        if (result.StudyUnits != 0)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            await statsService.IncrementStudyUnits(userId, today, result.StudyUnits, ct);
        }
    }
}