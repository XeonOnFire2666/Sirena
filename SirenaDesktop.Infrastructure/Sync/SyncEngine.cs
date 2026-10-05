using System.Net;
using System.Text.Json;
using SirenaDesktop.ApiCallers;
using SirenaDesktop.Application.Contracts;
using SirenaDesktop.Core.Models;
using SirenaDesktop.Infrastructure.Persistence;
using SirenaDesktop.Services;

namespace SirenaDesktop.Sync;

public sealed class SyncEngine(
    WorkspaceStore store,
    WorkspaceApiCaller workspaceApi,
    ITokenRefreshService refreshService,
    JwtTokenContainer jwt)
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<bool> RunAsync(CancellationToken ct = default)
    {
        var userId = jwt.GetUserId();
        if (userId == Guid.Empty) return false;

        await _gate.WaitAsync(ct);
        try
        {
            var online = await FlushAsync(ct);
            if (online)
                await PullAsync(userId, ct);
            return online;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<bool> FlushAsync(CancellationToken ct)
    {
        var due = await store.GetDueAsync(DateTime.UtcNow, ct);

        foreach (var item in due)
        {
            if (item.EntityKind != SyncEntityKind.Workspace) continue;

            var dto = JsonSerializer.Deserialize<UpsertWorkspaceDto>(item.PayloadJson, JsonSerializerOptions.Web);
            if (dto is null)
            {
                await store.AckAsync(item.EntityKind, item.EntityId, item.Version, ct);
                continue;
            }

            var result = await workspaceApi.Upsert(dto);

            if (result.IsUnauthorizedError)
            {
                if (!await refreshService.TryRefreshAsync()) return false;
                result = await workspaceApi.Upsert(dto);
            }

            if (!result.IsError && !result.IsUnauthorizedError)
            {
                await store.AckAsync(item.EntityKind, item.EntityId, item.Version, ct);
                continue;
            }

            await store.MarkFailedAsync(item, result.Message ?? $"HTTP {(int?)result.StatusCode}", ct);

            if (StopsFlush(result)) return false;
        }

        return true;
    }

    private async Task PullAsync(Guid userId, CancellationToken ct)
    {
        var response = await workspaceApi.GetAll(userId);
        if (response.IsError || response.IsUnauthorizedError || response.Workspaces is null) return;

        var pending = await store.GetPendingIdsAsync(SyncEntityKind.Workspace, ct);
        var localById = (await store.GetAllAsync(ct)).ToDictionary(w => w.Id);

        foreach (var server in response.Workspaces)
        {
            if (pending.Contains(server.Id)) continue;

            if (!localById.TryGetValue(server.Id, out var local) || server.UpdatedAt > local.UpdatedAt)
                await store.SaveServerAsync(server, ct);
        }
    }

    private static bool StopsFlush(WorkspaceResponse r) =>
        r.IsUnauthorizedError
        || r.StatusCode is null
        || r.StatusCode == HttpStatusCode.RequestTimeout
        || r.StatusCode == (HttpStatusCode)429
        || (int)r.StatusCode.Value >= 500;
}