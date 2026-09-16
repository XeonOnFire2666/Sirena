using SirenaAPI.Contract.User.Request;
using SirenaAPI.Logic.Contract.User.Response;
using SirenaAPI.Logic.Entities;

namespace SirenaAPI.Interfaces;

public interface IJwtProvider
{
    public TokensResponse GenerateTokens(Guid userId, RefreshToken refreshToken);
    public RefreshToken GenerateRefreshToken(Guid userId);
}