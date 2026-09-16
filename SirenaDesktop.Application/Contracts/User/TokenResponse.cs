using SirenaDesktop.Core.Models;

namespace SirenaDesktop.Contracts.User;

public record TokenResponse(
    RefreshToken? RefreshToken = null,
    string? AccessToken = null
);