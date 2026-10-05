using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SirenaDesktop.Application.Contracts;
using SirenaDesktop.Application.Contracts.Ai;
using SirenaDesktop.Application.Contracts.Connect;
using SirenaDesktop.Services;

namespace SirenaDesktop.Infrastructure.BackgroundWorkers;

public class AiAnalysisBackgroudWorker(
    IVideoWatchAccumulator accumulator,
    IGeminiAiProvider geminiCaller,
    IApiKeyStore apiKeyStore,
    JwtTokenContainer tokenContainer,
    ITimerSoundService timerSoundService,
    IConnectionContainer connectionContainer,
    int lowQualityScoreThreshold = 5,
    string? lowQualityAlertSoundPath = null,
    ILogger<AiAnalysisBackgroudWorker>? logger = null) : BackgroundService
{
    private const int MinAccumulatedMinutesToAnalyze = 1;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);
    private readonly ILogger<AiAnalysisBackgroudWorker>? _logger = logger;
    private readonly JwtTokenContainer _tokenContainer = tokenContainer;
    private readonly IVideoWatchAccumulator _accumulator = accumulator;
    private readonly IGeminiAiProvider _geminiCaller = geminiCaller;
    private readonly IApiKeyStore _apiKeyStore = apiKeyStore;
    private readonly IConnectionContainer _connectionContainer = connectionContainer;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        _logger?.LogInformation("[AiAnalysisBackgroudWorker] Started");

        using var timer = new PeriodicTimer(PollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_connectionContainer.GetState())
                {
                    _logger?.LogInformation("[AiAnalysisBackgroudWorker] Offline mode active. Waiting for connection...");
                    await _connectionContainer.WaitForOnlineAsync(stoppingToken).ConfigureAwait(false);
                    _logger?.LogInformation("[AiAnalysisBackgroudWorker] Connection restored. Resuming analysis...");
                }

                var userId = _tokenContainer.GetUserId();
                var totalMinutesPeek = _accumulator.TotalAccumulatedMinutes();

                _logger?.LogInformation(
                    "[AiAnalysisBackgroudWorker] Tick. userId={UserId}, hasKey={HasKey}, accumulatedMinutes={Minutes}",
                    userId, _apiKeyStore.HasKey, totalMinutesPeek);

                if (userId != Guid.Empty && _apiKeyStore.HasKey)
                {
                    var totalMinutes = _accumulator.TotalAccumulatedMinutes();

                    if (totalMinutes >= MinAccumulatedMinutesToAnalyze)
                    {
                        var snapshot = _accumulator.Drain();

                        _logger?.LogInformation(
                            "[AiAnalysisBackgroudWorker] Drained {Count} videos, calling Gemini",
                            snapshot.Count);

                        if (snapshot.Count > 0)
                        {
                            VideoAnalysisResult result;
                            try
                            {
                                result = await _geminiCaller.AnalyzeAsync(snapshot, stoppingToken)
                                    .ConfigureAwait(false);
                            }
                            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                            {
                                _logger?.LogInformation(
                                    "[AiAnalysisBackgroudWorker] Analysis canceled during shutdown, restoring snapshot");
                                _accumulator.Merge(snapshot);
                                break;
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogError(ex, "[AiAnalysisBackgroudWorker] Analysis call failed");
                                _accumulator.Merge(snapshot);
                                result = VideoAnalysisResult.Failed;

                                _connectionContainer.SetState(true);
                            }

                            _logger?.LogInformation(
                                "[AiAnalysisBackgroudWorker] Gemini result: IsSuccess={IsSuccess}, items={Items}",
                                result.IsSuccess, result.Items.Count);

                            if (!result.IsSuccess)
                            {
                                _accumulator.Merge(snapshot);
                            }
                            else if (HasLowQualityContent(result))
                            {
                                _logger?.LogInformation(
                                    "Low quality content detected (threshold {Threshold}). Playing alert sound.",
                                    lowQualityScoreThreshold);

                                timerSoundService.PlaySound(lowQualityAlertSoundPath);
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[AiAnalysisBackgroudWorker] error");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
                    break;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger?.LogInformation("[AiAnalysisBackgroudWorker] Stopped");
    }

    private bool HasLowQualityContent(VideoAnalysisResult result)
    {
        foreach (var item in result.Items)
        {
            if (item.Score < lowQualityScoreThreshold)
                return true;
        }

        return false;
    }
}