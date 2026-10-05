using System.Net;
using SirenaDesktop.Application.Contracts.Connect;
using SirenaDesktop.Application.Services;

namespace SirenaDesktop.Infrastructure.ExternalServices;

public class ConnectionService(HttpClient client, IConnectionContainer container) : IConnectionService
{
    private const string HealthUrl = "https://sirenaapi.onrender.com/health";

    public async Task CheckConnection(CancellationToken ct = default)
    {
        bool isOffline;

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(35));
            using var response = await client.GetAsync(HealthUrl, timeout.Token);
            isOffline = (int)response.StatusCode >= 500;
        }
        catch (HttpRequestException)
        {
            isOffline = true;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            isOffline = true;
        }

        container.SetState(isOffline);
    }
}