using SirenaDesktop.Application.Contracts.DayStat.Sync;

namespace SirenaDesktop.Application.Contracts.DayStat;

public interface IDataSyncService
{
    Task<SyncDataSuccess> SyncData(CancellationToken stoppingToken);
}