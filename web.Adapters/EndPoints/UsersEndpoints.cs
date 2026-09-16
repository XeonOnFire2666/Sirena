using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SirenaAPI.Services;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SirenaAPI.Contract.User.Request;
using Microsoft.AspNetCore.Mvc;
using Google.Apis.Auth;
using System.Security.Authentication;

namespace SirenaAPI.EndPoints;

public record RefreshTokenRequest(string Token);

public static class UsersEndpoints
{
    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("auth/google", LoginWithGoogle);
        app.MapGet("users/getbyname", GetByName);
        app.MapPost("users/refresh", Refresh);

        return app;
    }

    private static async Task<IResult> Refresh([FromBody] RefreshTokenRequest token, UserService userService)
    {
        if (string.IsNullOrWhiteSpace(token.Token)) return Results.BadRequest("Token Cannot Be Null");

        try
        {
            var tokens = await userService.ReloadTokens(token.Token);

            if (tokens == null) return Results.Unauthorized();

            return Results.Ok(tokens);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: "Server has upal"
            );
        }
    }

    private static async Task<IResult> LoginWithGoogle(
        LoginRequest request,
        UserService userService,
        HttpContext context
        )
    {
        try
        {
            var responce = await userService.LoginWithGoogle(request.GoogleTokenId);

            if (responce.RefreshToken!.TokenNahuy == Guid.Empty)
            {
                return Results.BadRequest("Incorrect password");
            }
            if (responce.AccessToken is null && responce.RefreshToken is null)
            {
                return Results.NotFound("Not Found");
            }

            return Results.Ok(responce);
        }
        catch (InvalidJwtException ex)
        {
            throw new AuthenticationException("Предоставленный Google ID токен не прошел верификацию.", ex);
        }
        
    }
    private static async Task<IResult> GetByName([FromBody] string username, UserService userService)
    {
        try
        {
            var user = await userService.GetByName(username);
            return user == null ? Results.NotFound(
                new {error = $"user {username} not found"}) : Results.Ok(user);
        }
        catch (TimeoutException timeoutEx)
        {
            return Results.StatusCode(StatusCodes.Status504GatewayTimeout);
        }
        catch (Npgsql.PostgresException psqlEx)
        {
            return Results.Problem($"Server error ({psqlEx.MessageText})", statusCode: 500);
        }
        catch (DbUpdateConcurrencyException dbUpdateEx)
        {
            return Results.Text("Server DbUpdate Exception");
        }
        catch (Exception ex)
        {
            return Results.NotFound();
        }
    }
}