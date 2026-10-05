using Microsoft.Extensions.Hosting;
using SirenaDesktop.Application.Contracts.Connect;
using SirenaDesktop.Sync;

namespace SirenaDesktop.Infrastructure.BackgroundWorkers;

public sealed class SyncTrigger
{
    private readonly SemaphoreSlim _signal = new(0, 1);

    public void Kick()
    {
        if (_signal.CurrentCount == 0) _signal.Release();
    }

    public Task WaitAsync(TimeSpan timeout, CancellationToken ct) => _signal.WaitAsync(timeout, ct);
}

public sealed class SyncWorker(
    SyncEngine engine,
    IConnectionContainer connection,
    SyncTrigger trigger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await connection.WaitForOnlineAsync(stoppingToken);
                var online = await engine.RunAsync(stoppingToken);
                await trigger.WaitAsync(online ? TimeSpan.FromSeconds(30) : TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}