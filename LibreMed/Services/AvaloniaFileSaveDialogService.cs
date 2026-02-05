using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace LibreMed.Services;

public sealed class AvaloniaFileSaveDialogService : IFileSaveDialogService
{
    public async Task<string?> PickPdfSavePathAsync(string suggestedFileName)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow is null)
            return null;

        var file = await desktop.MainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save as PDF",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "pdf",
            FileTypeChoices =
            [
                new FilePickerFileType("PDF document") { Patterns = ["*.pdf"] }
            ]
        });

        return file?.TryGetLocalPath();
    }

    public async Task<string?> PickJsonSavePathAsync(string suggestedFileName)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow is null)
            return null;

        var file = await desktop.MainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export database as JSON",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "json",
            FileTypeChoices =
            [
                new FilePickerFileType("JSON file") { Patterns = ["*.json"] }
            ]
        });

        return file?.TryGetLocalPath();
    }

    public async Task<string?> PickJsonOpenPathAsync()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow is null)
            return null;

        var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import database from JSON",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("JSON file") { Patterns = ["*.json"] }
            ]
        });

        return files.Count == 0 ? null : files[0].TryGetLocalPath();
    }
}