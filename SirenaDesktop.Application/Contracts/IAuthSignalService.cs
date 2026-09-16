namespace SirenaDesktop.Application.Contracts;

public interface IAuthSignalService
{
    Task WaitForLoginAsync(CancellationToken cancellationToken);
    void SignalLoginSuccess();
    void Reset();
}