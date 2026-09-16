using System.Text.Json;

namespace SirenaDesktop.Notifications;

public class StreakNotificationService
{
    private readonly string _filePath;

    public StreakNotificationService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SirenaDesktop");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "streak_state.json");
    }

    private State LoadState()
    {
        if (!File.Exists(_filePath))
            return new State();

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<State>(json) ?? new State();
        }
        catch
        {
            return new State();
        }
    }

    private void SaveState(State state)
    {
        var json = JsonSerializer.Serialize(state);
        File.WriteAllText(_filePath, json);
    }

    public bool ShouldCelebrate(int currentStreak)
    {
        if (currentStreak <= 0)
            return false;

        var state = LoadState();
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (state.LastCelebratedDate == today)
            return false;

        if (currentStreak <= state.LastCelebratedStreak)
        {
            state.LastCelebratedDate = today;
            state.LastCelebratedStreak = currentStreak;
            SaveState(state);
            return false;
        }

        state.LastCelebratedDate = today;
        state.LastCelebratedStreak = currentStreak;
        SaveState(state);
        return true;
    }

    private class State
    {
        public DateOnly LastCelebratedDate { get; set; }
        public int LastCelebratedStreak { get; set; }
    }
}