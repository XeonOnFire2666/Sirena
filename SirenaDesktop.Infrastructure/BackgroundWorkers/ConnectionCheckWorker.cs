using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SirenaDesktop.Application.Contracts.Connect;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SirenaDesktop.Infrastructure.BackgroundWorkers;

internal class ConnectionCheckWorker : BackgroundService
{
    private readonly IConnectionService _connectionService;

    private const int TotalDelayMs = 10000;      
    private const int DelayStepMs = 500;         

    public ConnectionCheckWorker(IConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _connectionService.CheckConnection(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ ConnectionCheckWorker ] Connection error: {ex.Message}");
            }

            for (var i = 0; i < TotalDelayMs; i += DelayStepMs)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    await Task.Delay(DelayStepMs, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}