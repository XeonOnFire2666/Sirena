using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using SirenaDesktop.Application.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace SirenaDesktop.Infrastructure.ExternalServices;
public class FolderPickerService : IFolderPickerService
{
    public async Task<string?> PickFolderAsync(string title, string? startPath = null)
    {
        var window = GetMainWindow();
        if (window is null) return null;

        var storageProvider = window.StorageProvider;

        IStorageFolder? startFolder = null;
        if (!string.IsNullOrWhiteSpace(startPath) && Directory.Exists(startPath))
            startFolder = await storageProvider.TryGetFolderFromPathAsync(startPath);

        var result = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            SuggestedStartLocation = startFolder
        });

        return result.FirstOrDefault()?.TryGetLocalPath();
    }

    private static Window? GetMainWindow() =>
        (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
}