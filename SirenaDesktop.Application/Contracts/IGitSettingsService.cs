namespace SirenaDesktop.Application.Contracts;

public interface IGitSettingsService
{
    string? GetRepoPath();

    string? GetTrackedRepoPath();

    void SetRepoPath(string? path);

    bool IsValidRepo(string? path);
}