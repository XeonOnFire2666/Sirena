using System.Diagnostics;
using SirenaDesktop.ApiCallers;
using SirenaDesktop.Application.Contracts;
using SirenaDesktop.Services;

namespace SirenaDesktop.Infrastructure.Services;

public class TokenRefreshService(UserApiCaller userApiCaller, TokensService tokensService) : ITokenRefreshService
{
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<bool> TryRefreshAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var currentTokens = tokensService.GetTokens();
            if (!currentTokens.Success || currentTokens.Response?.RefreshToken == null)
                return false;

            var newTokens = await userApiCaller.RefreshTokens(currentTokens.Response.RefreshToken);
            if (newTokens?.AccessToken == null)
            {
                Debug.WriteLine("[TryRefreshAsync] newTokens");
                return false;
            }

            tokensService.DeleteTokens();
            tokensService.AddTokens(newTokens);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}