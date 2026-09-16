namespace SirenaDesktop.Application.Contracts;

public interface ITokenRefreshService
{
    Task<bool> TryRefreshAsync();
}