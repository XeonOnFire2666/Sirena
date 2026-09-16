using SirenaDesktop.Contracts.User;

namespace SirenaDesktop.Application.Contracts.User.Tokens.LogicInterfaces;

public interface ITokenContainer
{
    bool AddTokens(TokenResponse responseTokens);
    bool CheckTokens();
    TokenResponse GetTokens();
    bool DeleteTokens();
}