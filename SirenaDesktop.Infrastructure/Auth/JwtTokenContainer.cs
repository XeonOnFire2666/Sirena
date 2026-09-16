using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SirenaDesktop.Application.Contracts;

namespace SirenaDesktop.Services;

public class JwtTokenContainer(TokensService service) : IJwtTokenContainer
{
    private readonly TokensService _service = service;
    private readonly JwtSecurityTokenHandler handler = new();

    public Guid GetUserId()
    {
        var tokens = _service.GetTokens();

        if (!tokens.Success) return Guid.Empty;

        var jwt = handler.ReadJwtToken(tokens.Response.AccessToken);

        Guid.TryParse(jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value, out var userId);

        return userId;
    }
}