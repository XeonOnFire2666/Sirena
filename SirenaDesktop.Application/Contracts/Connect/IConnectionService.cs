namespace SirenaDesktop.Application.Contracts.Connect;
public interface IConnectionService
{
    Task CheckConnection(CancellationToken ct = default);
}