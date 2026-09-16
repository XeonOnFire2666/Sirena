using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SirenaDesktop.Application.Contracts;

namespace SirenaDesktop.ViewModels;

public partial class GitSettingsViewModel : ObservableObject
{
    private readonly IGitSettingsService _gitSettingsService;

    [ObservableProperty] private bool _isRepoValid;

    [ObservableProperty] private string? _repoDisplayName;

    [ObservableProperty] private string? _repoPath;

    [ObservableProperty] private string? _statusMessage;

    public GitSettingsViewModel(IGitSettingsService gitSettingsService)
    {
        _gitSettingsService = gitSettingsService;

        var savedPath = _gitSettingsService.GetRepoPath();
        if (!string.IsNullOrWhiteSpace(savedPath))
        {
            RepoPath = savedPath;
            ValidateAndApply(savedPath, false);
        }
    }

    [RelayCommand]
    private void BrowseRepo()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Git repository folder",
            Multiselect = false
        };

        if (!string.IsNullOrWhiteSpace(RepoPath) && Directory.Exists(RepoPath)) dialog.InitialDirectory = RepoPath;

        if (dialog.ShowDialog() == true)
        {
            var selectedPath = dialog.FolderName;
            RepoPath = selectedPath;
            ValidateAndApply(selectedPath, true);
        }
    }

    [RelayCommand]
    private void ClearRepo()
    {
        _gitSettingsService.SetRepoPath(null);
        RepoPath = null;
        IsRepoValid = false;
        RepoDisplayName = null;
        StatusMessage = null;
    }

    private void ValidateAndApply(string path, bool showStatus)
    {
        var isValid = _gitSettingsService.IsValidRepo(path);
        IsRepoValid = isValid;
        RepoDisplayName = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        if (isValid)
        {
            _gitSettingsService.SetRepoPath(path);
            StatusMessage = showStatus ? "Repository connected" : null;
        }
        else
        {
            _gitSettingsService.SetRepoPath(path);
            StatusMessage = "Folder does not contain .git — repository is not tracked";
        }
    }
}