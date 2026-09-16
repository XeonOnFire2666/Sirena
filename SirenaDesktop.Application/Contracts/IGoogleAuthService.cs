using SirenaDesktop.Contracts.User;

namespace SirenaDesktop.Application.Contracts;

public interface IGoogleAuthService
{
    Task<LoginResponse> AuthorizeAsync();
}