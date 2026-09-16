using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using DnsClient;

namespace SirenaDesktop.Infrastructure.AI;

public sealed class DoHConnectionFactory
{
    private static readonly IPAddress PrimaryDnsIp = IPAddress.Parse("111.88.96.50");
    private static readonly IPAddress SecondaryDnsIp = IPAddress.Parse("111.88.96.51");

    private static readonly LookupClient _dnsClient = new(new LookupClientOptions(
        new NameServer(PrimaryDnsIp),
        new NameServer(SecondaryDnsIp))
    {
        UseTcpFallback = true,
        Timeout = TimeSpan.FromSeconds(5),
        Retries = 1,
        ThrowDnsErrors = false
    });

    public async ValueTask<Stream> ConnectCallback(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(10));

        var host = context.DnsEndPoint.Host;
        var port = context.DnsEndPoint.Port;

        var ip = await TryResolveViaCustomDnsAsync(host, cts.Token);

        if (ip is null)
            try
            {
                var addresses = await Dns.GetHostAddressesAsync(host, cts.Token);
                ip = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
                     ?? addresses.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GeminiAI][DNS] System DNS failed: {ex.Message}");
                throw;
            }

        if (ip is null)
            throw new SocketException((int)SocketError.HostNotFound);

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(ip, port, cts.Token);
            return new NetworkStream(socket, true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private static async Task<IPAddress?> TryResolveViaCustomDnsAsync(string host, CancellationToken ct)
    {
        try
        {
            var result = await _dnsClient.QueryAsync(host, QueryType.A, cancellationToken: ct);

            if (result.HasError)
            {
                Debug.WriteLine($"[GeminiAI][DNS] Custom DNS returned an error for '{host}': {result.ErrorMessage}");
                return null;
            }

            var ip = result.Answers.ARecords().FirstOrDefault()?.Address;

            if (ip is null)
            {
                Debug.WriteLine($"[GeminiAI][DNS] Custom DNS returned no A records for '{host}'.");
                return null;
            }

            Debug.WriteLine($"[GeminiAI][DNS] Resolved via xbox-dns: {host} -> {ip}");
            return ip;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GeminiAI][DNS] Query error to xbox-dns: {ex.Message}");
            return null;
        }
    }
}