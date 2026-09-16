using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using CredentialManagement;
using SirenaDesktop.Application.Contracts.User.Tokens.LogicInterfaces;
using SirenaDesktop.Contracts.User;

namespace SirenaDesktop.Repositories;

public class TokenContainer : ITokenContainer
{
    private const string TokenTargetId = "SirenaDesktop_Tokens";

    public bool AddTokens(TokenResponse responseTokens)
    {
        var json = JsonSerializer.Serialize(responseTokens, JsonSerializerOptions.Web);

        using var credential = new Credential
        {
            Target = TokenTargetId,
            Type = CredentialType.Generic,
            PersistanceType = PersistanceType.LocalComputer,
            Password = json
        };

        var result = credential.Save();

        if (!result)
        {
            var error = Marshal.GetLastWin32Error();
            Debug.WriteLine($"[TOKEN SAVE FAILED]: Win32Error={error}");
        }

        return result;
    }

    public bool CheckTokens()
    {
        using var credential = new Credential { Target = TokenTargetId };
        return credential.Exists();
    }

    public TokenResponse GetTokens()
    {
        using var credential = new Credential { Target = TokenTargetId };

        var success = credential.Load();
        if (success is false) return null!;

        return JsonSerializer.Deserialize<TokenResponse>(credential.Password, JsonSerializerOptions.Web)!;
    }

    public bool DeleteTokens()
    {
        using var credential = new Credential { Target = TokenTargetId };
        return credential.Delete();
    }
}