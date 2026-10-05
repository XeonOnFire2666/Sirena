using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SirenaDesktop.Application.Contracts;
using SirenaDesktop.Application.Contracts.Ai;
using SirenaDesktop.Core.Interfaces;
using SirenaDesktop.Core.Models;
using SirenaDesktop.Infrastructure.Infrastructure;
using SirenaDesktop.Infrastructure.Sync.PlatformDataTracker;
using SirenaDesktop.Repositories;
using SirenaDesktop.Services;
using SirenaDesktop.Services.Platform;
using System.Diagnostics;

namespace SirenaDesktop.Infrastructure.BackgroundWorkers;

public class DataCollectorBackgroundWorker(
    IPlatformTracker api,
    IServiceScopeFactory scopeFactory,
    JwtTokenContainer tokensService,
    IAuthSignalService authSignal,
    IVideoWatchAccumulator videoAccumulator) : BackgroundService
{
    private const int SaveThresholdSeconds = 60;
    private readonly IPlatformTracker _api = api;
    private readonly IAuthSignalService _authSignal = authSignal;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly JwtTokenContainer _tokensService = tokensService;
    private readonly IVideoWatchAccumulator videoAccumulator = videoAccumulator;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var safe = new Dictionary<string, int>();
        var minutesToday = new Dictionary<string, int>();
        var trackedDay = DateOnly.FromDateTime(DateTime.Today);
        var hydrated = false;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var userId = IsUserAuthorized();
                if (userId == Guid.Empty)
                {
                    hydrated = false;
                    await _authSignal.WaitForLoginAsync(stoppingToken).ConfigureAwait(false);
                    continue;
                }

                var today = DateOnly.FromDateTime(DateTime.Today);
                if (today != trackedDay)
                {
                    var dropped = videoAccumulator.TotalAccumulatedMinutes();
                    if (dropped > 0)
                        Debug.WriteLine(
                            $"[Collector] Day changed: discarded {dropped} min of video without analysis.");

                    trackedDay = today;
                    videoAccumulator.Clear();
                    minutesToday.Clear();
                    safe.Clear();
                    hydrated = false;
                }

                if (!hydrated)
                {
                    await HydrateMinutesToday(minutesToday, userId, today, stoppingToken);
                    hydrated = true;
                }

                var windowInfo = _api.GetActiveWindowInfo();

                var isYoutubeWatch = !string.IsNullOrEmpty(windowInfo.Domain)
                                     && windowInfo.Domain.Contains("youtube.com", StringComparison.OrdinalIgnoreCase)
                                     && YouTubeHelper.IsVideoWatchPage(windowInfo.Url);

                if (isYoutubeWatch)
                {
                    var rawTitle = YouTubeHelper.ExtractVideoTitle(windowInfo.Title);
                    var key = string.IsNullOrWhiteSpace(rawTitle) ? windowInfo.Domain : rawTitle;
                    videoAccumulator.Upsert(key, 5);
                }
                else
                {
                    var afk = _api.GetIdleSeconds();
                    if (afk > 180)
                    {
                        var delay = afk > 1800 ? 500000 : 30000;
                        await Task.Delay(delay, stoppingToken);
                        continue;
                    }
                }

                var isDomain = !string.IsNullOrEmpty(windowInfo.Domain);
                var trackKey = isDomain ? windowInfo.Domain : windowInfo.AppName;

                var accumulatedSeconds = safe.GetValueOrDefault(trackKey, 0) + 5;
                safe[trackKey] = accumulatedSeconds;

                if (accumulatedSeconds >= SaveThresholdSeconds)
                {
                    safe[trackKey] = 0;
                    var newMinutes = minutesToday.GetValueOrDefault(trackKey, 0) + 1;
                    minutesToday[trackKey] = newMinutes;

                    await SaveStats(windowInfo, trackKey, newMinutes, userId, today, stoppingToken);
                }

                await Task.Delay(5000, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected shutdown when the application closes — ignore, see Microsoft docs on background Service
            // https://learn.microsoft.com/en-us/dotnet/core/extensions/hosted-services
        }
    }

    private async Task HydrateMinutesToday(
        Dictionary<string, int> minutesToday,
        Guid userId,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbService = scope.ServiceProvider.GetRequiredService<DayStatsService>();

            var stats = await dbService.GetByDate(today, userId, cancellationToken);
            if (stats == null)
                return;

            foreach (var tag in stats.Tags.Where(t => t.Platform == "windows"))
            {
                var isDomain = !string.IsNullOrEmpty(tag.Domain);
                var trackKey = isDomain ? tag.Domain! : tag.AppTitle;

                if (string.IsNullOrEmpty(trackKey))
                    continue;
                if (!minutesToday.TryGetValue(trackKey, out var existing) || tag.RecordedMinutes > existing)
                    minutesToday[trackKey] = tag.RecordedMinutes;
            }

            Debug.WriteLine(
                $"[Collector] Hydrated minutesToday with {minutesToday.Count} trackKeys from local DB.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Collector Hydrate Error]: {ex}");
        }
    }

    private async Task SaveStats(
        WindowInfo windowInfo,
        string trackKey,
        int totalMinutesToday,
        Guid userId,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbService = scope.ServiceProvider.GetRequiredService<DayStatsService>();
            var mapRepository = scope.ServiceProvider.GetRequiredService<AppTagMapRepository>();

            var map = await mapRepository.GetOrCreate(
                userId,
                trackKey,
                () => RandomColorGenerator.Generate().ToString(),
                cancellationToken);

            var tag = new ActionTag(
                Guid.NewGuid(),
                map.TagId,
                DateTime.UtcNow,
                windowInfo.AppName,
                windowInfo.Domain,
                totalMinutesToday,
                "windows",
                map.ColorHex
            );

            await dbService.UpsertTag(userId, today, tag, cancellationToken);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Collector Error]: {ex}");
        }
    }

    private Guid IsUserAuthorized()
    {
        return _tokensService.GetUserId();
    }
}