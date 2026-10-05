using SirenaDesktop.Application.Contracts.Connect;

namespace SirenaDesktop.Application.Services;

public class ConnectionContainer : IConnectionContainer
{
    private readonly object _lock = new();
    private bool _isConnectionError;
    private TaskCompletionSource _onlineTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public event Action<bool>? StateChanged;

    public bool IsOffline => GetState();

    public bool GetState()
    {
        lock (_lock)
        {
            return _isConnectionError;
        }
    }

    public void SetState(bool state)
    {
        lock (_lock)
        {
            if (_isConnectionError == state)
                return;

            _isConnectionError = state;

            if (!state)
            {
                _onlineTcs.TrySetResult();
            }
            else if (_onlineTcs.Task.IsCompleted)
            {
                _onlineTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }

        StateChanged?.Invoke(state);
    }

    public async Task WaitForOnlineAsync(CancellationToken ct)
    {
        Task taskToWait;

        lock (_lock)
        {
            if (!_isConnectionError)
                return;

            taskToWait = _onlineTcs.Task;
        }

        await taskToWait.WaitAsync(ct).ConfigureAwait(false);
    }
}