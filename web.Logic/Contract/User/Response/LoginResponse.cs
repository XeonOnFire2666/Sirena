using SirenaAPI.Logic.Entities;

namespace SirenaAPI.Contract.User.Request;

public record LoginResponse(
    Models.User? User = null,
    RefreshToken? RefreshToken = null,
    string? AccessToken = null
    );
