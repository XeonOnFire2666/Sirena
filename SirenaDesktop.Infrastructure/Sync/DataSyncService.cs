using System.Diagnostics;
using SirenaDesktop.ApiCallers;
using SirenaDesktop.Application.Contracts.DayStat;
using SirenaDesktop.Application.Contracts.DayStat.Sync;
using SirenaDesktop.Services;

namespace SirenaDesktop.Sync;

public class DataSyncService(
    DayStatsApiCaller webApi,
    DayStatsService dbService,
    JwtTokenContainer tokensContainer,
    StatsExistsFlagService flagService,
    HydrationFlagService hydrationFlagService) : IDataSyncService
{
    private readonly DayStatsService _dbService = dbService;
    private readonly StatsExistsFlagService _flagService = flagService;
    private readonly HydrationFlagService _hydrationFlagService = hydrationFlagService;
    private readonly JwtTokenContainer _tokensContainer = tokensContainer;
    private readonly DayStatsApiCaller _webApi = webApi;

    public async Task<SyncDataSuccess> SyncData(CancellationToken stoppingToken)
    {
        var userId = _tokensContainer.GetUserId();
        if (userId == Guid.Empty)
            return new SyncDataSuccess(true, false);

        Debug.WriteLine("--------[SyncData] Fetching all-time user history from server...");
        var dataFromServer = await _webApi.GetForAllTime(userId);

        if (dataFromServer.IsError)
            return new SyncDataSuccess(true, false);
        if (dataFromServer.IsUnauthorizedError)
            return new SyncDataSuccess(false, true);

        await _dbService.AddDownloadData(dataFromServer.MoreSlatts, stoppingToken);

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (dataFromServer.MoreSlatts.Any(s => s.Date == today))
            await _flagService.Update(true, today, userId, stoppingToken);

        await _hydrationFlagService.MarkHydrated(userId, stoppingToken);

        return new SyncDataSuccess(false, false);
    }
}