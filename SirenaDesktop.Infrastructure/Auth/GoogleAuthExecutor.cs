using System.Net.Http;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Util.Store;
using SirenaDesktop.ApiCallers;
using SirenaDesktop.Contracts.User;

namespace SirenaDesktop.Infrastructure.Auth;

public class GoogleAuthExecutor(UserApiCaller api) : IDisposable
{
    private readonly UserApiCaller _api = api;
    private CancellationTokenSource? _authCts;

    public void Dispose()
    {
        _authCts?.Cancel();
    }


    public async Task<UserCredential> AcquireIdentityProviderCredentialsAsync()
    {
        if (_authCts != null)
            await CancelOperation();
        _authCts = new CancellationTokenSource();

        var id = new ClientSecrets
        {
            ClientId = "100603258697-o6lp7nf3edn9k3qegc0sdjkt7pa09hf6.apps.googleusercontent.com",
            ClientSecret = "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"
        };

        var initializer = new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = id,
            Scopes = ["openid", "profile", "email"],
            DataStore = new NullDataStore()
        };

        var flow = new ForceSelectAccountFlow(initializer);

        var codeReceiver = new LocalServerCodeReceiver(@"
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Sirena Authorization</title>
                <script type='text/javascript'>
                    function closeWindow() {
                        // Bypass browser restrictions on closing externally opened tabs
                        window.open('', '_self', ''); 
                        window.close();
                    }
                </script>
            </head>
            <body onload='closeWindow()' style='font-family: sans-serif; text-align: center; margin-top: 50px; background-color: #f9f9f9; color: #333;'>
                <h2>Login successful!</h2>
                <p>You can close this window. If it did not close automatically, please close it manually.</p>
            </body>
            </html>");


        return await new AuthorizationCodeInstalledApp(flow, codeReceiver)
            .AuthorizeAsync("user", _authCts.Token);
    }

    public async Task<LoginResponse> Login(LoginRequest request)
    {
        if (_authCts != null)
            await CancelOperation();
        _authCts = new CancellationTokenSource();

        using var client = new HttpClient();

        try
        {
            return await _api.LoginWithGoogle(request, _authCts.Token);
        }
        catch (Exception)
        {
            await CancelOperation();
        }

        return new LoginResponse();
    }

    public async Task CancelOperation()
    {
        await _authCts?.CancelAsync();
    }
}

public class ForceSelectAccountFlow : GoogleAuthorizationCodeFlow
{
    public ForceSelectAccountFlow(Initializer initializer) : base(initializer)
    {
    }

    public override AuthorizationCodeRequestUrl CreateAuthorizationCodeRequest(string redirectUri)
    {
        var request = (GoogleAuthorizationCodeRequestUrl)base.CreateAuthorizationCodeRequest(redirectUri);

        request.Prompt = "select_account";

        return request;
    }
}