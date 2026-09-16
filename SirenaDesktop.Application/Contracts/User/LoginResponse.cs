using SirenaDesktop.Core.Models;

namespace SirenaDesktop.Contracts.User;

public record LoginResponse(
    Core.Models.User? User = null,
    RefreshToken? RefreshToken = null,
    string? AccessToken = null
);