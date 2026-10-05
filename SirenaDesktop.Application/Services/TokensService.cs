using SirenaDesktop.Application.Contracts.User.Tokens.LogicInterfaces;
using SirenaDesktop.Contracts.User;

namespace SirenaDesktop.Services;

public class TokensService(ITokenContainer container)
{
    private readonly ITokenContainer _container = container;

    public TokensResponce AddTokens(TokenResponse responseTokens)
    {
        if (responseTokens is null || responseTokens.AccessToken is null)
            return new TokensResponce(false, "Bad request");

        var saved = _container.AddTokens(responseTokens);
        if (!saved)
            return new TokensResponce(false, "Failed to persist tokens");

        return new TokensResponce(true, "Success");
    }

    public TokensResponce GetTokens()
    {
        var tokens = _container.GetTokens();

        if (tokens is null) return new TokensResponce(false, "Tokens don't exist");

        return new TokensResponce(true, "Success", tokens);
    }

    public TokensResponce DeleteTokens()
    {
        _container.DeleteTokens();

        return new TokensResponce(true, "Success");
    }

    public record TokensResponce(bool Success, string Message, TokenResponse Response = null);
}