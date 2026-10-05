using System.Text.Json;
using SirenaDesktop.Core.Models;

namespace SirenaDesktop.Services;

public class TimerHistoryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    private readonly string _folderPath;

    public TimerHistoryService()
    {
        _folderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SirenaDesktop");
        _filePath = Path.Combine(_folderPath, "timer_history.json");
    }

    public List<TimerSession> LoadAll()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new List<TimerSession>();

            var json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json))
                return new List<TimerSession>();

            var sessions = JsonSerializer.Deserialize<List<TimerSession>>(json, JsonOptions);
            return sessions ?? new List<TimerSession>();
        }
        catch
        {
            return new List<TimerSession>();
        }
    }

    public List<TimerSession> LoadToday()
    {
        var today = DateTime.Today;
        return LoadAll()
            .Where(s => s.StartedAt.Date == today)
            .OrderBy(s => s.StartedAt)
            .ToList();
    }

    public void SaveAll(IEnumerable<TimerSession> sessions)
    {
        try
        {
            Directory.CreateDirectory(_folderPath);
            var json = JsonSerializer.Serialize(sessions.ToList(), JsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch
        {
        }
    }

    public void Upsert(TimerSession session)
    {
        var all = LoadAll();
        var index = all.FindIndex(s => s.Id == session.Id);
        if (index >= 0)
            all[index] = session;
        else
            all.Add(session);

        SaveAll(all);
    }
}