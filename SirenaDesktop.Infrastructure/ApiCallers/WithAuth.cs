using System.Net.Http;
using System.Net.Http.Headers;
using SirenaDesktop.Services;

namespace SirenaDesktop.ApiCallers;

public class AuthTokens(TokensService tokensService)
{
    private readonly TokensService _tokensService = tokensService;

    public async Task<HttpRequestMessage> WithAuth(HttpMethod method, string url, HttpContent content = default)
    {
        var request = new HttpRequestMessage(method, url);
        var token = _tokensService.GetTokens()?.Response?.AccessToken;
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (content != null)
            request.Content = content;

        return request;
    }
}