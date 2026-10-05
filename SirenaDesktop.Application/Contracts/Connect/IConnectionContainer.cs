namespace SirenaDesktop.Application.Contracts.Connect;

public interface IConnectionContainer
{
    event Action<bool>? StateChanged;
    bool IsOffline { get; }
    bool GetState();
    void SetState(bool state);
    Task WaitForOnlineAsync(CancellationToken ct);
}