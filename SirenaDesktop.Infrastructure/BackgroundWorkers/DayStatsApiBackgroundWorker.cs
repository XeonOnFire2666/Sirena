using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SirenaDesktop.ApiCallers;
using SirenaDesktop.Application.Contracts;
using SirenaDesktop.Application.Contracts.Connect;
using SirenaDesktop.Core.Models;
using SirenaDesktop.Infrastructure.Services;
using SirenaDesktop.Services;
using SirenaDesktop.Sync;

public class DayStatsApiBackgroundWorker(
    DayStatsApiCaller webApi,
    IAuthSignalService authSignal,
    IServiceScopeFactory scopeFactory,
    JwtTokenContainer tokenContainer,
    TokenRefreshService refreshService,
    StatsExistsFlagService flagService,
    IConnectionContainer connectionContainer) : BackgroundService
{
    private readonly IAuthSignalService _authSignal = authSignal;
    private readonly StatsExistsFlagService _flagService = flagService;
    private readonly TokenRefreshService _refreshService = refreshService;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IConnectionContainer _connectionContainer = connectionContainer;

    private readonly Dictionary<(Guid UserId, DateOnly Day), Dictionary<Guid, int>> _syncedTagMinutes = [];
    private readonly JwtTokenContainer _tokenContainer = tokenContainer;
    private readonly DayStatsApiCaller _webApi = webApi;

    private readonly int delayStepMs = 2000;
    private readonly int totalDelayMs = 300000;
    private bool _isSync;
    private Guid? _lastUserId;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_connectionContainer.GetState())
                {
                    Debug.WriteLine("------[Worker] Offline mode active. Waiting for connection...");
                    await _connectionContainer.WaitForOnlineAsync(stoppingToken).ConfigureAwait(false);
                    Debug.WriteLine("------[Worker] Connection restored! Resuming sync...");
                }

                var userId = _tokenContainer.GetUserId();
                Debug.WriteLine($"------[Worker] Tick. userId={userId}, lastUserId={_lastUserId}, isSync={_isSync}");

                if (userId == Guid.Empty || userId != _lastUserId)
                {
                    _isSync = false;
                    if (userId == Guid.Empty)
                    {
                        Debug.WriteLine("------[Worker] userId empty, waiting for login");
                        _lastUserId = null;
                        await _authSignal.WaitForLoginAsync(stoppingToken).ConfigureAwait(false);
                        continue;
                    }

                    _lastUserId = userId;
                }

                var todayDate = DateOnly.FromDateTime(DateTime.Today);
                using (var scope = _scopeFactory.CreateScope())
                {
                    if (!_isSync)
                    {
                        var hydrationService = scope.ServiceProvider.GetRequiredService<DataSyncService>();
                        var syncOk = await SyncWithRetryAsync(hydrationService, stoppingToken);

                        if (!syncOk)
                        {
                            _isSync = false;
                            _connectionContainer.SetState(true);
                            await Task.Delay(30000, stoppingToken);
                            continue;
                        }

                        _isSync = true;
                        var isExistTodayStats = await _flagService.GetByDate(userId, todayDate, stoppingToken);

                        if (isExistTodayStats == null)
                        {
                            await _flagService.Add(todayDate, userId, false, stoppingToken);
                            isExistTodayStats = await _flagService.GetByDate(userId, todayDate, stoppingToken);
                        }
                    }

                    var dbService = scope.ServiceProvider.GetRequiredService<DayStatsService>();
                    var flagService = scope.ServiceProvider.GetRequiredService<StatsExistsFlagService>();
                    var stats = await dbService.GetByDate(todayDate, userId, stoppingToken);

                    if (stats != null)
                    {
                        var isExistTodayStats = await flagService.GetByDate(userId, todayDate, stoppingToken);
                        Debug.WriteLine(
                            $"-----[Worker] Flag: {(isExistTodayStats == null ? "-----NULL" : isExistTodayStats.IsExists.ToString())}");

                        if (isExistTodayStats == null || !isExistTodayStats.IsExists)
                        {
                            var result = await CreateWithRetryAsync(stats, userId, todayDate);
                            Debug.WriteLine(
                                $"--------[Worker] CreateDayStats -> IsError={result.IsError}, IsUnauthorized={result.IsUnauthorizedError}, msg={result.message}");

                            if (result.IsError)
                            {
                                _connectionContainer.SetState(true);
                            }
                            else if (!result.IsUnauthorizedError)
                            {
                                MarkAllSynced(userId, todayDate, stats);
                                if (isExistTodayStats == null || !isExistTodayStats.IsExists)
                                    await flagService.Add(todayDate, userId, true, stoppingToken);
                                else
                                    await flagService.Update(true, todayDate, userId, stoppingToken);
                            }
                        }
                        else
                        {
                            var key = (userId, todayDate);
                            if (!_syncedTagMinutes.ContainsKey(key))
                            {
                                Debug.WriteLine(
                                    "--------[Worker] App restarted. Fetching today's synced tags from server to prevent duplicates...");
                                var serverData = await _webApi.GetByDate(todayDate, userId);

                                if (serverData.IsError)
                                {
                                    _connectionContainer.SetState(true);
                                }
                                else if (!serverData.IsUnauthorizedError && serverData.Stats != null)
                                {
                                    MarkAllSynced(userId, todayDate, serverData.Stats);
                                }
                            }

                            var deltaTags = GetUnsyncedTags(userId, todayDate, stats);
                            if (deltaTags.Count == 0)
                            {
                                Debug.WriteLine("--------[Worker] Nothing new to sync, skipping update call");
                            }
                            else
                            {
                                var result = await UpdateWithRetryAsync(stats, deltaTags);
                                Debug.WriteLine(
                                    $"--------[Worker] UpdateDayStats -> IsError={result.IsError}, IsUnauthorized={result.IsUnauthorizedError}, msg={result.message}");

                                if (result.IsError)
                                {
                                    _connectionContainer.SetState(true);
                                }
                                else if (!result.IsUnauthorizedError)
                                {
                                    MarkSynced(userId, todayDate, deltaTags);
                                }
                            }
                        }
                    }
                }

                for (var i = 0; i < totalDelayMs; i += delayStepMs)
                {
                    if (stoppingToken.IsCancellationRequested
                        || _tokenContainer.GetUserId() != _lastUserId
                        || _connectionContainer.GetState())         
                    {
                        break;
                    }

                    await Task.Delay(delayStepMs, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private List<ActionTag> GetUnsyncedTags(Guid userId, DateOnly day, DayStats stats)
    {
        var key = (userId, day);
        var localPlatformTags = stats.Tags.Where(t => t.Platform == "windows");

        if (!_syncedTagMinutes.TryGetValue(key, out var syncedMinutes)) return localPlatformTags.ToList();

        return localPlatformTags
            .Where(t => !syncedMinutes.TryGetValue(t.TagId, out var lastSynced) || lastSynced != t.RecordedMinutes)
            .ToList();
    }

    private void MarkSynced(Guid userId, DateOnly day, List<ActionTag> tags)
    {
        var key = (userId, day);
        if (!_syncedTagMinutes.TryGetValue(key, out var map))
        {
            map = new Dictionary<Guid, int>();
            _syncedTagMinutes[key] = map;
        }

        foreach (var t in tags.Where(x => x.Platform == "windows")) map[t.TagId] = t.RecordedMinutes;
    }

    private void MarkAllSynced(Guid userId, DateOnly day, DayStats stats)
    {
        MarkSynced(userId, day, stats.Tags);
    }

    private async Task<bool> SyncWithRetryAsync(DataSyncService hydrationService, CancellationToken ct)
    {
        var error = await hydrationService.SyncData(ct);
        Debug.WriteLine(
            $"--------[Worker] SyncData result: IsError={error.IsError}, IsUnauthorized={error.IsUnauthorizedError}");

        if (!error.IsError && !error.IsUnauthorizedError)
            return true;

        if (error.IsError)
            return false;

        var refreshed = await _refreshService.TryRefreshAsync();
        if (!refreshed)
        {
            Debug.WriteLine("---- - - - - -[TokenError]: Refresh failed, user needs to re-login");
            return false;
        }

        var retryError = await hydrationService.SyncData(ct);
        Debug.WriteLine(
            $"--------[Worker] SyncData retry result: IsError={retryError.IsError}, IsUnauthorized={retryError.IsUnauthorizedError}");

        return !retryError.IsError && !retryError.IsUnauthorizedError;
    }

    private async Task<DayStatsResponce> CreateWithRetryAsync(DayStats stats, Guid userId, DateOnly todayDate)
    {
        var result = await _webApi.CreateDayStats(
            new CreateDayStatsDto(stats.Tags.Where(c => c.Platform == "windows").ToList(), userId, todayDate));

        if (!result.IsUnauthorizedError)
            return result;

        var refreshed = await _refreshService.TryRefreshAsync();
        if (!refreshed)
            return result;

        return await _webApi.CreateDayStats(
            new CreateDayStatsDto(stats.Tags.Where(c => c.Platform == "windows").ToList(), userId, todayDate));
    }

    private async Task<DayStatsResponce> UpdateWithRetryAsync(DayStats stats, List<ActionTag> deltaTags)
    {
        var result = await _webApi.UpdateDayStats(new UpdateDayStatsDto(deltaTags, stats.UserId, stats.Date));

        if (!result.IsUnauthorizedError)
            return result;

        var refreshed = await _refreshService.TryRefreshAsync();
        if (!refreshed)
            return result;

        return await _webApi.UpdateDayStats(new UpdateDayStatsDto(deltaTags, stats.UserId, stats.Date));
    }
}