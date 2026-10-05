using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using SirenaDesktop.Contracts.User;
using SirenaDesktop.Core.Models;

namespace SirenaDesktop.ApiCallers;

public record RefreshTokenRequest(string Token);

public class UserApiCaller(HttpClient client)
{
    private readonly HttpClient _client = client;
    private readonly string host = "https://sirenaapi.onrender.com";

    public async Task<TokenResponse> RefreshTokens(RefreshToken token)
    {
        try
        {
            var responce = await _client.PostAsync(host + "/users/refresh",
                JsonContent.Create(new RefreshTokenRequest(token.TokenNahuy.ToString())));

            if (responce.StatusCode == HttpStatusCode.Unauthorized)
            {
                Debug.WriteLine("[Refresh] 401 Unauthorized ");
                return new TokenResponse();
            }

            if (!responce.IsSuccessStatusCode)
            {
                Debug.WriteLine($"[Refresh] Server error: {(int)responce.StatusCode}");
                return new TokenResponse();
            }

            responce.EnsureSuccessStatusCode();

            var content = await responce.Content.ReadAsStreamAsync();

            var tokens = await JsonSerializer.DeserializeAsync<TokenResponse>(content, JsonSerializerOptions.Web);
            return tokens ?? new TokenResponse();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Refresh] NETWORK EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            return new TokenResponse();
        }
    }

    public async Task<LoginResponse> LoginWithGoogle(LoginRequest request, CancellationToken ct)
    {
        var responce = await _client.PostAsync(host + "/auth/google", JsonContent.Create(request), ct);

        if (responce == null) return new LoginResponse();

        var result = await JsonSerializer.DeserializeAsync<LoginResponse>(await responce.Content.ReadAsStreamAsync(ct),
            JsonSerializerOptions.Web, ct);

        return result;
    }
}