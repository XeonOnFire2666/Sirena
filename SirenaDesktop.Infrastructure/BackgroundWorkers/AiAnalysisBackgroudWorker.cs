using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SirenaDesktop.Infrastructure.AI;
using SirenaDesktop.Services;

namespace SirenaDesktop.Infrastructure.BackgroundWorkers;

public class AiAnalysisBackgroudWorker(
    LocalAiVideoAnalysis videoAnalysis,
    DayStatsService statsService,
    JwtTokenContainer tokenContainer,
    ILogger<AiAnalysisBackgroudWorker>? logger = null) : BackgroundService
{
    private const int MinAccumulatedSecondsToAnalyze = 30;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);
    private readonly ILogger<AiAnalysisBackgroudWorker>? _logger = logger;
    private readonly DayStatsService _statsService = statsService;
    private readonly JwtTokenContainer _tokenContainer = tokenContainer;
    private readonly LocalAiVideoAnalysis _videoAnalysis = videoAnalysis;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var userId = _tokenContainer.GetUserId();

                if (userId != Guid.Empty)
                {
                    var allTime = _videoAnalysis.GetAllTime();

                    if (allTime >= MinAccumulatedSecondsToAnalyze)
                    {
                        var result = await _videoAnalysis.AnalyzeAndFlushAsync(stoppingToken)
                            .ConfigureAwait(false);

                        Console.WriteLine("");

                        if (result.StudyUnits > 0)
                        {
                            var today = DateOnly.FromDateTime(DateTime.Today);

                            await _statsService.IncrementStudyUnits(
                                    userId, today, result.StudyUnits, stoppingToken)
                                .ConfigureAwait(false);

                            _logger?.LogInformation(
                                "Study units +{StudyUnits} from {Count} videos",
                                result.StudyUnits, result.Items.Count);
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

            await Task.Delay(PollInterval, stoppingToken).ConfigureAwait(false);
        }
    }
}