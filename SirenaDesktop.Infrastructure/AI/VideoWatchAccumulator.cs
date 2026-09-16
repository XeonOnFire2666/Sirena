using SirenaDesktop.Application.Contracts.Ai;

namespace SirenaDesktop.Infrastructure.AI;

public sealed class VideoWatchAccumulator : IVideoWatchAccumulator
{
    private const int SecondsPerMinute = 60;
    private readonly object _lock = new();
    private readonly Dictionary<string, int> _minutes = new();
    private readonly Dictionary<string, int> _pendingSeconds = new();

    public void Upsert(string videoTitle, int secondsWatched)
    {
        if (string.IsNullOrWhiteSpace(videoTitle) || secondsWatched <= 0) return;

        lock (_lock)
        {
            var seconds = _pendingSeconds.GetValueOrDefault(videoTitle, 0) + secondsWatched;

            while (seconds >= SecondsPerMinute)
            {
                seconds -= SecondsPerMinute;
                _minutes[videoTitle] = _minutes.GetValueOrDefault(videoTitle, 0) + 1;
            }

            _pendingSeconds[videoTitle] = seconds;
        }
    }

    public IReadOnlyDictionary<string, int> Snapshot()
    {
        lock (_lock)
        {
            return new Dictionary<string, int>(_minutes);
        }
    }

    public IReadOnlyDictionary<string, int> Drain()
    {
        lock (_lock)
        {
            var result = new Dictionary<string, int>(_minutes);
            _minutes.Clear();
            _pendingSeconds.Clear();
            return result;
        }
    }

    public void Merge(IReadOnlyDictionary<string, int> data)
    {
        if (data is null || data.Count == 0) return;

        lock (_lock)
        {
            foreach (var (title, minutes) in data)
            {
                if (string.IsNullOrWhiteSpace(title) || minutes <= 0) continue;
                _minutes[title] = _minutes.GetValueOrDefault(title, 0) + minutes;
            }
        }
    }

    public int TotalAccumulatedMinutes()
    {
        lock (_lock)
        {
            return _minutes.Values.Sum();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _minutes.Clear();
            _pendingSeconds.Clear();
        }
    }
}