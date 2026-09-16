using Microsoft.Extensions.Configuration;
using SirenaDesktop.ApiCallers;
using SirenaDesktop.Application.Contracts;
using SirenaDesktop.Contracts.User;

namespace SirenaDesktop.Infrastructure.Auth;

public class GoogleAuthService(IConfiguration configuration, UserApiCaller api) : IGoogleAuthService
{
    private readonly UserApiCaller _api = api;
    private readonly IConfiguration _configuration = configuration;

    public async Task<LoginResponse> AuthorizeAsync()
    {
        using var executor = new GoogleAuthExecutor(_api);
        try
        {
            var credential = await executor.AcquireIdentityProviderCredentialsAsync();
            var tokenStr = credential.Token?.IdToken;

            if (string.IsNullOrEmpty(tokenStr)) return new LoginResponse();

            return await executor.Login(new LoginRequest(tokenStr));
        }
        catch (OperationCanceledException)
        {
            await executor.CancelOperation();
        }

        return new LoginResponse();
    }
}