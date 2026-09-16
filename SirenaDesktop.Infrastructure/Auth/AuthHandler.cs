using SirenaDesktop.Application.Contracts;

namespace SirenaDesktop.Infrastructure.Auth;

public class AuthSignalService : IAuthSignalService
{
    private readonly Lock _lock = new();
    private volatile bool _isSignaled;
    private TaskCompletionSource _loginTcs = CreateTcs();

    public Task WaitForLoginAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return _loginTcs.Task.WaitAsync(cancellationToken);
        }
    }

    public void SignalLoginSuccess()
    {
        lock (_lock)
        {
            if (!_isSignaled)
            {
                _isSignaled = true;
                _loginTcs.TrySetResult();
            }
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _isSignaled = false;
            if (!_loginTcs.Task.IsCompleted) _loginTcs.TrySetCanceled();
            _loginTcs = CreateTcs();
        }
    }

    private static TaskCompletionSource CreateTcs()
    {
        return new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}