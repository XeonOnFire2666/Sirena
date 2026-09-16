using System.IO;
using System.Text.Json;
using LibGit2Sharp;
using SirenaDesktop.Application.Contracts;

namespace SirenaDesktop.Infrastructure.AI;

public class GitRepoSettings
{
    public string? RepoPath { get; set; }
}

public class GitSettingsService : IGitSettingsService
{
    private readonly GitRepoSettings _cache;
    private readonly object _lock = new();
    private readonly string _settingsPath;

    public GitSettingsService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SirenaDesktop");

        Directory.CreateDirectory(dir);
        _settingsPath = Path.Combine(dir, "git-settings.json");
        _cache = Load();
    }

    public string? GetRepoPath()
    {
        lock (_lock)
        {
            return _cache.RepoPath;
        }
    }

    public string? GetTrackedRepoPath()
    {
        var path = GetRepoPath();
        return IsValidRepo(path) ? path : null;
    }

    public void SetRepoPath(string? path)
    {
        lock (_lock)
        {
            _cache.RepoPath = string.IsNullOrWhiteSpace(path) ? null : path;
            Save();
        }
    }

    public bool IsValidRepo(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            return Directory.Exists(path) && Repository.IsValid(path);
        }
        catch
        {
            return false;
        }
    }

    private GitRepoSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
                return new GitRepoSettings();

            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<GitRepoSettings>(json) ?? new GitRepoSettings();
        }
        catch
        {
            return new GitRepoSettings();
        }
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsPath, json);
    }
}