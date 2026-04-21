using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using LocoCalc.Services;

namespace LocoCalc.Services;

/// <summary>Desktop implementation: OS save-file dialog + Process.Start to open PDF.</summary>
public class DesktopPdfSaveService(Window owner) : IPdfSaveService
{
    public async Task<string?> PickSavePathAsync(string suggestedName)
    {
        var topLevel = Window.GetTopLevel(owner);
        if (topLevel is null) return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title             = "Uložit PDF / Save PDF",
            SuggestedFileName = suggestedName,
            DefaultExtension  = "pdf",
            FileTypeChoices   = [new FilePickerFileType("PDF") { Patterns = ["*.pdf"] }]
        });

        return file?.TryGetLocalPath();
    }

    public void OpenFile(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch { /* ignore if OS can't open */ }
    }
}
