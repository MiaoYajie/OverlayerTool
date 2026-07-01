using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace OverlayerTool.App.Helpers;

public static class DialogHelper
{
    public static async Task<string?> PickOpenFileAsync(Window owner, string title, params string[] patterns)
    {
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType(title)
                {
                    Patterns = patterns
                }
            ]
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    public static async Task<string?> PickSaveFolderAsync(Window owner, string title)
    {
        var folders = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }

    public static async Task<string?> PickProjectFolderAsync(Window owner, string title)
    {
        return await PickSaveFolderAsync(owner, title);
    }

    public static async Task<string?> PickSaveProjectFolderAsync(Window owner, string title, string suggestedName)
    {
        var folders = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            SuggestedStartLocation = await owner.StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents)
        });

        if (folders.Count == 0)
            return null;

        var basePath = folders[0].TryGetLocalPath();
        if (string.IsNullOrEmpty(basePath))
            return null;

        return Path.Combine(basePath, suggestedName);
    }
}
