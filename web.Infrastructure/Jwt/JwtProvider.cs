using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SirenaAPI.Interfaces;
using SirenaAPI.Logic.Contract.User.Response;
using SirenaAPI.Logic.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SirenaAPI.Jwt;

public class JwtProvider(IOptions<JwtOptions> options) : IJwtProvider
{
    private readonly JwtOptions _options = options.Value;

    public TokensResponse GenerateTokens(Guid userId, RefreshToken refreshToken)
    {
        var sk = _options.SecretKey;

        var signingCredentials =
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(sk)),
                SecurityAlgorithms.HmacSha256);

        List<Claim> claims = [new Claim(ClaimTypes.NameIdentifier, $"{userId}")];

        Console.WriteLine(_options.Issuer);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            signingCredentials: signingCredentials,
            expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenExpiresMinutes)
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new TokensResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken
            );
    }

    public RefreshToken GenerateRefreshToken(Guid userId)
    {
        var expiry = _options.RefreshTokenExpiresDays;

        return new RefreshToken
        {
            UserId = userId,
            TokenNahuy = Guid.NewGuid(),
            Expiry = DateTime.UtcNow.AddDays(expiry)
        };
    }
}

public class JwtOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public int AccessTokenExpiresMinutes { get; set; }
    public int RefreshTokenExpiresDays { get; set; }
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}