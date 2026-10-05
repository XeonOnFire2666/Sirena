using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirenaDesktop.Application.Contracts;

namespace SirenaDesktop.ViewModels;

public partial class GitSettingsViewModel : ObservableObject
{
    private readonly IGitSettingsService _gitSettingsService;
    private readonly IFolderPickerService _folderPicker;
    private readonly Window _ownerWindow;

    [ObservableProperty] private bool _isRepoValid;

    [ObservableProperty] private string? _repoDisplayName;

    [ObservableProperty] private string? _repoPath;

    [ObservableProperty] private string? _statusMessage;

    public GitSettingsViewModel(IGitSettingsService gitSettingsService, IFolderPickerService folderPicker)
    {
        _gitSettingsService = gitSettingsService;
        _folderPicker = folderPicker;

        var savedPath = _gitSettingsService.GetRepoPath();
        if (!string.IsNullOrWhiteSpace(savedPath))
        {
            RepoPath = savedPath;
            ValidateAndApply(savedPath, false);
        }
    }

    [RelayCommand]
    private async Task BrowseRepo()
    {
        var selectedPath = await _folderPicker.PickFolderAsync("Select Git repository folder", RepoPath);
        if (string.IsNullOrWhiteSpace(selectedPath)) return;

        RepoPath = selectedPath;
        ValidateAndApply(selectedPath, true);
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