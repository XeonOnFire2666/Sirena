using SirenaDesktop.Core.Models;
using SirenaDesktop.Infrastructure.Services;
using SirenaDesktop.Repositories;

namespace SirenaDesktop.Sync;

public record CombinedSyncResult(bool WorkspacesOffline);

public class CombinedSyncService(
    WorkspaceService workspaceService,
    IPendingChangeRepository pendingRepo)
{
    private readonly IPendingChangeRepository _pendingRepo = pendingRepo;
    private readonly WorkspaceService _workspaceService = workspaceService;

    public async Task<CombinedSyncResult> SyncAll(CancellationToken ct = default)
    {
        var pending = await _pendingRepo.GetAll(ct);
        var wsChanges = pending.Where(p => p.EntityKind == PendingEntityKind.Workspace).ToList();

        await _workspaceService.PushPendingChanges(wsChanges);

        var wsResult = await _workspaceService.Sync();

        return new CombinedSyncResult(wsResult.IsOffline);
    }
}