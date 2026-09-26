using Microsoft.Extensions.Caching.Memory;
using SirenaAPI.Interfaces;
using SirenaAPI.Models;
using SirenaAPI.Contract.User.Request;
using SirenaAPI.Logic.Entities;
using Microsoft.Extensions.Options;
using System.Security.Authentication;
using Google.Apis.Auth;
using static Google.Apis.Auth.GoogleJsonWebSignature;
using SirenaAPI.Logic.Contract.User.Response;

namespace SirenaAPI.Services;

public class UserService(
    IUserRepository<User> userRepository,
    ITokenRepository<RefreshToken> tokenRepository,
    IJwtProvider jwtProvider,
    IMemoryCache memoryCache,
    IOptions<RealGoogleOptions> options)
{
    private readonly IJwtProvider _jwtProvider = jwtProvider;
    private readonly IUserRepository<User> _userRepository = userRepository;
    private readonly ITokenRepository<RefreshToken> _tokenRepository = tokenRepository;
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly IOptions<RealGoogleOptions> _options = options;

    public async Task<LoginResponse> Register(
        string username, string email, string googleId, string picUrl,
        CancellationToken ct = default)
    {
        var user = User.Create(username, email, googleId, picUrl);
        await _userRepository.Add(user, ct);

        var refreshToken = _jwtProvider.GenerateRefreshToken(user.Id);
        await _tokenRepository.Add(refreshToken, ct);

        var response = _jwtProvider.GenerateTokens(user.Id, refreshToken);
        Console.WriteLine($"[REGISTER] UserId: {user.Id}, RefreshToken: {refreshToken.TokenNahuy}");
        return new(user, response.RefreshToken, response.AccessToken);
    }

    public async Task<LoginResponse> LoginWithGoogle(string tokenId, CancellationToken ct = default)
    {
        Payload payload;

        try
        {
            var validationSettings = new ValidationSettings()
            {
                Audience = [
                    _options.Value.DesktopClientID,
                    _options.Value.FlutterAndroidClientID,
                    _options.Value.FlutterWebClientID
                ]
            };

            payload = await ValidateAsync(tokenId, validationSettings);
        }
        catch (InvalidJwtException ex)
        {
            throw new AuthenticationException("Google ID токен не прошел верификацию.", ex);
        }

        var user = await _userRepository.GetByGoogleId(payload.Subject, ct);

        if (user == null)
        {
            Console.WriteLine($"[LOGIN] New user: {payload.Email}");
            return await Register(payload.Name, payload.Email, payload.Subject, payload.Picture, ct);
        }

        var refreshToken = _jwtProvider.GenerateRefreshToken(user.Id);
        await _tokenRepository.Add(refreshToken, ct);

        var response = _jwtProvider.GenerateTokens(user.Id, refreshToken);
        Console.WriteLine($"[LOGIN] Existing user: {user.Id}, New RefreshToken: {refreshToken.TokenNahuy}");
        return new(user, response.RefreshToken, response.AccessToken);
    }

    public async Task<TokensResponse> ReloadTokens(string token)
    {
        if (!Guid.TryParse(token, out var tokenGuid))
        {
            Console.WriteLine($"[REFRESH] Invalid GUID format: '{token}'");
            return null;
        }

        Console.WriteLine($"[REFRESH] Searching DB for token: {tokenGuid}");

        var refreshToken = await _tokenRepository.GetById(tokenGuid);

        if (refreshToken == null)
        {
            Console.WriteLine($"[REFRESH FAIL] Token NOT FOUND in DB: {tokenGuid}");
            return null;
        }

        if (refreshToken.Expiry <= DateTime.UtcNow)
        {
            Console.WriteLine($"[REFRESH FAIL] Token EXPIRED. Expiry: {refreshToken.Expiry:O}, UTC now: {DateTime.UtcNow:O}");
            await _tokenRepository.Delete(tokenGuid);
            return null;
        }

        var newRefreshToken = _jwtProvider.GenerateRefreshToken(refreshToken.UserId);
        var affected = await _tokenRepository.Update(refreshToken.TokenNahuy, newRefreshToken.TokenNahuy, newRefreshToken.Expiry);

        if (affected == 0)
        {
            Console.WriteLine($"[REFRESH CRITICAL] Update affected 0 rows! Old: {refreshToken.TokenNahuy}, New: {newRefreshToken.TokenNahuy}");
            return null;
        }

        Console.WriteLine($"[REFRESH OK] UserId: {refreshToken.UserId}, Old: {refreshToken.TokenNahuy}, New: {newRefreshToken.TokenNahuy}");
        return _jwtProvider.GenerateTokens(refreshToken.UserId, newRefreshToken);
    }

    public async Task<User?> GetByName(string username)
    {
        _memoryCache.TryGetValue(username, out User? user);

        if (user == null)
        {
            user = await _userRepository.GetByName(username);
            if (user == null) return user;

            Console.WriteLine($"{user.Name} извлечен из базы данных");
            _memoryCache.Set(user.Name, user, new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(10)));
        }
        else
        {
            Console.WriteLine($"{user.Name} извлечен из кэша");
        }
        return user;
    }
}

public class RealGoogleOptions
{
    public string DesktopClientID { get; set; } = string.Empty;
    public string FlutterAndroidClientID { get; set; } = string.Empty;
    public string FlutterWebClientID { get; set; } = string.Empty;
}